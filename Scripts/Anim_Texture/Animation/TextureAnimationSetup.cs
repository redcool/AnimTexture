using PowerUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AnimTexture
{
    /// <summary>
    /// Setup 
    /// TextureAnimation
    /// SkinnedMeshRenderer to MeshRenderer
    /// 
    /// </summary>
    public class TextureAnimationSetup : MonoBehaviour
    {
        const string helpBox = "Replace SkinnedMeshRenderer to MeshRenderer,1 replace skinned material to AnimTexture mat";
        TextureAnimation texAnim;
        //=============================================================== FastSetup
        [HelpBox]
        public string setupHelpBox = helpBox;

        [Tooltip("setup AnimTexture")]
        [EditorNotNull]
        public AnimTextureManifest animTextureManifest;

        [Tooltip("for Animator play animation states")]
        [LoadAsset("AnimTexSimpleController_noClip")]
        [EditorNotNull]
        public RuntimeAnimatorController animatorController;

        [Tooltip("mat for each SkinnedMeshRenderer(AnimTex or BoneTexture)")]
        [EditorNotNull]
        public Material[] animTextureMats;


        [EditorButton(onClickCall = "SetupAnimTexture")]
        public bool isSetupAnimTex;

        [Header("Current Skinned")]
        public SkinnedMeshRenderer[] skinnedMeshes;

        [EditorButton(onClickCall = "FindSkinned")]
        public bool isFindSkinned;

        void FindSkinned()
        {
            skinnedMeshes = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        }

        public void SetupAnimTexture()
        {
            // remove old components
            gameObject.DestroyComponent<MeshRenderer>();
            gameObject.DestroyComponent<MeshFilter>();
            //
            texAnim = gameObject.GetOrAddComponent<TextureAnimation>();

            texAnim.manifest = animTextureManifest;

            skinnedMeshes = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (skinnedMeshes.Length == 0)
            {
                Debug.Log("SkinnedMeshRenderer not found");
                return;
            }
            if(skinnedMeshes.Length > animTextureMats.Length)
            {
                Debug.Log("SkinnedMeshes.length must < animTextureMats.Length");
                return;
            }


            SetupChildMeshRenderers(skinnedMeshes);
            texAnim.SetupMeshRenderer();
            transform.localScale = Vector3.one;

            var anim = gameObject.GetOrAddComponent<Animator>();
            anim.runtimeAnimatorController = animatorController;

            gameObject.GetOrAddComponent<AnimatorControl>();
        }

        private void SetupChildMeshRenderers(SkinnedMeshRenderer[] skinnedMeshes)
        {
            var parentTr = transform.Find("MeshRendererGO");
            if (parentTr)
            {
                parentTr.gameObject.DestroyChildren();
            }
            else
            {
                parentTr = new GameObject("MeshRendererGO").transform;
                parentTr.SetParent(transform, false);
            }

            for (int i = 0; i < skinnedMeshes.Length; i++)
            {
                var skinned = skinnedMeshes[i];
                var animTexMat = i < animTextureMats.Length ? animTextureMats[i] : default;

                var childTr = new GameObject(skinned.name).transform;
                childTr.parent = parentTr.transform;
                childTr.position = Vector3.zero;

                var mr = childTr.gameObject.GetOrAddComponent<MeshRenderer>();
                //mr.sharedMaterials = skinned.sharedMaterials;
                mr.sharedMaterial = animTexMat;

                var mf = childTr.gameObject.GetOrAddComponent<MeshFilter>();
                mf.sharedMesh = skinned.sharedMesh;

                skinned.enabled = false;
            }
            
        }
    }
}
