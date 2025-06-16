using PowerUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

namespace AnimTexture
{
    public static class SkinnedTools
    {

        static Dictionary<Mesh, BoneInfoPerVertex[]> meshBoneInfoPerVertexDict = new();


        public static Vector3[] GetBonedVertices_CPU(Matrix4x4[] bones, Mesh originalSharedMesh)
        {
            var bonesPerVertex = originalSharedMesh.GetBonesPerVertex();
            var boneStartPerVertex = originalSharedMesh.GetBoneStartPerVertex();
            var boneWeights = originalSharedMesh.GetAllBoneWeights();
            var vertices = originalSharedMesh.vertices;

            for (int i = 0; i < bonesPerVertex.Length; i++)
            {
                var boneCount = bonesPerVertex[i];
                var boneStart = boneStartPerVertex[i];

                var bonePos = new Vector4();
                var pos = new float4(vertices[i], 1);

                for (int j = 0; j < boneCount; j++)
                {
                    var bw = boneWeights[boneStart + j];
                    var mat = bones[bw.boneIndex];

                    bonePos += mat * pos * bw.weight;
                }
                vertices[i] = bonePos;
            }
            return vertices;
        }

        public static Vector3[] GetBonedVertices_CPU(Transform rootTr, Transform[] boneTrs, Mesh originalSharedMesh)
        {
            Matrix4x4[] bones = boneTrs.Select((tr, id) => rootTr.worldToLocalMatrix * tr.localToWorldMatrix * originalSharedMesh.bindposes[id]).ToArray();
            return GetBonedVertices_CPU(bones, originalSharedMesh);
        }

        /// <summary>
        /// For Test
        /// get bone info
        /// </summary>
        /// <param name="mat"></param>
        /// <param name="mesh"></param>
        /// <param name="boneTrs"></param>
        /// <param name="meshBindposes"></param>
        /// <param name="boneWeightBuffer"></param>
        /// <param name="boneInfoPerVertexBuffer"></param>
        /// <param name="bonesBuffer"></param>
        /// <param name="isSendArray"></param>
        public static void TestSendBonesInfo(Material mat, Mesh mesh, Transform[] boneTrs, Matrix4x4[] meshBindposes,
            GraphicsBuffer boneWeightBuffer, GraphicsBuffer boneInfoPerVertexBuffer, GraphicsBuffer bonesBuffer, bool isSendArray = false)
        {
            var bonesPerVertex = mesh.GetBonesPerVertex();
            var boneStartPerVertex = mesh.GetBoneStartPerVertex();
            var boneWeightArray = mesh.GetAllBoneWeights();

            //// ========================= send buffer
            GraphicsBufferTools.TryCreateBuffer(ref boneWeightBuffer, GraphicsBuffer.Target.Structured, boneWeightArray.Length, Marshal.SizeOf<BoneWeight1>());
            boneWeightBuffer.SetData(boneWeightArray);

            var boneInfoPerVertex = bonesPerVertex
                .Zip(boneStartPerVertex, (count, start) => new BoneInfoPerVertex { bonesCountPerVertex = count, bonesStartIndexPerVertex = start })
                .ToArray();

            GraphicsBufferTools.TryCreateBuffer(ref boneInfoPerVertexBuffer, GraphicsBuffer.Target.Structured, boneWeightArray.Length, Marshal.SizeOf<BoneInfoPerVertex>());
            boneInfoPerVertexBuffer.SetData(boneInfoPerVertex);

            // calc matrix in cs
            var bones = boneTrs.Select((tr, id) => tr.localToWorldMatrix * meshBindposes[id]).ToArray();

            GraphicsBufferTools.TryCreateBuffer(ref bonesBuffer, GraphicsBuffer.Target.Structured, bones.Length, Marshal.SizeOf<Matrix4x4>());
            bonesBuffer.SetData(bones);

            mat.SetBuffer("_BoneWeightBuffer", boneWeightBuffer);
            mat.SetBuffer("_BoneInfoPerVertexBuffer", boneInfoPerVertexBuffer);
            mat.SetBuffer("_Bones", bonesBuffer);
            mat.SetFloat("_BoneCount", bones.Length);

            if (isSendArray)
            {
                //// ========================= send array
                mat.SetFloatArray("_BoneCountArray", bonesPerVertex.Select(Convert.ToSingle).ToArray());
                mat.SetFloatArray("_BoneStartArray", boneStartPerVertex.Select(Convert.ToSingle).ToArray());
                mat.SetFloatArray("_BoneWeightArray", boneWeightArray.Select(bw => bw.weight).ToArray());
                mat.SetFloatArray("_BoneWeightIndexArray", boneWeightArray.Select(bw => (float)bw.boneIndex).ToArray());

                //var bones = skin.bones.Select((tr, id) => skin.transform.worldToLocalMatrix * tr.localToWorldMatrix * skin.sharedMesh.bindposes[id]).ToArray();
                mat.SetMatrixArray("_BonesArray", bones);
            }
        }
        /// <summary>
        /// Calc bone matrix (cs or cpu) then send material
        /// 
        /// </summary>
        /// <param name="rootTr"></param>
        /// <param name="skinnedMat"></param>
        /// <param name="bonedMesh"></param>
        /// <param name="boneTrs"></param>
        /// <param name="meshBindposes"></param>
        /// <param name="boneWeightPerVertexBuffer"></param>
        /// <param name="boneInfoPerVertexBuffer"></param>
        /// <param name="bonesBuffer"></param>
        /// <param name="calcBoneMatrixCS"></param>
        /// <param name="localToWorldBuffer"></param>
        /// <param name="bindposesBuffer"></param>
        /// <param name="isSendWeightBuffer">use _BoneWeightBuffer</param>
        public static void ApplySkinnedTransform(Transform rootTr, Material skinnedMat, Mesh bonedMesh, Transform[] boneTrs, Matrix4x4[] meshBindposes
            , ref GraphicsBuffer boneWeightPerVertexBuffer, ref GraphicsBuffer boneInfoPerVertexBuffer, ref GraphicsBuffer bonesBuffer
            , ComputeShader calcBoneMatrixCS, ref GraphicsBuffer localToWorldBuffer, ref GraphicsBuffer bindposesBuffer
            , bool isSendWeightBuffer = false)
        {
            GraphicsBufferTools.TryCreateBuffer(ref bonesBuffer, GraphicsBuffer.Target.Structured, boneTrs.Length, Marshal.SizeOf<Matrix4x4>());

            var bonesPerVertex = bonedMesh.GetBonesPerVertex();
            var boneStartPerVertex = bonedMesh.GetBoneStartPerVertex();
            var boneWeightArray = bonedMesh.GetAllBoneWeights();

            //// ========================= send weight buffer

            if (isSendWeightBuffer)
            {
                GraphicsBufferTools.TryCreateBuffer(ref boneWeightPerVertexBuffer, GraphicsBuffer.Target.Structured, boneWeightArray.Length, Marshal.SizeOf<BoneWeight1>());
                GraphicsBufferTools.TryCreateBuffer(ref boneInfoPerVertexBuffer, GraphicsBuffer.Target.Structured, boneWeightArray.Length, Marshal.SizeOf<BoneInfoPerVertex>());

                boneWeightPerVertexBuffer.SetData(boneWeightArray);

                var boneInfoPerVertex = DictionaryTools.Get(meshBoneInfoPerVertexDict, bonedMesh, (mesh) =>
                {
                    var boneInfoPerVertex = bonesPerVertex
                        .Zip(boneStartPerVertex, (count, start) => new BoneInfoPerVertex { bonesCountPerVertex = count, bonesStartIndexPerVertex = start })
                        .ToArray();
                    return boneInfoPerVertex;
                }
                );

                boneInfoPerVertexBuffer.SetData(boneInfoPerVertex);

                skinnedMat.SetBuffer("_BoneWeightBuffer", boneWeightPerVertexBuffer);
                skinnedMat.SetBuffer("_BoneInfoPerVertexBuffer", boneInfoPerVertexBuffer);
            }


            // calc matrix in cs
            if (calcBoneMatrixCS.CanExecute())
            {
                CalcSkinnedBonesMatrix(calcBoneMatrixCS,ref bonesBuffer, rootTr, boneTrs, meshBindposes,ref localToWorldBuffer,ref bindposesBuffer);
            }
            else
            {   // calc matrix 
                var bones = boneTrs.Select((boneTr, id) => rootTr.worldToLocalMatrix * boneTr.localToWorldMatrix * meshBindposes[id]).ToArray();
                bonesBuffer.SetData(bones);
            }

            skinnedMat.SetBuffer("_Bones", bonesBuffer);
        }

