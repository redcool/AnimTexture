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
        const string helpBox = "Replace SkinnedMeshRenderer to MeshRenderer, \n1 replace skinned material to AnimTexture mat";
        TextureAnimation texAnim;
        //=============================================================== FastSetup
        [HelpBox]
        public string setupHelpBox = helpBox;

        [Tooltip("setup AnimTexture")]
        [EditorNotNull]
        public AnimTextureManifest animTextureManifest;

        [EditorNotNull]
        [Tooltip("for Animator play animation states,AnimatorControl need only")]
        [LoadAsset("AnimTexSimpleController_noClip")]
        public RuntimeAnimatorController animatorController;

        [Tooltip("mat for each SkinnedMeshRenderer(AnimTex or BoneTexture)")]
        [EditorNotNull]
        public Material[] animTextureMats;
        [Tooltip("open material's keyword")]
        public string animTexKeyword = "_ANIM_TEX_ON";

        [EditorButton(onClickCall = nameof(SetupAnimTexture))]
        public bool isSetupAnimTex;

        [Header("Current Skinned")]
        public SkinnedMeshRenderer[] skinnedMeshes;

        [EditorButton(onClickCall = nameof(FindSkinned))]
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
            //if(skinnedMeshes.Length > animTextureMats.Length)
            //{
            //    Debug.Log("SkinnedMeshes.length must < animTextureMats.Length");
            //    return;
            //}

            SetupChildMeshRenderers(skinnedMeshes);
            texAnim.SetupMeshRenderer();
            transform.localScale = Vector3.one;

            var anim = gameObject.GetComponent<Animator>();
            // use Animator
            if (anim)
            {
                anim.runtimeAnimatorController = animatorController;
                gameObject.GetOrAddComponent<AnimatorControl>();
            }
        }

        private void SetupChildMeshRenderers(SkinnedMeshRenderer[] skinnedMeshes)
        {
            var meshRendererTr = transform.Find("MeshRendererGO");
            if (meshRendererTr)
            {
                meshRendererTr.gameObject.DestroyChildren();
            }
            else
            {
                meshRendererTr = new GameObject("MeshRendererGO").transform;
                meshRendererTr.SetParent(transform, false);
            }

            for (int i = 0; i < skinnedMeshes.Length; i++)
            {
                var skinned = skinnedMeshes[i];
                var animTexMat = i < animTextureMats.Length ? animTextureMats[i] : default;

                if (animTexMat)
                    animTexMat.EnableKeyword(animTexKeyword);

                meshRendererTr.position = Vector3.zero;

                var mr = meshRendererTr.gameObject.GetOrAddComponent<MeshRenderer>();
                //mr.sharedMaterials = skinned.sharedMaterials;
                mr.sharedMaterial = animTexMat ?? skinned.share;

                var mf = meshRendererTr.gameObject.GetOrAddComponent<MeshFilter>();
                mf.sharedMesh = skinned.sharedMesh;

                skinned.enabled = false;
            }
            
        }
    }
}
