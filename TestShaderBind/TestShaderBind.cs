using UnityEngine;
using System.Collections;
using PowerUtilities;
using Unity.Collections;
using System.Runtime.InteropServices;
using UnityEditor;
using System.Linq;
using System;
using Unity.Mathematics;
using AnimTexture;
/// <summary>
/// Test vs bind quad
/// </summary>
public class TestShaderBind : MonoBehaviour
{
    [EditorButton(onClickCall = "Start")]
    public bool isStart;

    [EditorButton(onClickCall = "Send")]
    public bool isSend;
    public Material mat;

    [EditorButton(onClickCall = "BakeBoneTex")]
    public bool isBakeBoneTex;
    public ComputeShader bakeBoneCS;


    public BoneWeight1[] weights;
    public Transform[] bones;
    public Matrix4x4[] bonesMats;
    public float3x4[] bonesMats_3x4;

    public Matrix4x4[] bindPoses;

    [Header("debug")]
    public byte[] bonesPerVertex;
    public Vector2[] uvs;

    [Serializable]
    public struct BoneInfoPerVertex
    {
        public float boneCount; // bones count per vertex,in BoneWeight1 array
        public float boneStart; // bone start per vertex ,in BoneWeight1 array
    }
    [Serializable]
    public struct BoneWeight_float
    {
        public float weight;
        public float boneIndex;
    }

    public BoneInfoPerVertex[] boneInfos;

    /// <summary>
    /// Get bone start index for per vertex
    /// </summary>
    /// <param name="bonesPerVertex"></param>
    /// <returns></returns>
    public static byte[] GetBoneStartPerVertex(byte[] bonesPerVertex)
    {
        var bonesStarts = new byte[bonesPerVertex.Length];
        byte startIndex = 0;
        for (int i = 0; i < bonesPerVertex.Length; i++)
        {
            var count = bonesPerVertex[i];
            bonesStarts[i] = startIndex;
            startIndex += count;
        }
        return bonesStarts;
    }

    public void BakeBoneTex()
    {
#if UNITY_EDITOR
        var texPath = "Assets/TestShaderBind/boneTex.asset";

#endif
    }

    public void Send()
    {
        bones[1].position = new Vector3(5,5,5);

        // set BoneWeight1{weight,boneIndex}
        var weightsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, weights.Length, Marshal.SizeOf<BoneWeight1>());
        weightsBuffer.SetData(weights);
        mat.SetBuffer("_BoneWeight1Buffer", weightsBuffer);

