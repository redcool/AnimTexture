Shader "Unlit/TestBindPose_Skin"
{
    Properties
    {
		[GroupToggle(,BUFFER_ON)]_BufferOn("_BufferOn",float) = 0
    }

HLSLINCLUDE



ENDHLSL
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
		cull off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma shader_feature BUFFER_ON

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			sampler2D _MainTex;

			struct BoneInfoPerVertex{
				uint bonesCount;
				uint bonesStartIndex;
			};
			struct BoneWeight1{
				float weight;
				uint boneIndex;
			};

			float4x4 _BonesArray[22];
			half _BoneCountArray[487];
			half _BoneStartArray[487];

			half _BoneWeightArray[790];
			half _BoneWeightIndexArray[790];

			// half _BoneWeight0[487];
			// half _BoneWeightIndex0[487];
			// half _BoneWeight1[487];
			// half _BoneWeightIndex1[487];

			StructuredBuffer<float4x4> _Bones;
			StructuredBuffer<BoneInfoPerVertex> _BoneInfoPerVertex;
			StructuredBuffer<BoneWeight1> _BoneWeight;

            struct appdata
            {
                half2 uv : TEXCOORD0;
				uint vertexId:SV_VertexID;
				float4 pos:POSITION;
            };

            struct v2f
            {
                half4 vertex : SV_POSITION;
                half2 uv : TEXCOORD0;
				float3 worldPos:TEXTURE1;
            };

			// float4 GetBonePos_(uint vid,float4 pos){
			// 	#if defined(BUFFER_ON)
			// 	BoneInfoPerVertex info = _BoneInfoPerVertex[vid];
			// 	uint bonesCount = info.bonesCount;
			// 	uint startIndex = info.bonesStartIndex;
			// 	#else
			// 	uint startIndex = (_BoneStartArray[vid]);
			// 	uint bonesCount = (_BoneCountArray[vid]);

			// 	#endif

			// 	float4 bonePos = (float4)0;
			// 	float4x4 boneMat = {1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1};

			// 	uint boneId = _BoneWeightIndex0[vid];
			// 	float boneWeight =  _BoneWeight0[vid];
			// 	boneMat = _BonesArray[boneId];
			// 	bonePos += mul(boneMat,pos) * boneWeight;

			// 	boneId = _BoneWeightIndex1[vid];
			// 	boneWeight = _BoneWeight1[vid];
			// 	boneMat = _BonesArray[boneId];
			// 	bonePos += mul(boneMat,pos) * boneWeight;

			// 	return bonePos;
			// }

			float4 GetBonePos(uint vid,float4 pos){

				#if defined(BUFFER_ON)
				BoneInfoPerVertex info = _BoneInfoPerVertex[vid];
				uint bonesCount = info.bonesCount;
				uint startIndex = info.bonesStartIndex;
				#else
				uint startIndex = (_BoneStartArray[vid]);
				uint bonesCount = (_BoneCountArray[vid]);

				#endif

				float4 bonePos = (float4)0;
				float4x4 boneMat = {1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1};

				UNITY_UNROLLX(4)
				for(uint i = 0;i <bonesCount;i++) //
				{
					#if defined(BUFFER_ON)
					BoneWeight1 bw = _BoneWeight[startIndex + i];
					uint boneIndex = bw.boneIndex;
					float weight = bw.weight;

					boneMat = _Bones[boneIndex];
					#else
					uint boneIndex = _BoneWeightIndexArray[startIndex+i];
					float weight =  _BoneWeightArray[startIndex+i];

					boneMat = _BonesArray[boneIndex];
					#endif
					
					bonePos += mul(boneMat,pos) * weight;
				}
				return bonePos;
			}


            v2f vert (appdata v)
            {
                v2f o;

				float4 pos = GetBonePos(v.vertexId,v.pos);
				o.vertex = TransformObjectToHClip(pos);
// v.pos.w *=100;
// o.vertex = mul(UNITY_MATRIX_MVP,v.pos);
                o.uv = v.uv;
				o.worldPos = TransformObjectToWorld(pos);

                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
				return float4(i.uv,0,1);
                half4 col = tex2D(_MainTex, i.uv);
                return col;
            }
            ENDHLSL
        }
    }
}
