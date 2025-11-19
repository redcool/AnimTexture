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
        public const string POWER_UTILS_MENU = "PowerUtilities/" + ANIM_TEXTURE_PATH;
        //if you change AnimTexture path, need change this path.
        public const string ANIM_TEXTURE_PATH = "AnimTexture";
        public const string DEFAULT_TEX_DIR = ANIM_TEXTURE_PATH + "/AnimTexPath";
        public const string ASSET_DEFAULT_TEX_DIR = "Assets/" + DEFAULT_TEX_DIR;

        /// <summary>
        /// Bake animTex from selected objects,
        /// <returns></returns>
        [MenuItem(POWER_UTILS_MENU + "/BakeMeshTexture")]
        public static void BakeMeshTextureFromSelected()
        {
            var objs = Selection.GetFiltered<GameObject>(SelectionMode.DeepAssets);
            BakeMeshTexture(objs, false, out var _);
        }

        /// <summary>
        /// baked animation object to mesh texture
        /// </summary>
        /// <param name="objs"></param>
        /// <param name="finalTargetFolder">save in folder</param>
        public static void BakeMeshTexture(GameObject[] objs, bool isSaveInPrefabFolder, out AnimTextureManifest manifest)
        {
            manifest = null;

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

                //1 get animationClip
                var clipList = GetAnimationClipsFromAssetOrAnimation(obj);

                var clipCount = BakeAllClips(obj, clipList, saveFolder, out manifest);
                ShowResult(skinnedMeshGo, clipCount);
            }

            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(saveFolder));
        }

        public static int BakeAllClips(GameObject obj, List<AnimationClip> clipList, string saveFolder, out AnimTextureManifest manifest)
        {
            saveFolder = CreateSaveFolder(saveFolder);

            var skin = obj.GetComponentInChildren<SkinnedMeshRenderer>();

            manifest = ScriptableObject.CreateInstance<AnimTextureManifest>();
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

        public static int BakeClip(GameObject go, SkinnedMeshRenderer skin, List<AnimationClip> animClipList, AnimTextureManifest manifest, List<int> yList)
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
        public static string GetManifestFileName(string goName)
            => $"{goName}_{nameof(AnimTextureManifest)}.asset";

        public static string GetManifestPath(string goName)
            => $"Assets/{AnimTextureEditor.DEFAULT_TEX_DIR}/{GetManifestFileName(goName)}";


        public static List<AnimationClip> GetAnimationClipsFromAssetOrAnimation(GameObject obj)
        {
            var clipList = AnimationUtility.GetAnimationClips(obj).ToList();
            return clipList;
        }

        /// <summary>
        /// Create save folder(default or saveFolder)
        /// </summary>
        /// <param name="saveFolder"></param>
        /// <returns></returns>
        public static string CreateSaveFolder(string saveFolder)
        {
            saveFolder = string.IsNullOrEmpty(saveFolder) ? $"Assets/{AnimTextureEditor.DEFAULT_TEX_DIR}" : saveFolder;
            PathTools.CreateAbsFolderPath(saveFolder);
            return saveFolder;
        }

        public static void ShowResult(GameObject go, int clipCount)
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
        public static List<int> GenerateAtlas(SkinnedMeshRenderer skin, List<AnimationClip> clipList, out Texture2D atlas)
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

        /// <summary>
        /// Start bake anim texture with all options
        /// </summary>
        /// <param name="objs"></param>
        /// <param name="bakeType"></param>
        /// <param name="isSaveInPrefabFolder"></param>
        /// <param name="playerType"></param>
        /// <param name="isDestroySkinnedMesnRenderer"></param>
        /// <param name="animTexMats"></param>
        /// <returns></returns>
        public static bool StartBakeFlow(GameObject[] objs, AnimTextureBakeType bakeType, bool isSaveInPrefabFolder, AnimTexPlayerType playerType, bool isDestroySkinnedMesnRenderer, Material[] animTexMats)
        {
            AnimTextureManifest manifest = BakeAnimTexture(objs, bakeType, isSaveInPrefabFolder);

            var player1 = CreateAnimTexPlayers(playerType, objs, isDestroySkinnedMesnRenderer).FirstOrDefault();
            if (!player1)
                return false;
            SetupAnimTexPlayer(manifest, player1, animTexMats);
            return true;
        }

        public static AnimTextureManifest BakeAnimTexture(GameObject[] objs, AnimTextureBakeType bakeType, bool isSaveInPrefabFolder)
        {
            AnimTextureManifest manifest = default;
            switch (bakeType)
            {
                case AnimTextureBakeType.BakeBone:
                    BakeBoneTexture(objs, isSaveInPrefabFolder, out manifest);
                    break;
                case AnimTextureBakeType.BakeMesh:
                    BakeMeshTexture(objs, isSaveInPrefabFolder, out manifest);
                    break;
            }

            return manifest;
        }

        public static void SetupAnimTexPlayer(AnimTextureManifest manifest, GameObject animTexPlayerGO, Material[] animTexMats)
        {
            var setup = animTexPlayerGO.GetComponentInChildren<TextureAnimationSetup>();
            setup.animTextureManifest = manifest;
            setup.animTextureMats = animTexMats;
            setup.animatorController = animTexPlayerGO.GetComponent<Animator>()?.runtimeAnimatorController;
            setup.SetupAnimTexture();
        }

        public static List<GameObject> CreateAnimTexPlayers(AnimTexPlayerType playerType, GameObject[] objs, bool isDestroySkinnedMesnRenderer)
        {
            switch (playerType)
            {
                case AnimTexPlayerType.Animator:
                    return AnimTexturePlayerCreator.CreatePlayer(objs, isDestroySkinnedMesnRenderer);
                case AnimTexPlayerType.SimpleAnimation:
                    return AnimTexturePlayerCreator.CreatePlayerWithSimpleControl(objs, isDestroySkinnedMesnRenderer);
            }
            return default;
        }
    }
}
#endif