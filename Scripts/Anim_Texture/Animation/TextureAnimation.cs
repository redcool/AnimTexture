﻿namespace AnimTexture
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

        [Header("AnimTexture Playing Info")]
        //=============================================================== info
        [Tooltip("include animation info,bone info per vertex")]
        public AnimTextureManifest manifest;
        public float playTime;
        public float offsetPlayTime;

        [Tooltip("unchecked use meshRenderer sharedMaterial when playing")]
        public bool isUseMaterialInst = true;

        [Tooltip("srp batch will failed, when use block")]
        public bool isUpdateBlock;
        bool isLastUpdateBlock;
        public float speed = 1;

        MaterialPropertyBlock block;
        public MeshRenderer[] mrs;
        public Material[] mats;

        Coroutine crossLerpCoroutine;
        bool needUpdateBlock;

        Dictionary<int, int> clipNameHashDict = new Dictionary<int, int>();
        [Tooltip("fading out clip info")]
        public AnimTextureClipInfo curClipInfo;
        [Tooltip("fading in clip info")]
        public AnimTextureClipInfo nextClipInfo;


        //=============================================================== Debug
        //[EditorGroup("Components", true)]
        [EditorButton(onClickCall = "AddTextureAnimationSetup", tooltip = "add TextureAnimationSetup for setup AnimTexture components")]
        public bool isAddTextureAnimationSetup;

        //[EditorGroup("Debug",true)]
        [EditorButton(onClickCall = "Awake", tooltip = "call Awake")]
        public bool isCallAwake;

        //[EditorGroup("Debug")]
        [EditorButton(onClickCall = "Update", tooltip = "call Update")]
        public bool isCallUpdate;

        //[EditorGroup("Debug")]
        public int curIndex;

        //[EditorGroup("Debug")]
        public int nextIndex;

        //[EditorGroup("Debug")]
        public float crossFadeTime = 0.5f;

        //[EditorGroup("Debug")]
        //[EditorButton(onClickCall = "TestCrossFade")]
        public bool crossTest;

        void AddTextureAnimationSetup()
        {
            gameObject.GetOrAddComponent<TextureAnimationSetup>();
        }
        public void TestCrossFade()
        {
            CrossFade(curIndex, nextIndex, crossFadeTime);
        }

        // Start is called before the first frame update
        void Awake()
        {
            if (!manifest)
            {
                Debug.Log("manifest is missing");
                return;
            }

            SetupMeshRenderer();

            if (block == null)
                block = new MaterialPropertyBlock();

            SetupDict();

            Play(curIndex);

        }

        public void SetupMeshRenderer()
        {
            mrs = GetComponentsInChildren<MeshRenderer>();
            mats = new Material[mrs.Length];
            for (int i = 0; i < mrs.Length; i++)
            {
                var mr = mrs[i];
                mats[i] = (Application.isPlaying && !isUpdateBlock )? mr.material : mr.sharedMaterial;  // new instance
                if (manifest.atlas)
                    mats[i].SetTexture(ID_ANIM_TEX, manifest.atlas);
            }
        }

        public float GetPlayTime() => Time.deltaTime * speed;

        // Update is called once per frame
        void Update()
        {
            if (!manifest)
                return;

            playTime += GetPlayTime();
            UpdatePlayTime();
            UpdateAnimLoop();

            if (isUpdateBlock && needUpdateBlock)
            {
                needUpdateBlock = false;

                foreach (var mr in mrs)
                    mr.SetPropertyBlock(block);
            }
            if(isUpdateBlock != isLastUpdateBlock)
            {
                isLastUpdateBlock = isUpdateBlock;

                if(!isUpdateBlock)
                {
                    foreach (var mr in mrs)
                    {
                        mr.SetPropertyBlock(null);
                    }
                }
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

            foreach (var mat in mats)
            {
                mat.SetFloat(startNameHash, clipInfo.startFrame, block);
                mat.SetFloat(endNameHash, clipInfo.endFrame, block);

            }
            needUpdateBlock = true;
            return clipInfo;
        }
        void UpdatePlayTime()
        {
            foreach (var mat in mats)
            {
                mat.SetFloat(ID_PLAY_TIME, playTime, block);
                mat.SetFloat(ID_OFFSET_PLAY_TIME, offsetPlayTime, block);
            }
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

                    foreach (var mat in mats)
                        mat.SetFloat(ID_LOOP, 1, block);
                }
            }
            else
            {
                foreach (var mat in mats)
                    mat.SetFloat(ID_LOOP, 0, block);

            }
            needUpdateBlock = true;
        }

        void UpdateCrossLerp(float lerp)
        {
            foreach (var mat in mats)
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
                crossLerp += speed * GetPlayTime();
                yield return 0;
            }
            UpdateCrossLerp(1);
            StopCoroutine(crossLerpCoroutine);
        }
    }
}