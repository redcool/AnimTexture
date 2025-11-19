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
        public string helpStr = "Select or Put a gameObject";
        public GameObject targetGO;

        [Header("Bake Anim Texture")]
        [Tooltip("bake bone or bake mesh to texture")]
        public AnimTextureBakeType bakeType = AnimTextureBakeType.BakeBone;

        [Tooltip("Save bakedTexture in baked targetGO folder")]
        public bool isSaveInObjFolder;

        [Header("Create Player")]
        public AnimTexPlayerType playerType = AnimTexPlayerType.Animator;
        [Tooltip("destroy skinnedMeshRenderer when CreateAnimTexPlayer done ")]
        public bool isDestroySkinnedMeshRenderer;

        [Tooltip("materials support AnimTexture")]
        public Material[] animTexMats;

        [EditorButton(onClickCall = nameof(StartBake))]
        public bool isStartBake;

        private GameObject[] GetSelectedOrTarget()
        {
            var objs = Selection.GetFiltered<GameObject>(SelectionMode.DeepAssets);
            if (targetGO)
                objs = new[] { targetGO };
            return objs;
        }

        public void StartBake()
        {
            var objs = GetSelectedOrTarget();

            AnimTextureEditor.StartBakeFlow(objs, bakeType, true, playerType, isDestroySkinnedMeshRenderer, animTexMats);
        }
    }
}
#endif