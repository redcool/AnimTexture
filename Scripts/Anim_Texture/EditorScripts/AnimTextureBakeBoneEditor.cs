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

        public const string BAKE_BONE_CS_FILENAME = "BakeBoneMatrix";

        [MenuItem(POWER_UTILS_MENU + "/BakeBoneTexAtlas")]
        public static void BakeBoneTexFromSelected()
        {
            var objs = Selection.GetFiltered<GameObject>(SelectionMode.DeepAssets);
            BakeBoneTexture(objs);
        }
        public static void BakeBoneTexture(GameObject[] objs, bool isSaveInObjFolder = false)
        {
            var hasSkinned = objs.Where(obj => obj.GetComponentInChildren<SkinnedMeshRenderer>()).FirstOrDefault();
            if (! hasSkinned)
            {
                EditorUtility.DisplayDialog("Warning", $" not found SkinnedMeshRenderer from selected objects", "ok");
                return;
            }

            // find BakeBoneMatrix.compute
            var bakeBoneCS = AssetDatabaseTools.FindAssetPathAndLoad<ComputeShader>(out _, BAKE_BONE_CS_FILENAME, ".compute");
            if (!bakeBoneCS)
                throw new FileNotFoundException("cannot found compute shader : BakeBone");

            var saveFolder = $"Assets/{DEFAULT_TEX_DIR}";

            for (int i = 0; i < objs.Length; i++)
            {
                var obj = objs[i];
                if (isSaveInObjFolder)
                    saveFolder = AssetDatabaseTools.GetAssetFolder(obj);

                var skinnedMesh = obj.GetComponentInChildren<SkinnedMeshRenderer>();
                //1 check animationClip
                var clipList = GetAnimationClipsFromAssetOrAnimation(obj);

                var clipCount = BakeBoneAllClips(obj, clipList, bakeBoneCS, bakeBoneCS.CanExecute(), saveFolder);
                ShowResult(obj, clipCount);
            }
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(saveFolder));
        }

        /// <summary>
        /// Create save folder(default or saveFolder)
        /// </summary>
        /// <param name="saveFolder"></param>
        /// <returns></returns>
        public static string CreateSaveFolder(string saveFolder)
        {
            saveFolder = string.IsNullOrEmpty(saveFolder) ? $"Assets/{DEFAULT_TEX_DIR}" : saveFolder;
            PathTools.CreateAbsFolderPath(saveFolder);
            return saveFolder;
        }

        public static int BakeBoneAllClips(GameObject obj, List<AnimationClip> clipList, ComputeShader bakeBondCS, bool isUseCS, string saveFolder)
        {
            saveFolder = CreateSaveFolder(saveFolder);

            // create manifest
            var couunt = 0;
            var skin = obj.GetComponentInChildren<SkinnedMeshRenderer>();

            var manifest = ScriptableObject.CreateInstance<AnimTextureManifest>();
            FillBoneWeights(skin, manifest);
            FillBones(skin, manifest);

            var yList = GenBoneTexture(skin, clipList, out manifest.atlas);
            couunt += BakeBoneClips(obj, skin, clipList, manifest, yList, bakeBondCS, isUseCS);
            manifest.atlas.Apply();

            //output infos
            if (manifest.atlas)
                AssetDatabase.CreateAsset(manifest.atlas, $"{saveFolder}/{obj.name}_BoneTexture.asset");

            AssetDatabase.CreateAsset(manifest, $"{saveFolder}/{obj.name}_{typeof(AnimTextureManifest).Name}.asset");

            AssetDatabase.Refresh();
            return couunt;
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


        public static int BakeBoneClips(GameObject go, SkinnedMeshRenderer skin, List<AnimationClip> animClipList, AnimTextureManifest manifest, List<int> yList, ComputeShader bakeBoneCS, bool isUseCS)
        {
            var desc = new RenderTextureDescriptor(manifest.atlas.width, manifest.atlas.height, RenderTextureFormat.ARGBHalf, 0);
            desc.enableRandomWrite = true;
            desc.sRGB = false;
            var resultRT = new RenderTexture(desc);

            var index = 0;
            foreach (AnimationClip clip in animClipList)
            {
                var yStart = yList[index];
                if (isUseCS)
                {
                    AnimTextureTools.BakeBonesToRT(skin, go, clip, bakeBoneCS, yStart, resultRT);
                }
                else
                {
                    // bake a clip to texture, write it to manifest.atlas block
                    var boneTex = AnimTextureTools.BakeBonesToTexture(skin, go, clip);
                    var colors = boneTex.GetPixels(0, 0, boneTex.width, boneTex.height);
                    manifest.atlas.SetPixels(0, yStart, boneTex.width, boneTex.height, colors);
                }

                // record clip info
                manifest.animInfos.Add(new AnimTextureClipInfo(clip.name, yStart, yList[index + 1])
                {
                    frameRate = clip.frameRate,
                    isLoop = clip.isLooping,
                    length = clip.length
                });

                index++;
            }

            if (isUseCS)
            {
                resultRT.ReadRenderTexture(ref manifest.atlas);
            }

            return index;
        }
    }
}
#endif