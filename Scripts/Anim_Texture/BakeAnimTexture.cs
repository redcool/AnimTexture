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
    [CanEditMultipleObjects]
    [CustomEditor(typeof(BakeAnimTexture))]
    public class BakeAnimTextureEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Start Bake"))
            {
                var insts = targets.Select(t => t as BakeAnimTexture);
                foreach (var inst in insts)
                {
                    GameObject[] objs = GetTargets(inst);

                    var players = AnimTextureEditor.StartBakeFlow(objs, inst.bakeType, inst.isSaveInObjFolder, inst.playerType, inst.isDestroySkinnedMeshRenderer, inst.animTexMats);
                    var prefabFolders = objs.Select(obj => AssetDatabaseTools.GetAssetFolder(obj)).ToList();
                    AnimTextureEditor.EndBakeFlow(players, prefabFolders,inst.isSavePlayerPrefab,inst.playerType,inst.isDestroySkinnedMeshRenderer);
                }
            }
        }

        public static GameObject[] GetTargets(BakeAnimTexture inst)
        {
            var objs = inst.targetObjects != null ? inst.targetObjects.Where(obj => obj).ToArray() : default;
            if (objs == null || objs.Length == 0)
                objs = new GameObject[] { inst.gameObject };
            return objs;
        }
    }
#endif
    public enum AnimTextureBakeType { BakeBone,BakeMesh }
    public enum AnimTexPlayerType { None,TextureAnimationOnly,SimpleAnimation, Animator }

    public class BakeAnimTexture : MonoBehaviour
    {
        [Tooltip("Bake targetGO or self when empty, Get clips from Animation's controller or Animation")]
        public GameObject[] targetObjects;

        [Header("Bake AnimTexture(mesh texture or bone texture")]
        public AnimTextureBakeType bakeType = AnimTextureBakeType.BakeBone;

        [Tooltip("Baked animTexture save into targetGO folder")]
        public bool isSaveInObjFolder = true;

        [Header("Create Player")]
        [Tooltip("None: only bake,TextureAnimation: Only TextureAniation,SimpleControl : add SimpleAnimationControl ,Animator: add AnimationControl")]
        public AnimTexPlayerType playerType = AnimTexPlayerType.Animator;

        [Tooltip("Destroy skinnedMeshRenderer")]
        public bool isDestroySkinnedMeshRenderer;

        [Tooltip("MeshRenderer sharedMaterial, need support animTexture")]
        public Material[] animTexMats;

        public bool isSavePlayerPrefab;

    }
}
