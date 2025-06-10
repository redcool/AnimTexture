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

        public GameObject targetGO;

        [EditorBox("Bake Type", "isBakeMesh,isBakeBones",boxType = EditorBoxAttribute.BoxType.HBox)]
        [Tooltip("bake selected skinnedMeshRenderer mesh to texture")]
        [EditorButton(onClickCall ="BakeMesh")]
        public bool isBakeMesh;

        
        [Tooltip("bake selected skinnedMeshRenderer bones to texture")]
        [EditorButton(onClickCall = "BakeBone")]
        [HideInInspector]
        public bool isBakeBones;

        [EditorBox("AnimTexPlayer", "isCreateAnimTexPlayer", boxType = EditorBoxAttribute.BoxType.HBox)]
        [Tooltip("create new TextureAnimation player from selected object")]
        [EditorButton(onClickCall = "CreateAnimTexPlayer")]
        public bool isCreateAnimTexPlayer;


        public void BakeMesh()
        {
            GameObject[] objs = GetSelectedOrTarget();

            AnimTextureEditor.BakeAnimTexFromObjs(objs);
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
            AnimTextureEditor.BakeBoneTexture(objs);
        }
        public void CreateAnimTexPlayer()
        {
            var objs = Selection.GetFiltered<GameObject>(SelectionMode.Assets);
            AnimTexturePlayerCreator.CreatePlayer(objs);
        }
    }
}
#endif