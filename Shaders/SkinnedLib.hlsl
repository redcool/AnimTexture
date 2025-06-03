

#if !defined(SKINNED_LIB_HLSL)
#define SKINNED_LIB_HLSL

struct BoneInfoPerVertex{
    uint bonesCount;
    uint bonesStartIndex;
};
struct BoneWeight1{
    float weight;
    uint boneIndex;
};

StructuredBuffer<BoneInfoPerVertex> _BoneInfoPerVertexBuffer;
StructuredBuffer<BoneWeight1> _BoneWeightBuffer;
StructuredBuffer<float4x4> _Bones;

/**
    Get vertex skinned local position
    vid : vertexId
    pos : vertex local position
*/
float4 GetSkinnedPos(uint vid,float4 pos){
    float4 bonePos = (float4)0;

    BoneInfoPerVertex boneInfo = _BoneInfoPerVertexBuffer[vid];
    float bonesCount = boneInfo.bonesCount;
    float boneStart = boneInfo.bonesStartIndex;

    float4x4 boneMat = {1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1};

    UNITY_UNROLLX(4)
    for(int i=0;i<bonesCount;i++){
        BoneWeight1 bw = _BoneWeightBuffer[boneStart + i];
        float weight = bw.weight;
        uint boneIndex = bw.boneIndex;

        boneMat = _Bones[boneIndex];
        bonePos += mul(boneMat,pos) * weight;
    }

    return bonePos;
}

#endif //SKINNED_LIB_HLSL