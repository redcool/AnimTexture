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

        public int curIndex;
        public int nextIndex;
        public float crossFadeTime = 1;

        public bool crossTest;
        public float playTime;
        public float offsetPlayTime;

        Renderer r;
        MaterialPropertyBlock block;
        Material mat;

        Coroutine crossLerpCoroutine;
        bool needUpdateBlock;

        Dictionary<int, int> clipNameHashDict = new Dictionary<int, int>();
        public AnimTextureClipInfo curClipInfo;

        [EditorButton(onClickCall ="Awake")]
        public bool isCallAwake;

        [EditorButton(onClickCall = "Update")]
        public bool isCallUpdate;

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
        [Header("debug")]
        public SkinnedMeshRenderer skinned;
        GraphicsBuffer boneWeightBuffer,boneInfoPerVertexBuffer,bonesBuffer;
        void UpdateBoneArray(Material mat)
        {

            var bonesPerVertex = skinned.sharedMesh.GetBonesPerVertex().ToArray();
            var boneStartPerVertex = skinned.sharedMesh.GetBoneStartPerVertex();
            var boneWeightArray = skinned.sharedMesh.GetAllBoneWeights().ToArray();
            var bones = skinned.bones.Select((tr, id) => tr.localToWorldMatrix * skinned.sharedMesh.bindposes[id]).ToArray();

            mat.SetFloat("_BoneCount", bones.Length);
            //// ========================= send buffer
            GraphicsBufferTools.TryCreateBuffer(ref boneWeightBuffer, GraphicsBuffer.Target.Structured, boneWeightArray.Length,Marshal.SizeOf<BoneWeight1>());
            boneWeightBuffer.SetData(boneWeightArray);

            var boneInfoPerVertex = bonesPerVertex.
                Zip(boneStartPerVertex, (count, start) => new BoneInfoPerVertex { bonesCountPerVertex = count, bonesStartIndexPerVertex = start })
                .ToArray();
            GraphicsBufferTools.TryCreateBuffer(ref boneInfoPerVertexBuffer, GraphicsBuffer.Target.Structured, boneWeightArray.Length, Marshal.SizeOf<BoneInfoPerVertex>());
            boneInfoPerVertexBuffer.SetData(boneInfoPerVertex);

            GraphicsBufferTools.TryCreateBuffer(ref bonesBuffer, GraphicsBuffer.Target.Structured, bones.Length, Marshal.SizeOf<Matrix4x4>());
            bonesBuffer.SetData(bones);

            mat.SetBuffer("_BoneWeightBuffer", boneWeightBuffer);
            mat.SetBuffer("_BoneInfoPerVertexBuffer", boneInfoPerVertexBuffer);
            mat.SetBuffer("_Bones", bonesBuffer);
            //// ========================= send array
            mat.SetFloatArray("_BoneCountArray", bonesPerVertex.Select(Convert.ToSingle).ToArray());
            mat.SetFloatArray("_BoneStartArray", boneStartPerVertex.Select(Convert.ToSingle).ToArray());
            mat.SetFloatArray("_BoneWeightArray", boneWeightArray.Select(bw => bw.weight).ToArray());
            mat.SetFloatArray("_BoneWeightIndexArray", boneWeightArray.Select(bw => (float)bw.boneIndex).ToArray());

            //var bones = skin.bones.Select((tr, id) => skin.transform.worldToLocalMatrix * tr.localToWorldMatrix * skin.sharedMesh.bindposes[id]).ToArray();
            mat.SetMatrixArray("_BonesArray", bones);
        }

        void UpdateBoneInfo()
        {
            if (!manifest)
                return;


            UpdateBoneArray(mat);
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

            if (needUpdateBlock)
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

        void UpdateAnimTime(int index, int startNameHash,int endNameHash)
        {
            if (index >= manifest.animInfos.Count)
                return;

            curClipInfo = manifest.animInfos[index];

            mat.SetFloat(startNameHash, curClipInfo.startFrame, block);
            mat.SetFloat(endNameHash, curClipInfo.endFrame, block);
            needUpdateBlock = true;
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

            UpdateAnimTime(index, ID_START_FRAME, ID_END_FRAME);
            UpdateAnimTime(index, ID_NEXT_START_FRAME, ID_NEXT_END_FRAME);
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

            UpdateAnimTime(index, ID_START_FRAME, ID_END_FRAME);
            UpdateAnimTime(nextIndex, ID_NEXT_START_FRAME, ID_NEXT_END_FRAME);

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