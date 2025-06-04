#if !defined(CS_LIB_HLSL)
#define CS_LIB_HLSL

/**
    https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/sm5-attributes-numthreads

*/

/*Dispatched groups*/
float3 _DispatchGroupSize;
uint GetDispatchThreadIndex(uint3 groupId/*SV_GroupID*/,uint groupThreadIndex/*SV_GroupIndex*/,uint3 groupThreadSize){

    uint3 groupSize = (uint3)_DispatchGroupSize;
    //SV_GroupId(2,1,0) = 0*5*3+1*5+2 = 7
    uint groupIndex = groupId.z * groupSize.x * groupSize.y + groupId.y * groupSize.x + groupId.x;;
    return (groupIndex-1) * (groupThreadSize.x*groupThreadSize.y*groupThreadSize.z) + groupThreadIndex;
}

#endif //CS_LIB_HLSL