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

    public class AnimTextureManifest : ScriptableObject
    {
        [Serializable]
        public struct BoneInfoPerVertex
        {
            public float bonesCountPerVertex;
            public float bonesStartIndexPerVertex;
        }
        /// <summary>
        /// Animation info
        /// </summary>
        public List<AnimTextureClipInfo> animInfos = new List<AnimTextureClipInfo>();
        /// <summary>
        /// BoneMesh texture atlas
        /// </summary>
        public Texture2D atlas;

        //----------- bake bone 
       

        public GraphicsBuffer bindposesBuffer;
        public GraphicsBuffer bonesBuffer;
        public GraphicsBuffer boneInfoBuffer;
        public GraphicsBuffer boneWeightsBuffer;

        // original data
        public Matrix4x4[] bindposes;
        public Matrix4x4[] bones;
        public BoneInfoPerVertex[] boneInfoPerVertices;
        public BoneWeight1[] boneWeightArray;

        public Transform[] boneTrs;
        public string[] bonePaths;

        public Transform[] GetBoneTrs(Transform rootBone)
        {
            if (boneTrs == null)
            {
                boneTrs = new Transform[bonePaths.Length];
                for (int i = 0; i < bonePaths.Length; i++)
                {
                    var path = bonePaths[i];
                    var bone = rootBone.Find(path);
                    boneTrs[i] = bone;
                }
            }
            return boneTrs;
        }

        public GraphicsBuffer GetBindPosesBuffer()
        {
            if (!bindposesBuffer.IsValidSafe())
            {
                bindposesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, bindposes.Length, AnimTextureUtils.BoneMatrixSize);
            }
            bindposesBuffer.SetData(bindposes);
            return bindposesBuffer;
        }

        public GraphicsBuffer GetBoneInfoPerVertexBuffer()
        {
            if (!boneInfoBuffer.IsValidSafe())
            {
                boneInfoBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, boneInfoPerVertices.Length, Marshal.SizeOf<BoneInfoPerVertex>());
            }

            boneInfoBuffer.SetData(boneInfoPerVertices);
            return boneInfoBuffer;
        }

        public GraphicsBuffer GetBoneWeightsBuffer()
        {
            if (!boneWeightsBuffer.IsValidSafe())
            {
                boneWeightsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, boneWeightArray.Length, Marshal.SizeOf<BoneWeight1>());
                boneWeightsBuffer.SetData(boneWeightArray);
            }
            return boneWeightsBuffer;
        }

        public GraphicsBuffer GetBonesBuffer()
        {
            if (!bonesBuffer.IsValidSafe())
            {
                bonesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, bones.Length, AnimTextureUtils.BoneMatrixSize);
            }
            bonesBuffer.SetData(bones);
            return bonesBuffer;
        }
    }

    [System.Serializable]
    public class AnimTextureClipInfo
    {
        public string clipName;
        public int clipNameHash;
        public int startFrame;
        public int endFrame;
        public bool isLoop;
        public float length;

        public AnimTextureClipInfo(string clipName,int startFrame,int endFrame)
        {
            this.clipName = clipName;
            this.startFrame = startFrame;
            this.endFrame = endFrame;

            if(!string.IsNullOrEmpty(clipName))
                clipNameHash = Animator.StringToHash(clipName);
        }
    }
}