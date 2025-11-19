namespace AnimTexture
{
    using PowerUtilities;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
#if UNITY_EDITOR
    using UnityEditor;
#endif
    using UnityEngine;
    using Object = UnityEngine.Object;

#if UNITY_EDITOR
    [CustomEditor(typeof(BakeAnimTexture))]
    public class BakeAnimTextureEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var inst = target as BakeAnimTexture;

            if (GUILayout.Button("Bake AnimTexture"))
            {
                var targetGO = inst.targetGO ? inst.targetGO : Selection.activeGameObject;
                if (!targetGO)
                {
                    Debug.Log($"nothing selected {targetGO}");
                    return;
                }
                var objs = new[] { targetGO };

                AnimTextureEditor.StartBakeFlow(objs,inst.bakeType,true, inst.playerType, inst.isDestroySkinnedMeshRenderer, inst.animTexMats);
            }
        }

    }
#endif
    public enum AnimTextureBakeType { BakeBone,BakeMesh }
    public enum AnimTexPlayerType { None,Animator,SimpleAnimation}

    public class BakeAnimTexture : MonoBehaviour
    {

        [Header("Bake AnimTexture")]
        public AnimTextureBakeType bakeType = AnimTextureBakeType.BakeBone;

        [Header("Create Player")]
        public AnimTexPlayerType playerType = AnimTexPlayerType.Animator;

        public bool isDestroySkinnedMeshRenderer;

        [Tooltip("material support animTexture")]
        public Material[] animTexMats;

        public GameObject targetGO;
    }
}
