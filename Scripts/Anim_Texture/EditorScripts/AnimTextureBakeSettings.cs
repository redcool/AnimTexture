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

    [ProjectSettingGroup(ProjectSettingGroupAttribute.POWER_UTILS + "/AnimTextureBakeSettings", isUseUIElment = false)]
    [SOAssetPath("Assets/PowerUtilities/AnimTextureBakeSettings.asset")]
    public class AnimTextureBakeSettings : ScriptableObject
    {
        [EditorBorder(lineCount: 1, topColorStr: ColorTools.G_LIGHT_GREEN)]

        [HelpBox]
        public string helpStr = "Select or Put a gameObject";
        public GameObject targetGO;

        [Tooltip("Save bakedTexture in baked targetGO folder")]
        public bool isSaveInObjFolder;

        [EditorBox("Bake Type", "isBakeMesh,isBakeBones",boxType = EditorBoxAttribute.BoxType.HBox)]
        [EditorButton(onClickCall =nameof(BakeMesh),tooltip = "bake selected skinnedMeshRenderer mesh to texture")]
        public bool isBakeMesh;

        
        [EditorButton(onClickCall = nameof(BakeBone),tooltip = "bake selected skinnedMeshRenderer bones to texture")]
        [HideInInspector]
        public bool isBakeBones;

        [EditorBox("AnimTexPlayer", "isDestroySkinned,isCreateAnimTexPlayerWithAnimatorControl,isCreateAnimTexPlayerWithSimpleControl", boxType = EditorBoxAttribute.BoxType.VBox)]
        [Tooltip("destroy skinnedMeshRenderer when CreateAnimTexPlayer done ")]
        public bool isDestroySkinned;

        [HideInInspector]
        [EditorButton(onClickCall = nameof(CreateAnimTexPlayerWithAnimatorControl),tooltip = "create new TextureAnimation player from selected object")]
        public bool isCreateAnimTexPlayerWithAnimatorControl;

        [HideInInspector]
        [EditorButton(onClickCall = nameof(CreateAnimTexPlayerSimpleControl), tooltip = "create new TextureAnimation player from selected object")]
        public bool isCreateAnimTexPlayerWithSimpleControl;



        public void BakeMesh()
        {
            GameObject[] objs = GetSelectedOrTarget();

            AnimTextureEditor.BakeAnimTexFromObjs(objs,isSaveInObjFolder);
        }

        private GameObject[] GetSelectedOrTarget()
        {
            var objs = Selection.GetFiltered<GameObject>(SelectionMode.DeepAssets);
            if (targetGO)
                objs = new[] { targetGO };
            return objs;
        }

        public void BakeBone()
        {
            GameObject[] objs = GetSelectedOrTarget();
            AnimTextureEditor.BakeBoneTexture(objs,isSaveInObjFolder);
        }
        public void CreateAnimTexPlayerWithAnimatorControl()
        {
            var objs = Selection.GetFiltered<GameObject>(SelectionMode.Assets);
            var list = AnimTexturePlayerCreator.CreatePlayer(objs, isDestroySkinned);
            foreach (var obj in list)
            {
                obj.DestroyComponents<Animation>(true, true);
            }
        }
        public void CreateAnimTexPlayerSimpleControl()
        {
            var objs = Selection.GetFiltered<GameObject>(SelectionMode.Assets);
            var list = AnimTexturePlayerCreator.CreatePlayerWithSimpleControl(objs, isDestroySkinned);
            foreach (var obj in list)
            {
                obj.DestroyComponents<Animation>(true,true);
            }
        }
    }
}
#endif