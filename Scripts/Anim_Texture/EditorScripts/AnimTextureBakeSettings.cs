#if UNITY_EDITOR
using PowerUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AnimTexture
{
    /// <summary>
    /// Show bake options in ProjectSetting/Power_Utils/AnimTextureBakeSettings
    /// </summary>
    [ProjectSettingGroup(ProjectSettingGroupAttribute.POWER_UTILS + "/AnimTextureBakeSettings", isUseUIElment = false)]
    [SOAssetPath("Assets/PowerUtilities/AnimTextureBakeSettings.asset")]
    public class AnimTextureBakeSettings : ScriptableObject
    {
        [HelpBox]
        public string helpStr = "Select or Put gameObjects,when empty get objects from selection";

        [Tooltip("Bake target obj,check Selection.objects when empty,Get clips from Animation's controller or Animation")]
        public GameObject[] targetObjects;

        [Header("Bake AnimTexture(mesh texture or bone texture")]
        public AnimTextureBakeType bakeType = AnimTextureBakeType.BakeBone;

        [Tooltip("Baked animTexture save into targetGO folder")]
        public bool isSaveInObjFolder = true;

        [Header("Create Player")]
        public AnimTexPlayerType playerType = AnimTexPlayerType.Animator;

        [Tooltip("Destroy skinnedMeshRenderer")]
        public bool isDestroySkinnedMeshRenderer;

        [Tooltip("MeshRenderer sharedMaterial, need support animTexture")]
        public Material[] animTexMats;

        public bool isSavePlayerPrefab;

        [EditorButton(onClickCall = nameof(StartBake))]
        public bool isStartBake;

        private GameObject[] GetSelectedOrTargetS()
        {
            var objs = targetObjects != null ? targetObjects.Where(obj => obj).ToArray() : default;
            if (objs == null || objs.Length == 0)
                objs = Selection.GetFiltered<GameObject>(SelectionMode.DeepAssets);
            return objs;
        }

        public void StartBake()
        {
            var objs = GetSelectedOrTargetS();

            var players = AnimTextureEditor.StartBakeFlow(objs, bakeType, true, playerType, isDestroySkinnedMeshRenderer, animTexMats);
            var prefabFolders = objs.Select(obj => AssetDatabaseTools.GetAssetFolder(obj)).ToList();
            AnimTextureEditor.EndBakeFlow(players, prefabFolders, isSavePlayerPrefab);
        }
    }
}
#endif