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
        readonly int ID_ANIM_TEX = Shader.PropertyToID("_AnimTex");
        readonly int ID_LOOP = Shader.PropertyToID("_Loop");
        readonly int ID_CROSS_LERP = Shader.PropertyToID("_CrossLerp");
        readonly int ID_START_FRAME = Shader.PropertyToID("_StartFrame");
        readonly int ID_END_FRAME = Shader.PropertyToID("_EndFrame");
        readonly int ID_NEXT_START_FRAME = Shader.PropertyToID("_NextStartFrame");
        readonly int ID_NEXT_END_FRAME = Shader.PropertyToID("_NextEndFrame");
        readonly int ID_PLAY_TIME = Shader.PropertyToID("_PlayTime");
        readonly int ID_OFFSET_PLAY_TIME = Shader.PropertyToID("_OffsetPlayTime");

        public AnimTextureManifest manifest;
        public float playTime;
        public float offsetPlayTime;
        public bool isUpdateBlock;

        Renderer r;
        MaterialPropertyBlock block;
        Material mat;

        Coroutine crossLerpCoroutine;
        bool needUpdateBlock;

        Dictionary<int, int> clipNameHashDict = new Dictionary<int, int>();
        public AnimTextureClipInfo curClipInfo;
        public AnimTextureClipInfo nextClipInfo;

        [EditorButton(onClickCall ="Awake")]
        public bool isCallAwake;

        [EditorButton(onClickCall = "Update")]

        public bool isCallUpdate;
        
        
        [Header("Test")]
        public int curIndex;
        public int nextIndex;
        public float crossFadeTime = 1;
        public bool crossTest;

        // Start is called before the first frame update
        void Awake()
        {
            r = GetComponent<Renderer>();
            mat = Application.isPlaying ? r.material : r.sharedMaterial;  // new instance

            if (manifest.atlas)
                r.sharedMaterial.SetTexture(ID_ANIM_TEX, manifest.atlas);

            if (block == null)
                block = new MaterialPropertyBlock();

            SetupDict();

            Play(curIndex);

        }

        void UpdateBoneInfo()
        {
            if (!manifest)
                return;
            manifest.SendBoneBuffer(mat);
            manifest.SendBoneArray(mat);
        }

        // Update is called once per frame
        void Update()
        {
            playTime += Time.deltaTime;
            UpdatePlayTime();
            UpdateAnimLoop();
            UpdateBoneInfo();

            if (crossTest)
            {
                crossTest = false;

                //Play(animIndex);
                CrossFade(curIndex, nextIndex, crossFadeTime);
            }

            if (isUpdateBlock && needUpdateBlock)
            {
                needUpdateBlock = false;
                r.SetPropertyBlock(block);
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