        public static void CalcSkinnedBonesMatrix(ComputeShader calcBoneMatrixCS, ref GraphicsBuffer bonesBuffer, Transform rootTr, Transform[] boneTrs, Matrix4x4[] meshBindposes
            , ref GraphicsBuffer localToWorldBuffer, ref GraphicsBuffer bindposesBuffer)
        {
            GraphicsBufferTools.TryCreateBuffer(ref localToWorldBuffer, GraphicsBuffer.Target.Structured, boneTrs.Length, Marshal.SizeOf<Matrix4x4>());
            GraphicsBufferTools.TryCreateBuffer(ref bindposesBuffer, GraphicsBuffer.Target.Structured, boneTrs.Length, Marshal.SizeOf<Matrix4x4>());

            localToWorldBuffer.SetData(boneTrs.Select(tr => tr.localToWorldMatrix).ToArray());
            bindposesBuffer.SetData(meshBindposes);

            var kernel = calcBoneMatrixCS.FindKernel("CalcBoneMatrix");
            calcBoneMatrixCS.SetBuffer(kernel, "_Bones", bonesBuffer);
            calcBoneMatrixCS.SetBuffer(kernel, "_LocalToWorldBuffer", localToWorldBuffer);
            calcBoneMatrixCS.SetBuffer(kernel, "_BindposesBuffer", bindposesBuffer);
            calcBoneMatrixCS.SetMatrix("_RootWorldToLocal", rootTr.worldToLocalMatrix);
            calcBoneMatrixCS.DispatchKernel(kernel, boneTrs.Length, 1, 1);
        }

        public static void CalcSkinnedMesh(ComputeShader skinnedMeshCS,GraphicsBuffer meshBuffer, GraphicsBuffer resultBonesBuffer, GraphicsBuffer boneWeightPerVertexBuffer, GraphicsBuffer boneInfoPerVertexBuffer)
        {
            var kernel = skinnedMeshCS.FindKernel("CalcSkinnedMesh");
            skinnedMeshCS.SetBuffer(kernel, "_MeshBuffer", meshBuffer);
            skinnedMeshCS.SetBuffer(kernel, "_Bones", resultBonesBuffer);
            skinnedMeshCS.SetBuffer(kernel, "_BoneInfoPerVertexBuffer",boneInfoPerVertexBuffer);
            skinnedMeshCS.SetBuffer(kernel, "_BoneWeightBuffer", boneWeightPerVertexBuffer);

            skinnedMeshCS.DispatchKernel(kernel, 64,64, 1);
        }
    }
}