#if UNITY_EDITOR
using PowerUtilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using static AnimTexture.AnimTextureManifest;
using Object = UnityEngine.Object;

namespace AnimTexture
{
    /// <summary>
    /// Bake BoneTex
    /// </summary>
    public partial class AnimTextureEditor
    {
        [MenuItem(POWER_UTILS_MENU + "/BakeBoneTexAtlas_From_GenericAnimType")]
        public static void BakeBoneTexFromSelected()
        {
            var objs = Selection.GetFiltered<GameObject>(SelectionMode.DeepAssets);
            var skinnedMeshGo = objs.Where(obj => obj.GetComponentInChildren<SkinnedMeshRenderer>()).FirstOrDefault();
            if (!skinnedMeshGo)
            {
                EditorUtility.DisplayDialog("Warning", $" not found SkinnedMeshRenderer from selected objects", "ok");
                return;
            }

            //1 check animationClip
            var clipList = GetAnimationClipsFromAssets(objs);
            if (clipList.Count() == 0)
            {
                clipList = GetAnimstionClipFromAnimation(objs);
            }

            var bakeBoneCS = AssetDatabaseTools.FindAssetPathAndLoad<ComputeShader>(out _, "BakeBoneMatrix", ".compute");
            if (!bakeBoneCS)
                throw new FileNotFoundException("cannot found compute shader : BakeBone");

            var clipCount = BakeBoneAllClips(skinnedMeshGo, clipList, bakeBoneCS);
            ShowResult(skinnedMeshGo, clipCount);
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>($"Assets/{DEFAULT_TEX_DIR}"));
        }

        public static int BakeBoneAllClips(GameObject go, List<AnimationClip> clipList, ComputeShader bakeBondCS)
        {
            var skin = go.GetComponentInChildren<SkinnedMeshRenderer>();

            var dir = $"{Application.dataPath}/{DEFAULT_TEX_DIR}/";
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var manifest = ScriptableObject.CreateInstance<AnimTextureManifest>();
            FillBoneWeights(skin, manifest);
            FillBones(skin, manifest);

            var yList = GenBoneTexture(skin, clipList, out manifest.atlas);
            BakeBoneClips(go, skin, clipList, manifest, yList, bakeBondCS);
            manifest.atlas.Apply();
            //output infos
            AssetDatabase.CreateAsset(manifest.atlas, $"Assets/{DEFAULT_TEX_DIR}/{go.name}_BoneTexture.asset");
            AssetDatabase.CreateAsset(manifest, $"Assets/{DEFAULT_TEX_DIR}/{go.name}_{typeof(AnimTextureManifest).Name}.asset");

            AssetDatabase.Refresh();
            return 1;
        }

        public static void FillBones(SkinnedMeshRenderer skin, AnimTextureManifest manifest)
        {
            var bindPoseArr = skin.sharedMesh.GetBindposes();
            var bindposes = new float3x4[skin.bones.Length];
            var bonePaths = new string[bindposes.Length];
            
            for (int i = 0; i < skin.bones.Length; i++)
            {
                var bindpose = bindPoseArr[i];
                var bone = skin.bones[i];

                var localToBone = bone.localToWorldMatrix;
                bindposes[i] = new float3x4(
                    (Vector3)localToBone.GetColumn(0),
                    (Vector3)localToBone.GetColumn(1),
                    (Vector3)localToBone.GetColumn(2),
                    (Vector3)localToBone.GetColumn(3)
                    );

                bonePaths[i] = bone.GetHierarchyPath(skin.transform.root);
            }
            manifest.bonePaths = bonePaths;
            manifest.bones = skin.bones.Select(tr => tr.localToWorldMatrix).ToArray();
            manifest.bindposes = bindPoseArr.ToArray();
        }

        public static void FillBoneWeights(SkinnedMeshRenderer skin, AnimTextureManifest manifest)
        {
            var mesh = skin.sharedMesh;
            var bonesPerVertex = mesh.GetBonesPerVertex();

            var weights = mesh.GetAllBoneWeights();
            var bonesStartIndexPerVertex = mesh.GetBoneStartPerVertex();

            manifest.boneInfoPerVertices = bonesPerVertex
                    .Zip(bonesStartIndexPerVertex, (count, start) => new BoneInfoPerVertex { bonesCountPerVertex = count, bonesStartIndexPerVertex = start })
                    .ToArray();

            manifest.boneWeightArray = weights.ToArray();
        }

        public static List<int> GenBoneTexture(SkinnedMeshRenderer skin, List<AnimationClip> clipList, out Texture2D atlas)
        {
            var width = skin.bones.Length * 3;

            var yStartList = new List<int>();
            var yStart = 0;
            yStartList.Add(0);

            foreach (var clip in clipList)
            {
                yStart += (int)(clip.length * clip.frameRate);
                yStartList.Add(yStart);

            }
            atlas = new Texture2D(width, yStart, TextureFormat.RGBAHalf, false, true);
            atlas.filterMode = FilterMode.Point;

            return yStartList;
        }


        public static int BakeBoneClips(GameObject go, SkinnedMeshRenderer skin, List<AnimationClip> animClipList, AnimTextureManifest manifest, List<int> yList,ComputeShader bakeBoneCS)
        {
            var desc = new RenderTextureDescriptor(manifest.atlas.width, manifest.atlas.height,RenderTextureFormat.ARGBHalf, 0);
            desc.enableRandomWrite = true;
            desc.sRGB = false;
            var resultTex = new RenderTexture(desc);

            var index = 0;
            foreach (AnimationClip clip in animClipList)
            {
                var yStart = yList[index];

                //AnimTextureTools.BakeBonesToRT(skin, go, clip, bakeBoneCS, yStart, resultTex);

                var boneTex = AnimTextureTools.BakeBonesToTexture(skin, go, clip);
                var colors = boneTex.GetPixels(0, 0, boneTex.width, boneTex.height);
                
                manifest.atlas.SetPixels(0, yStart, boneTex.width, boneTex.height, colors);

                manifest.animInfos.Add(new AnimTextureClipInfo(clip.name, yStart, yList[index + 1])
                {
                    isLoop = clip.isLooping,
                    length = clip.length
                });
                index++;
            }

            //resultTex.ReadRenderTexture(ref manifest.atlas);

            return index;
        }
    }
}
#endif