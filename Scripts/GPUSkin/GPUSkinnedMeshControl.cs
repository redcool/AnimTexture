using PowerUtilities;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace AnimTexture
{
    /// <summary>
    /// Support SRPBatch
    /// use MeshRenderer MeshFilter render SkinnedMeshRenderer
    /// </summary>
    public class GPUSkinnedMeshControl : MonoBehaviour
    {
        public SkinnedMeshRenderer skinned;

        [EditorNotNull]
        [LoadAsset("AnimTexture_GpuSkinned.mat")]
        [Tooltip("a material with keyword : _GPU_SKINNED_ON")]
        public Material gpuSkinnedMat;

        [EditorNotNull]
        [LoadAsset("CalcBoneMatrix.compute")]
        [Tooltip("use a compute shader calculate bones transform")]
        public ComputeShader calcBoneMatrixCS;
        
        [Tooltip("readback sbuffer update current editor mesh")]
        public bool isUpdateEditorMesh;

        [Tooltip("remove SkinnedMeshRenderer when OnEnable done")]
        public bool isRemoveSkinnedMeshRenderer;

        [Header("Cur State")]
        public MeshRenderer mr;
        public MeshFilter mf;
        public Mesh originalSharedMesh;

        public Transform[] boneTrs;

        GraphicsBuffer boneWeightPerVertexBuffer, boneInfoPerVertexBuffer, bonesBuffer;
        GraphicsBuffer localToWorldBuffer,bindPosesBuffer;

        [EditorButton(onClickCall = nameof(OnEnable))]
        public bool isCallEnable;

        [EditorButton(onClickCall = nameof(Update))]
        public bool isCallUpdate;

        //--------- editor only -------------
        GraphicsBuffer meshBuffer;
        Vector3[] skinnedVertices;

        public void OnEnable()
        {

            if (!skinned)
                skinned = GetComponentInChildren<SkinnedMeshRenderer>();

            var isValid = skinned && gpuSkinnedMat;

            if (!isValid)
            {
                return;
            }
            // save bones
            boneTrs = skinned.bones;
            //update skinned
            skinned.transform.localScale = Vector3.one;
            skinned.enabled = false;

            // update meshRenderer
            mr = gameObject.GetOrAddComponent<MeshRenderer>();
            mr.sharedMaterial = gpuSkinnedMat;
            mr.bounds = skinned.bounds;
            mr.localBounds = skinned.bounds;
            //var center = Vector3.Scale(skinned.bounds.center , skinned.rootBone.localScale);
            //var size = Vector3.Scale(skinned.bounds.size , skinned.rootBone.localScale);
            //mr.bounds = new Bounds(center, size);

            // mesh filter
            mf = gameObject.GetOrAddComponent<MeshFilter>();
            originalSharedMesh = mf.sharedMesh = skinned.sharedMesh;

            // update anim
            var anim = GetComponent<Animator>();
            //anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            // can remove SkinnedMeshRenderer

            if (isRemoveSkinnedMeshRenderer)
                skinned.Destroy();
        }


        public void Update()
        {
            if(boneTrs == null || boneTrs.Length == 0)
                return;

            var skinnedMat = Application.isPlaying ? mr.material : mr.sharedMaterial;
            SkinnedTools.ApplySkinnedTransform(transform,
                skinnedMat, 
                originalSharedMesh,
                boneTrs,
                originalSharedMesh.bindposes,
                ref boneWeightPerVertexBuffer, ref boneInfoPerVertexBuffer, ref bonesBuffer,
                calcBoneMatrixCS, ref localToWorldBuffer, ref bindPosesBuffer);

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
