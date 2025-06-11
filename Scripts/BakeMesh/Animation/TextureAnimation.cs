namespace AnimTexture
{
    using PowerUtilities;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using Unity.Mathematics;
    using UnityEngine;

    //[ExecuteAlways]
    
    public class TextureAnimation : MonoBehaviour
    {
        // anim tex material's variables
        readonly int ID_ANIM_TEX = Shader.PropertyToID("_AnimTex");
        readonly int ID_LOOP = Shader.PropertyToID("_Loop");
        readonly int ID_CROSS_LERP = Shader.PropertyToID("_CrossLerp");
        readonly int ID_START_FRAME = Shader.PropertyToID("_StartFrame");
        readonly int ID_END_FRAME = Shader.PropertyToID("_EndFrame");
        readonly int ID_NEXT_START_FRAME = Shader.PropertyToID("_NextStartFrame");
        readonly int ID_NEXT_END_FRAME = Shader.PropertyToID("_NextEndFrame");
        readonly int ID_PLAY_TIME = Shader.PropertyToID("_PlayTime");
        readonly int ID_OFFSET_PLAY_TIME = Shader.PropertyToID("_OffsetPlayTime");

        [Tooltip("include animation info,bone info per vertex")]
        public AnimTextureManifest manifest;
        public float playTime;
        public float offsetPlayTime;

        [Tooltip("unchecked use meshRenderer sharedMaterial when playing")]
        public bool isUseMaterialInst = true;

        [Tooltip("srp batch will failed, when use block")]
        public bool isUpdateBlock;

        MeshRenderer mr;
        MaterialPropertyBlock block;
        Material mat;

        Coroutine crossLerpCoroutine;
        bool needUpdateBlock;

        Dictionary<int, int> clipNameHashDict = new Dictionary<int, int>();
        [Tooltip("fading out clip info")]
        public AnimTextureClipInfo curClipInfo;
        [Tooltip("fading in clip info")]
        public AnimTextureClipInfo nextClipInfo;

        //==================FastSetup
        [EditorGroup("FastSetup",true)]
        [Tooltip("setup AnimTexture")]
        public AnimTextureManifest animTextureManifest;

        [EditorGroup("FastSetup")]
        [Tooltip("material for mesh renderer")]
        public Material boneTextureMat;

        [EditorGroup("FastSetup")]
        [LoadAsset("AnimTexSimpleController_noClip")]
        [Tooltip("for Animator play animation states")]
        public RuntimeAnimatorController animatorController;

        [EditorGroup("FastSetup")]
        [EditorButton(onClickCall = "SetupAnimTexture")]
        public bool isSetupAnimTex;

        //==================Debug
        [EditorGroup("Debug",true)]
        [EditorButton(onClickCall ="Awake")]
        public bool isCallAwake;

        [EditorGroup("Debug")]
        [EditorButton(onClickCall = "Update")]
        public bool isCallUpdate;

        [EditorGroup("Debug")]
        public int curIndex;

        [EditorGroup("Debug")]
        public int nextIndex;

        [EditorGroup("Debug")]
        public float crossFadeTime = 0.5f;

        [EditorGroup("Debug")]
        [EditorButton(onClickCall = "TestCrossFade")]
        public bool crossTest;

        public void SetupAnimTexture()
        {
            manifest = animTextureManifest;

            var skinned = gameObject.GetComponentInChildren<SkinnedMeshRenderer>();
            if (!skinned)
            {
                Debug.Log("SkinnedMeshRenderer not found");
                return;
            }

            skinned.Destroy();
            transform.localScale = Vector3.one;

            var mr = gameObject.GetOrAddComponent<MeshRenderer>();
            if (boneTextureMat)
                mr.sharedMaterial = boneTextureMat;
            else
                mr.sharedMaterials = skinned.sharedMaterials;

            var mf = gameObject.GetOrAddComponent<MeshFilter>();
            mf.sharedMesh = skinned.sharedMesh;

            var anim = gameObject.GetOrAddComponent<Animator>();
            anim.runtimeAnimatorController = animatorController;

            gameObject.GetOrAddComponent<AnimatorControl>();


        }

        public void TestCrossFade()
        {
            CrossFade(curIndex, nextIndex, crossFadeTime);
        }

        // Start is called before the first frame update
        void Awake()
        {
            // 1 check FastSetup 
            if (!manifest)
                manifest = animTextureManifest;

            //2 no manifest, disable self
            if (!manifest)
            {
                Debug.Log("manifest is missing");
                return;
            }

            mr = GetComponent<MeshRenderer>();
            if (manifest.atlas)
                mr.sharedMaterial.SetTexture(ID_ANIM_TEX, manifest.atlas);

            mat = Application.isPlaying ? mr.material : mr.sharedMaterial;  // new instance

            if (block == null)
                block = new MaterialPropertyBlock();

            SetupDict();

            Play(curIndex);

        }

        void UpdateBoneInfo()
        {
            manifest.SendBoneBuffer(mat);
            manifest.SendBoneArray(mat);
        }

        // Update is called once per frame
        void Update()
        {
            if (!manifest)
                return;

            playTime += Time.deltaTime;
            UpdatePlayTime();
            UpdateAnimLoop();
            UpdateBoneInfo();

            if (isUpdateBlock && needUpdateBlock)
            {
                needUpdateBlock = false;
                mr.SetPropertyBlock(block);
            }
        }

        void SetupDict()
        {
            for (int i = 0; i < manifest.animInfos.Count; i++)
            {
                clipNameHashDict[manifest.animInfos[i].clipNameHash] = i;
            }
        }

        public int GetClipIndex(int stateNameHash)
        {
            if (clipNameHashDict.TryGetValue(stateNameHash, out int index))
                return index;
            return -1;
        }
        public int GetClipIndex(string stateName)
        {
            return GetClipIndex(Animator.StringToHash(stateName));
        }

        AnimTextureClipInfo UpdateAnimTime(int index, int startNameHash,int endNameHash)
        {
            if (index >= manifest.animInfos.Count)
                return default;

            var clipInfo = manifest.animInfos[index];

            mat.SetFloat(startNameHash, clipInfo.startFrame, block);
            mat.SetFloat(endNameHash, clipInfo.endFrame, block);
            needUpdateBlock = true;
            return clipInfo;
        }
        void UpdatePlayTime()
        {
            mat.SetFloat(ID_PLAY_TIME, playTime,block);
            mat.SetFloat(ID_OFFSET_PLAY_TIME, offsetPlayTime,block);
            needUpdateBlock = true;
        }

        void UpdateAnimLoop()
        {
            if (!curClipInfo.isLoop)
            {
                if (playTime > curClipInfo.length)
                {
                    //var loopLerp = block.GetFloat(ID_LOOP);
                    //Debug.Log(playTime + ":" + curClipInfo.length);
                    mat.SetFloat(ID_LOOP, 1,block);
                }
            }
            else
                mat.SetFloat(ID_LOOP, 0, block);
            needUpdateBlock = true;
        }

        void UpdateCrossLerp(float lerp)
        {
            mat.SetFloat(ID_CROSS_LERP, lerp, block);
            needUpdateBlock = true;
        }

        public void Play(int index)
        {
            playTime = 0;
            offsetPlayTime = 0;

            curClipInfo = UpdateAnimTime(index, ID_START_FRAME, ID_END_FRAME);
            nextClipInfo = UpdateAnimTime(index, ID_NEXT_START_FRAME, ID_NEXT_END_FRAME);
            UpdateCrossLerp(1);
        }

        public void Play(string clipName)
        {
            var index = GetClipIndex(clipName);
            if (index < 0)
                return;

            Play(index);
        }
        /// <summary>
        /// crossFade play,
        /// </summary>
        /// <param name="index">fade out clip index</param>
        /// <param name="nextIndex">fade in clip index</param>
        /// <param name="fadeTime">crassFading time</param>
        public void CrossFade(int index,int nextIndex,float fadeTime)
        {
            playTime = 0;

            var animInfo = manifest.animInfos[index];
            offsetPlayTime = animInfo.length - fadeTime;

            curClipInfo = UpdateAnimTime(index, ID_START_FRAME, ID_END_FRAME);
            nextClipInfo = UpdateAnimTime(nextIndex, ID_NEXT_START_FRAME, ID_NEXT_END_FRAME);

            if(crossLerpCoroutine != null)
                StopCoroutine(crossLerpCoroutine);

            crossLerpCoroutine = StartCoroutine(WaitForUpdateCrossLerp(nextIndex,fadeTime));
        }
        /// <summary>
        /// crossFade play,
        /// </summary>
        /// <param name="clipName">fade out clipName</param>
        /// <param name="nextClipName">fade in clipName</param>
        /// <param name="fadeTime">crassFading time</param>
        public void CrossFade(string clipName, string nextClipName, float fadeTime)
        {
            var index = GetClipIndex(clipName);
            var nextIndex = GetClipIndex(nextClipName);
            if (index < 0 || nextIndex < 0)
                return;

            CrossFade(index, nextIndex, fadeTime);
        }

        IEnumerator WaitForUpdateCrossLerp(int index,float fadeTime)
        {
            if (fadeTime <= 0)
            {
                UpdateCrossLerp(1);
                yield break;
            }

            var speed = 1.0f / fadeTime;
            var crossLerp = 0f;

            while (crossLerp < 1)
            {
                UpdateCrossLerp(crossLerp);
                crossLerp += speed * Time.deltaTime;
                yield return 0;
            }
            UpdateCrossLerp(1);
            StopCoroutine(crossLerpCoroutine);
        }
    }
}