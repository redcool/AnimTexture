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
        public float3x4[] bindposes;
        public Matrix4x4[] originalBindPoses;
        public BoneInfoPerVertex[] boneInfoPerVertices;

        public BoneWeight1[] boneWeight1Array;

        public string[] bonePaths;
        public Transform[] boneTrs;

        public float3x4[] bones;

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
                bindposesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, bindposes.Length, Marshal.SizeOf<float3x4>());

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

        public GraphicsBuffer GetBoneWeight1Buffer()
        {
            if (!boneWeightsBuffer.IsValidSafe())
            {
                boneWeightsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, boneWeight1Array.Length, Marshal.SizeOf<BoneWeight1>());
                boneWeightsBuffer.SetData(boneWeight1Array);
            }
            return boneWeightsBuffer;
        }
        public GraphicsBuffer GetBonesBuffer(Transform tr)
        {
            if (boneTrs == null)
            {
                GetBoneTrs(tr);
            }
            if(bones == null || bones.Length != boneTrs.Length)
            {
                bones = new float3x4[boneTrs.Length];
            }

            for (int i = 0; i < boneTrs.Length; i++)
            {
                var bone = boneTrs[i];
                //var mat = bone.localToWorldMatrix * originalBindPoses[i];
                var mat = bone.localToWorldMatrix;
                bones[i] = mat.ToFloat3x4();
            }
            if (!bonesBuffer.IsValidSafe())
            {
                bonesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, bones.Length, Marshal.SizeOf<float3x4>());
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