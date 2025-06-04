using PowerUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace AnimTexture
{
    public class GPUSkinnedMeshControl : MonoBehaviour
    {
        public SkinnedMeshRenderer skinned;
        public Material gpuSkinnedMat;
        public ComputeShader calcBoneMatrixCS;
        public bool isUpdateEditorMesh;

        [Header("Cur State")]
        public bool isInited;
        public MeshRenderer mr;
        public MeshFilter mf;
        public Mesh originalSharedMesh;

        public Transform[] boneTrs;
        GraphicsBuffer boneWeightPerVertexBuffer, boneInfoPerVertexBuffer, bonesBuffer;
        GraphicsBuffer localToWorldBuffer,bindPosesBuffer;
        GraphicsBuffer meshBuffer;

        public void OnEnable()
        {
            if (!skinned)
                skinned = GetComponentInChildren<SkinnedMeshRenderer>();
            
            isInited = skinned && gpuSkinnedMat;

            if (!isInited)
            {
                enabled = false;
                return;
            }
            // save 
            boneTrs = skinned.bones;
            //update skinned
            skinned.transform.localScale = Vector3.one;
            skinned.enabled = false;

            // update meshRenderer
            mr = gameObject.GetOrAddComponent<MeshRenderer>();
            mr.sharedMaterial = gpuSkinnedMat;
            mr.bounds = skinned.bounds;

            mf = gameObject.GetOrAddComponent<MeshFilter>();
            originalSharedMesh = mf.sharedMesh = skinned.sharedMesh;


            // update anim
            var anim = GetComponent<Animator>();
            //anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            // can remove SkinnedMeshRenderer
        }

        Vector3[] skinnedVertices;

        public void Update()
        {
            SkinnedTools.CalcSendBonesInfo(transform, gpuSkinnedMat, originalSharedMesh, boneTrs, originalSharedMesh.bindposes,
                ref boneWeightPerVertexBuffer, ref boneInfoPerVertexBuffer, ref bonesBuffer, calcBoneMatrixCS, localToWorldBuffer, bindPosesBuffer, false);
#if UNITY_EDITOR
            if (isUpdateEditorMesh)
            {
                var bones = new Matrix4x4[bonesBuffer.count];
                bonesBuffer.GetData(bones);

                //var bones1 = boneTrs.Select((tr, id) => transform.worldToLocalMatrix * tr.localToWorldMatrix * originalSharedMesh.bindposes[id]);
                //var results = bones1.Select((tr, id) => $"{tr} == {bones[id]} ? {tr == bones[id]}");
                //Debug.Log(string.Join("\n",results));

                //mf.mesh.vertices = SkinnedTools.GetBonedVertices_CPU(transform, skinned.bones, originalSharedMesh);
                //mf.mesh.vertices = SkinnedTools.GetBonedVertices_CPU(bones, originalSharedMesh);

                // calc gpu skinned mesh
                GraphicsBufferTools.TryCreateBuffer(ref meshBuffer, GraphicsBuffer.Target.Structured, originalSharedMesh.vertexCount, Marshal.SizeOf<Vector3>());
                meshBuffer.SetData(originalSharedMesh.vertices);

                SkinnedTools.CalcSkinnedMesh(calcBoneMatrixCS, meshBuffer, bonesBuffer, boneWeightPerVertexBuffer, boneInfoPerVertexBuffer);


                if (skinnedVertices == null)
                    skinnedVertices = new Vector3[originalSharedMesh.vertexCount];

                meshBuffer.GetData(skinnedVertices);
                //mf.mesh.SetVertices(skinnedVertices, 0, skinnedVertices.Length);
                mf.mesh.vertices = skinnedVertices;
                mf.mesh.RecalculateBounds();
            }
            else
            {
                mf.mesh = originalSharedMesh;
            }
#endif
        }


    }
}