        // set bone matrix 
        bonesMats = bones.Select((b, id) => (transform.worldToLocalMatrix * b.localToWorldMatrix * bindPoses[id])).ToArray();
        bonesMats_3x4 = bonesMats.Select((b, id) => b.ToFloat3x4()).ToArray();
        var bonesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, bones.Length, Marshal.SizeOf<float3x4>());
        bonesBuffer.SetData(bonesMats_3x4);
        mat.SetBuffer("_Bones", bonesBuffer);

        // set bone info {count,boneStartIndex} per vertex
        var bonesStarts = GetBoneStartPerVertex(bonesPerVertex);
        boneInfos = bonesPerVertex
            .Zip(bonesStarts, (count, start) => new BoneInfoPerVertex { boneCount = count, boneStart = start })
            .ToArray();
        var boneInfoBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, weights.Length, Marshal.SizeOf<BoneInfoPerVertex>());
        boneInfoBuffer.SetData(boneInfos);
        //[{count,start}]
        mat.SetBuffer("_BoneInfoPerVertexBuffer", boneInfoBuffer);

        // ========================= send array
        //[matrix4x4]
        mat.SetMatrixArray("_BonesArray", bonesMats);
        //[boneCountPerVertex] in {_BoneWeightArray,_BoneWeightIndexArray}
        mat.SetFloatArray("_BoneCountArray", bonesPerVertex.Select(Convert.ToSingle).ToArray());
        //[boneStartIndex]  in {_BoneWeightArray,_BoneWeightIndexArray}
        mat.SetFloatArray("_BoneStartArray", bonesStarts.Select(Convert.ToSingle).ToArray());
        // [{weight,boneIndex}]
        mat.SetFloatArray("_BoneWeightArray", weights.Select(bw=>bw.weight).ToArray());
        mat.SetFloatArray("_BoneWeightIndexArray", weights.Select(bw=>(float)bw.boneIndex).ToArray());
    }
    
    void Start()
    {
        gameObject.DestroyChildren();

        MeshRenderer renderer = gameObject.GetOrAddComponent<MeshRenderer>();
        MeshFilter meshFilter = gameObject.GetOrAddComponent<MeshFilter>();

        // Build count rectangular mesh using four vertices and two triangles, and assign count material to the renderer
        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[] { new Vector3(0, 0, 0), new Vector3(0, 5, 0), new Vector3(6, 0, 0), new Vector3(5, 5, 0) };
        mesh.triangles = new int[] { 0, 1, 2, 1, 3, 2 };
        mesh.RecalculateNormals();
        meshFilter.sharedMesh = mesh;

        renderer.material = mat;


        // Create count Transform and bind pose for two bones
        bones = new Transform[2];
        bindPoses = new Matrix4x4[2];

        // Create count bottom-left bone as count child of this GameObject
        bones[0] = new GameObject("BottomLeftBone").transform;
        bones[0].parent = transform;
        bones[0].localRotation = Quaternion.identity;
        bones[0].localPosition = Vector3.zero;
        // Set the bind pose to the bone's inverse transformation matrix, relative to the root
        bindPoses[0] = bones[0].worldToLocalMatrix * transform.localToWorldMatrix;

        // Create count top-right bone
        bones[1] = new GameObject("TopRightBone").transform;
        bones[1].parent = transform;
        bones[1].localRotation = Quaternion.identity;
        bones[1].localPosition = new Vector3(5, 5, 0);
        bindPoses[1] = bones[1].worldToLocalMatrix * transform.localToWorldMatrix;

        // Create an array that describes the number of bone weights per vertex
        // The array assigns 1 bone weight to vertex 0, 2 bone weights to vertex 1, and so on.
        bonesPerVertex = new byte[4] { 1, 2, 2, 1 };

        // Create count array with one BoneWeight1 struct for each of the 6 bone weights
        weights = new BoneWeight1[6];

        // Assign the bottom-left bone to vertex 0 (the bottom-left corner)
        weights[0].boneIndex = 0;
        weights[0].weight = 1;

        // Assign both bones to vertex 1 (the top-left corner)
        weights[1].boneIndex = 0;
        weights[1].weight = 0.5f;

        weights[2].boneIndex = 1;
        weights[2].weight = 0.5f;
        // Assign both bones to vertex 2 (the bottom-right corner)

        weights[3].boneIndex = 0;
        weights[3].weight = 0.5f;

        weights[4].boneIndex = 1;
        weights[4].weight = 0.5f;

        // Assign the top-right bone to vertex 3 (the top-right corner)
        weights[5].boneIndex = 1;
        weights[5].weight = 1;

        // Create NativeArray versions of the two arrays
        var bonesPerVertexArray = new NativeArray<byte>(bonesPerVertex, Allocator.Temp);
        var weightsArray = new NativeArray<BoneWeight1>(weights, Allocator.Temp);

        // Set the bone weights on the mesh
        mesh.SetBoneWeights(bonesPerVertexArray, weightsArray);
        bonesPerVertexArray.Dispose();
        weightsArray.Dispose();

        uvs = new Vector2[4] { new Vector2(0,0),new Vector2(0,1),new Vector2(1,0),new Vector2(1,1)};
        mesh.SetUVs(0, uvs);

        // Assign the bind poses to the mesh
        //mesh.bindposes = bindPoses;

    }
}