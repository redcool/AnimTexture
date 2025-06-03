using PowerUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace AnimTexture
{
    public static class SkinnedTools
    {

        static Dictionary<Mesh, BoneInfoPerVertex[]> meshBoneInfoPerVertexDict = new();

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

        public static void CalcSendBonesInfo(Transform rootTr, Material mat, Mesh mesh, Transform[] boneTrs, Matrix4x4[] meshBindposes,
            ref GraphicsBuffer boneWeightPerVertexBuffer,ref GraphicsBuffer boneInfoPerVertexBuffer, ref GraphicsBuffer bonesBuffer
            , ComputeShader calcBondMatrixCS = null, GraphicsBuffer localToWorldBuffer = null, GraphicsBuffer bindposesBuffer = null, bool isSendArray = false)
        {
            var bonesPerVertex = mesh.GetBonesPerVertex();
            var boneStartPerVertex = mesh.GetBoneStartPerVertex();
            var boneWeightArray = mesh.GetAllBoneWeights();

            //// ========================= send buffer
            GraphicsBufferTools.TryCreateBuffer(ref boneWeightPerVertexBuffer, GraphicsBuffer.Target.Structured, boneWeightArray.Length, Marshal.SizeOf<BoneWeight1>());
            boneWeightPerVertexBuffer.SetData(boneWeightArray);

            var boneInfoPerVertex = DictionaryTools.Get(meshBoneInfoPerVertexDict, mesh, (mesh) =>
            {
                var boneInfoPerVertex = bonesPerVertex
                    .Zip(boneStartPerVertex, (count, start) => new BoneInfoPerVertex { bonesCountPerVertex = count, bonesStartIndexPerVertex = start })
                    .ToArray();
                return boneInfoPerVertex;
            }
            );

            GraphicsBufferTools.TryCreateBuffer(ref boneInfoPerVertexBuffer, GraphicsBuffer.Target.Structured, boneWeightArray.Length, Marshal.SizeOf<BoneInfoPerVertex>());
            boneInfoPerVertexBuffer.SetData(boneInfoPerVertex);

            GraphicsBufferTools.TryCreateBuffer(ref bonesBuffer, GraphicsBuffer.Target.Structured, boneTrs.Length, Marshal.SizeOf<Matrix4x4>());

            // calc matrix in cs
            if (calcBondMatrixCS.CanExecute())
            {
                CalcSkinnedBonesMatrix(calcBondMatrixCS, bonesBuffer, rootTr, boneTrs, meshBindposes, localToWorldBuffer, bindposesBuffer);
            }
            else
            {
                var bones = boneTrs.Select((boneTr, id) => rootTr.worldToLocalMatrix * boneTr.localToWorldMatrix * meshBindposes[id]).ToArray();
                bonesBuffer.SetData(bones);
                mat.SetFloat("_BoneCount", bones.Length);
                mat.SetMatrixArray("_BonesArray", bones);
            }

            mat.SetBuffer("_BoneWeightBuffer", boneWeightPerVertexBuffer);
            mat.SetBuffer("_BoneInfoPerVertexBuffer", boneInfoPerVertexBuffer);
            mat.SetBuffer("_Bones", bonesBuffer);

            //// ========================= send array
            if (isSendArray)
            {
                mat.SetFloatArray("_BoneCountArray", bonesPerVertex.Select(Convert.ToSingle).ToArray());
                mat.SetFloatArray("_BoneStartArray", boneStartPerVertex.Select(Convert.ToSingle).ToArray());
                mat.SetFloatArray("_BoneWeightArray", boneWeightArray.Select(bw => bw.weight).ToArray());
                mat.SetFloatArray("_BoneWeightIndexArray", boneWeightArray.Select(bw => (float)bw.boneIndex).ToArray());
            }
        }

        public static void CalcSkinnedBonesMatrix(ComputeShader calcBoneMatrixCS, GraphicsBuffer resultBonesBuffer, Transform rootTr, Transform[] boneTrs, Matrix4x4[] meshBindposes
            , GraphicsBuffer localToWorldBuffer, GraphicsBuffer bindposesBuffer)
        {
            GraphicsBufferTools.TryCreateBuffer(ref localToWorldBuffer, GraphicsBuffer.Target.Structured, boneTrs.Length, Marshal.SizeOf<Matrix4x4>());
            GraphicsBufferTools.TryCreateBuffer(ref bindposesBuffer, GraphicsBuffer.Target.Structured, boneTrs.Length, Marshal.SizeOf<Matrix4x4>());

            localToWorldBuffer.SetData(boneTrs.Select(tr => tr.localToWorldMatrix).ToArray());
            bindposesBuffer.SetData(meshBindposes);

            var kernel = calcBoneMatrixCS.FindKernel("CalcBoneMatrix");
            calcBoneMatrixCS.SetBuffer(kernel, "_Bones", resultBonesBuffer);
            calcBoneMatrixCS.SetBuffer(kernel, "_LocalToWorldBuffer", localToWorldBuffer);
            calcBoneMatrixCS.SetBuffer(kernel, "_BindposesBuffer", bindposesBuffer);
            calcBoneMatrixCS.SetMatrix("_RootWorldToLocal", rootTr.worldToLocalMatrix);
            calcBoneMatrixCS.DispatchKernel(kernel, 64, 64, 1);

            // test buffer read back 
            /*
            var bones1 = new Matrix4x4[boneTrs.Length];
            resultBonesBuffer.GetData(bones1);
            var bones = boneTrs.Select((tr, id) => tr.localToWorldMatrix * meshBindposes[id]).ToArray();

            bones1.ForEach((bone, id) =>
            {
                Debug.Log($"{bone} , {bones[id]} ,is equals: {bone == bones[id]}");
            });
            */
        }

        public static void CalcSkinnedMesh(ComputeShader skinnedMeshCS,GraphicsBuffer meshBuffer, GraphicsBuffer resultBonesBuffer, GraphicsBuffer boneWeightPerVertexBuffer, GraphicsBuffer boneInfoPerVertexBuffer)
        {
            var kernel = skinnedMeshCS.FindKernel("CalcSkinnedMesh");
            skinnedMeshCS.SetBuffer(kernel, "_MeshBuffer", meshBuffer);
            skinnedMeshCS.SetBuffer(kernel, "_Bones", resultBonesBuffer);
            skinnedMeshCS.SetBuffer(kernel, "_BoneInfoPerVertexBuffer",boneInfoPerVertexBuffer);
            skinnedMeshCS.SetBuffer(kernel, "_BoneWeightBuffer", boneWeightPerVertexBuffer);

            skinnedMeshCS.DispatchKernel(kernel, meshBuffer.count, 1, 1);
        }
    }
}