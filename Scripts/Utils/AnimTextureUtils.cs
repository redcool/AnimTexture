﻿namespace AnimTexture
{
    using PowerUtilities;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using UnityEngine;

    public class AnimTextureUtils
    {
        public static int BoneMatrixSize => Marshal.SizeOf<Matrix4x4>();

        public static Texture2D BakeMeshToTexture(SkinnedMeshRenderer skin, GameObject clipGo, AnimationClip clip)
        {
            var width = skin.sharedMesh.vertexCount;
            var frameCount = (int)(clip.length * clip.frameRate);
            var timePerFrame = clip.length / frameCount;
            var tex = new Texture2D(width, frameCount, TextureFormat.RGBAHalf, false, true);
            tex.name = clip.name;

            float time = 0;
            Mesh mesh = new Mesh();
            for (int y = 0; y < frameCount; y++)
            {
                clip.SampleAnimation(clipGo, time += timePerFrame);
                skin.BakeMesh(mesh);

                var colors = new Color[mesh.vertexCount];

                for (int x = 0; x < mesh.vertexCount; x++)
                {
                    var v = mesh.vertices[x];
                    colors[x] = new Vector4(v.x, v.y, v.z);
                }
                tex.SetPixels(0, y, width, 1, colors);
            }
            tex.Apply();
            return tex;
        }
        /// <summary>
        /// row : matrix
        /// col : frame
        /// {
        ///     frame 1,
        ///     frame 2,
        /// }
        /// </summary>
        /// <param name="skin"></param>
        /// <param name="clipGo"></param>
        /// <param name="clip"></param>
        /// <returns></returns>
        //public static Texture2D BakeBonesToTexture(SkinnedMeshRenderer skin,GameObject clipGo,AnimationClip clip)
        //{
        //    var bones = skin.bones;
        //    var bindposes = skin.sharedMesh.bindposes;
        //    var boneCount = bones.Length;

        //    var frameCount = (int)(clip.length * clip.frameRate);
        //    var timePerFrame = clip.length / frameCount;
        //    var width = boneCount * 3;

        //    var tex = new Texture2D(width, frameCount, TextureFormat.RGBA32, false, true);
        //    tex.name = clip.name;

        //    float time = 0;
        //    for (int y = 0; y < frameCount; y++)
        //    {
        //        clip.SampleAnimation(clipGo, time += timePerFrame);

        //        var colors = new Color[bones.Length * 3];

        //        for (int x = 0; x < boneCount; x++)
        //        {
        //            var boneTr = bones[x];
        //            var boneMat = skin.transform.worldToLocalMatrix * boneTr.localToWorldMatrix * bindposes[x];
        //            for(int m = 0; m < 3; m++)
        //            {
        //                // colorId = x*3+m
        //                colors[x * 3 + m] = boneMat.GetRow(m);
        //            }
        //        }
        //        tex.SetPixels(0, y, width, 1, colors);
        //    }
        //    tex.Apply();
        //    return tex;
        //}
        static GraphicsBuffer bindposesBuffer, bonesBuffer;

        public static void BakeBonesToRT(SkinnedMeshRenderer skin, GameObject clipGo, AnimationClip clip,ComputeShader cs,int yStart,RenderTexture resultTex)
        {
            // create reusage
            GraphicsBufferTools.TryCreateBuffer(ref bindposesBuffer, GraphicsBuffer.Target.Structured, skin.bones.Length, BoneMatrixSize);
            GraphicsBufferTools.TryCreateBuffer(ref bonesBuffer,GraphicsBuffer.Target.Structured, skin.bones.Length, BoneMatrixSize);

            // 
            var bindposes = skin.sharedMesh.bindposes;
            bindposesBuffer.SetData(bindposes);

            // get cs
            var bakeBoneMatrixKernel = cs.FindKernel("BakeBoneMatrix");
            cs.SetTexture(bakeBoneMatrixKernel,"_ResultTex", resultTex);


            var frameCount = (int)(clip.length * clip.frameRate); // pixels height, a row per clip frame
            var timePerFrame = clip.length / frameCount;
            var time = 0f;

            var sb = new StringBuilder();

            for (int rowId = 0; rowId < frameCount; rowId++)
            {
                clip.SampleAnimation(clipGo, time += timePerFrame);

                var boneTrs = skin.bones;
                var boneCount = boneTrs.Length;
                var bones = boneTrs.Select(tr => tr.localToWorldMatrix).ToArray();

                var width = boneCount * 3; // a bone 3 pixel

                sb.AppendLine(rowId.ToString());
                sb.AppendLine(bones[0].ToString());
                sb.AppendLine();
                // update data
                bonesBuffer.SetData(bones);

                cs.SetInt("_YStart", yStart + rowId);
                cs.SetBuffer(bakeBoneMatrixKernel,"_BindposesBuffer", bindposesBuffer);
                cs.SetBuffer(bakeBoneMatrixKernel, "_BonesBuffer", bonesBuffer);
                cs.SetMatrix("_RootWorldToLocal",skin.transform.worldToLocalMatrix);

                // dispatch a row 
                cs.DispatchKernel(bakeBoneMatrixKernel, width, 1, 1);
            }

            Debug.Log(sb.ToString());

            
        }
    }
}