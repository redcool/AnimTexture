Shader "Unlit/TestBindPose_Buffer"
{
    Properties
    {

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

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			sampler2D _MainTex;

			struct BoneInfoPerVertex{
				half bonesCount;
				half bonesStartIndex;
			};
			struct BoneWeight1{
				float weight;
				uint boneIndex;
			};

			StructuredBuffer<BoneInfoPerVertex> _BoneInfoPerVertexBuffer;
			StructuredBuffer<BoneWeight1> _BoneWeight1Buffer;
			StructuredBuffer<float3x4> _Bones;

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

			float4 GetBonePos(uint vid,float4 pos){
				BoneInfoPerVertex boneInfo = _BoneInfoPerVertexBuffer[vid];
				uint startIndex = (boneInfo.bonesStartIndex);
				uint bonesCount = (boneInfo.bonesCount);

				float4 bonePos = (float4)0;
				float4x4 boneMat = {1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1};

				UNITY_UNROLLX(4)
				for(uint i = 0;i <bonesCount;i++) //
				{
					BoneWeight1 bw = _BoneWeight1Buffer[startIndex + i];
					float boneIndex = bw.boneIndex;
					float weight = bw.weight;

					boneMat._11_12_13_14 = _Bones[boneIndex]._11_12_13_14;
					boneMat._21_22_23_24 = _Bones[boneIndex]._21_22_23_24;
					boneMat._31_32_33_34 = _Bones[boneIndex]._31_32_33_34;

					bonePos += mul(boneMat,pos) * weight;
				}

				return bonePos;
			}


            v2f vert (appdata v)
            {
                v2f o;

				float4 bonePos = GetBonePos(v.vertexId,v.pos);
				o.vertex =  mul(UNITY_MATRIX_MVP,bonePos);

                o.uv = v.uv;
				o.worldPos = TransformObjectToWorld(bonePos);

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
