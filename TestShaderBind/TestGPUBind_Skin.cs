using PowerUtilities;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
/// <summary>
/// Test vs bind quad
/// </summary>
public class TestGPUBind_Skin : MonoBehaviour
{
    public SkinnedMeshRenderer skinned;
    public Material mat;

    [EditorButton(onClickCall = "Start")]
    public bool isStart;

    [EditorButton(onClickCall = "Send")]
    public bool isSend;


    [EditorButton(onClickCall = "BakeBoneTex")]
    public bool isBakeBoneTex;
    public ComputeShader bakeBoneCS;

    public BoneWeight1[] boneWeights;
    public Matrix4x4[] bones;

    GraphicsBuffer bonesBuffer,boneInfoPerVertexBuffer,boneWeightBuffer;

    public byte[] bonesPerVertex;
    public int[] boneStartPerVertex;

    [Serializable]
    public struct BoneInfoPerVertex
    {
        public uint boneCount; // bones count per vertex,in BoneWeight1 array
        public int boneStart; // bone start per vertex ,in BoneWeight1 array
    }

    public BoneInfoPerVertex[] boneInfoPerVertex;


    public void Send()
    {
        bonesPerVertex = skinned.sharedMesh.GetBonesPerVertex().ToArray();
        boneStartPerVertex = skinned.sharedMesh.GetBoneStartPerVertex();

        boneWeights = skinned.sharedMesh.GetAllBoneWeights().ToArray();
        var bindPoses = skinned.sharedMesh.bindposes;
        bones = skinned.bones.Select((b, id) => (b.localToWorldMatrix * bindPoses[id])).ToArray();

        boneInfoPerVertex = bonesPerVertex.Zip(boneStartPerVertex, (c, id) => new BoneInfoPerVertex { boneCount = c, boneStart = id }).ToArray();

        // ========================= send buffer
        GraphicsBufferTools.TryCreateBuffer(ref bonesBuffer, GraphicsBuffer.Target.Structured, bones.Length, Marshal.SizeOf<Matrix4x4>());
        bonesBuffer.SetData(bones);
        GraphicsBufferTools.TryCreateBuffer(ref boneInfoPerVertexBuffer, GraphicsBuffer.Target.Structured, boneInfoPerVertex.Length, Marshal.SizeOf<BoneInfoPerVertex>());
        boneInfoPerVertexBuffer.SetData(boneInfoPerVertex);

        GraphicsBufferTools.TryCreateBuffer(ref boneWeightBuffer, GraphicsBuffer.Target.Structured, boneWeights.Length, Marshal.SizeOf<BoneWeight1>());
        boneWeightBuffer.SetData(boneWeights);

        mat.SetBuffer("_Bones", bonesBuffer);
        mat.SetBuffer("_BoneInfoPerVertex",boneInfoPerVertexBuffer);
        mat.SetBuffer("_BoneWeight",boneWeightBuffer);

        // ========================= send array
        //[matrix4x4]
        mat.SetMatrixArray("_BonesArray", bones);
        //[boneCountPerVertex] in {_BoneWeightArray,_BoneWeightIndexArray}
        mat.SetFloatArray("_BoneCountArray", bonesPerVertex.Select(Convert.ToSingle).ToArray());
        //[boneStartIndex]  in {_BoneWeightArray,_BoneWeightIndexArray}
        mat.SetFloatArray("_BoneStartArray", boneStartPerVertex.Select(Convert.ToSingle).ToArray());
        // [{weight,boneIndex}]
        mat.SetFloatArray("_BoneWeightArray", boneWeights.Select(bw=>bw.weight).ToArray());
        mat.SetFloatArray("_BoneWeightIndexArray", boneWeights.Select(bw=>(float)bw.boneIndex).ToArray());
    }
    
    void Start()
    {
        MeshRenderer renderer = gameObject.GetOrAddComponent<MeshRenderer>();
        renderer.material = mat;

        var filter = gameObject.GetOrAddComponent<MeshFilter>();
        filter.sharedMesh = skinned.sharedMesh;
    }
}