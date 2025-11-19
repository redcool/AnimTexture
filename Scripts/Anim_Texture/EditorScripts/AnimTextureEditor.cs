#if UNITY_EDITOR
namespace AnimTexture
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using System.Linq;
    using System.IO;
    using System.Text;
    using PowerUtilities;

    public partial class AnimTextureEditor
    {
        public const string POWER_UTILS_MENU = "PowerUtilities/"+ ANIM_TEXTURE_PATH;
        //if you change AnimTexture path, need change this path.
        public const string ANIM_TEXTURE_PATH = "AnimTexture";
        public const string DEFAULT_TEX_DIR = ANIM_TEXTURE_PATH+"/AnimTexPath";
        public const string ASSET_DEFAULT_TEX_DIR = "Assets/"+DEFAULT_TEX_DIR;

        /// <summary>
        /// Bake animTex from selected objects,
        /// (contains SkinnedMeshRenderer,
        /// lots of AnimationClips
        /// </summary>
        /// <param name="gos"></param>
        /// <returns></returns>
        [MenuItem(POWER_UTILS_MENU + "/BakeAnimTexAtlas_From_GenericAnimType")]
        public static void BakeAnimTexFromSelected()
        {
            var objs = Selection.GetFiltered<GameObject>(SelectionMode.DeepAssets);

            BakeAnimTexFromObjs(objs);
        }

        /// <summary>
        /// baked animation object to boneTexture
        /// </summary>
        /// <param name="objs"></param>
        /// <param name="finalTargetFolder">save in folder</param>
        public static void BakeAnimTexFromObjs(GameObject[] objs, bool isSaveInPrefabFolder=false)
        {
            var skinnedMeshGo = objs.Where(obj => obj.GetComponentInChildren<SkinnedMeshRenderer>()).FirstOrDefault();
            if (!skinnedMeshGo)
            {
                EditorUtility.DisplayDialog("Warning", $" not found SkinnedMeshRenderer from selected objects", "ok");
                return;
            }
            
            var saveFolder = ASSET_DEFAULT_TEX_DIR;
            foreach (var obj in objs)
            {
                if (isSaveInPrefabFolder)
                    saveFolder = AssetDatabaseTools.GetAssetFolder(obj, ASSET_DEFAULT_TEX_DIR);

                //1 check animationClip
                List<AnimationClip> clipList = GetAnimationClipsFromAssetOrAnimation(obj);

                var clipCount = BakeAllClips(obj, clipList, saveFolder);
                ShowResult(skinnedMeshGo, clipCount);
            }

            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(saveFolder));
        }

        public static List<AnimationClip> GetAnimationClipsFromAssetOrAnimation(params GameObject[] objs)
        {
            var clipList = GetAnimationClipsFromAssets(objs);
            if (clipList.Count() == 0)
            {
                clipList = GetAnimstionClipFromAnimation(objs);
            }

            return clipList;
        }

        public static List<AnimationClip> GetAnimstionClipFromAnimation(params GameObject[] objs)
        {
            return objs.SelectMany(obj => AnimationUtility.GetAnimationClips(obj)).ToList();
        }

        public static List<AnimationClip> GetAnimationClipsFromAssets(params GameObject[] objs)
        {
            return objs.SelectMany(obj => AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(obj)))
                .Where(a => a is AnimationClip c && !c.name.StartsWith("__preview__"))
                .Select(a => (AnimationClip)a)
                .ToList();
        }

        static void ShowResult(GameObject go,int clipCount)
        {
            if (clipCount == 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine("AnimationClips cannt found! Checks:");
                sb.AppendLine("1 (Mesh or AnimationMesh)'s Animation Type (Legacy or Generic)");
                sb.AppendLine("2 Animation Component's Animations maybe has nothing!");
                Debug.Log(sb);
            }
            else
                Debug.Log($"{go} ,clips:{clipCount}");
        }

        public static int BakeAllClips(GameObject obj,List<AnimationClip> clipList,string saveFolder=null)
        {
            saveFolder = CreateSaveFolder(saveFolder);

            var skin = obj.GetComponentInChildren<SkinnedMeshRenderer>();

            var manifest = ScriptableObject.CreateInstance<AnimTextureManifest>();
            var yList = GenerateAtlas(skin, clipList, out manifest.atlas);
            var count = BakeClip(obj, skin, clipList, manifest, yList);
            manifest.atlas.Apply();

            //output atlas
            AssetDatabase.CreateAsset(manifest.atlas, $"{saveFolder}/{obj.name}_AnimTexture.asset");
            //output infos
            AssetDatabase.CreateAsset(manifest, $"{saveFolder}/{obj.name}_{typeof(AnimTextureManifest).Name}.asset");

            AssetDatabase.Refresh();
            return count;
        }

        private static int BakeClip(GameObject go, SkinnedMeshRenderer skin, List<AnimationClip> animClipList, AnimTextureManifest manifest, List<int> yList)
        {
            var index = 0;
            foreach (AnimationClip clip in animClipList)
            {
                //tex
                var y = yList[index];
                var tex = AnimTextureTools.BakeMeshToTexture(skin, go, clip);
                manifest.atlas.SetPixels(0, y, tex.width, tex.height, tex.GetPixels());
                Object.DestroyImmediate(tex);

                manifest.animInfos.Add(new AnimTextureClipInfo(clip.name, y, yList[index + 1])
                {
                    frameRate = clip.frameRate,
                    isLoop = clip.isLooping,
                    length = clip.length
                });
                index++;
            }

            return index;
        }
        /// <summary>
        /// Get a texture atlas from skinnedMeshRenderer
        /// 
        ///  widch: vertexCount , less count less width
        ///  height: sum of (clipLength * clip.frameRate) , less frameRate less height
        /// </summary>
        /// <param name="skin"></param>
        /// <param name="clipList"></param>
        /// <param name="atlas"></param>
        /// <returns></returns>
        static List<int> GenerateAtlas(SkinnedMeshRenderer skin, List<AnimationClip> clipList, out Texture2D atlas)
        {
            var yList = new List<int>();
            var width = skin.sharedMesh.vertexCount;
            var y = 0;
            yList.Add(0);

            foreach (var clip in clipList)
            {
                y += (int)(clip.length * clip.frameRate);
                yList.Add(y);
            }
            atlas = new Texture2D(width, y, TextureFormat.RGBAHalf, false);
            atlas.filterMode = FilterMode.Point;
            return yList;
        }

        public static string GetManifestPath(string goName)
        {
            string path = $"Assets/{DEFAULT_TEX_DIR}/{goName}_{typeof(AnimTextureManifest).Name}.asset";
            return path;
        }
    }
}
#endif