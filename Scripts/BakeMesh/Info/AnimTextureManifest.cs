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
        /// <summary>
        /// Animation info
        /// </summary>
        public List<AnimTextureClipInfo> animInfos = new ();
        /// <summary>
        /// BoneMesh texture atlas
        /// </summary>
        public Texture2D atlas;

        //----------- bake bone 
        [Header("buffer")]

         GraphicsBuffer boneInfoBuffer;
         GraphicsBuffer boneWeightsBuffer;

        // original data
        [ListItemDraw("count:,bonesCountPerVertex,id:,bonesStartIndexPerVertex","50,100,50,100")]
        public BoneInfoPerVertex[] boneInfoPerVertices;

        [ListItemDraw("weight:,m_Weight,boneIndex:,m_BoneIndex", "50,100,50,100")]
        public BoneWeight1[] boneWeightArray;

        public Transform[] boneTrs;
        public string[] bonePaths;

        public Matrix4x4[] bones;
        public Matrix4x4[] bindposes;

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

        public void SendBoneBuffer(Material mat)
        {
            GraphicsBufferTools.TryCreateBuffer(ref boneInfoBuffer, GraphicsBuffer.Target.Structured, boneInfoPerVertices.Length, Marshal.SizeOf<BoneInfoPerVertex>());
            boneInfoBuffer.SetData(boneInfoPerVertices);

            GraphicsBufferTools.TryCreateBuffer(ref boneWeightsBuffer, GraphicsBuffer.Target.Structured, boneWeightArray.Length, Marshal.SizeOf<BoneWeight1>());
            boneWeightsBuffer.SetData(boneWeightArray);

            mat.SetBuffer("_BoneInfoPerVertexBuffer", boneInfoBuffer);
            mat.SetBuffer("_BoneWeightBuffer", boneWeightsBuffer);
        }

    }
}