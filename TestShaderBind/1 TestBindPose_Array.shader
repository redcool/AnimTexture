Shader "Unlit/TestBindPose_Array"
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
				uint bonesCount;
				uint bonesStartIndex;
			};
			struct BoneWeight1{
				float weight;
				uint boneIndex;
			};

			float4x4 _BonesArray[2];
			float _BoneCountArray[4];
			float _BoneStartArray[4];

			float _BoneWeightArray[6];
			float _BoneWeightIndexArray[6];

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
				uint startIndex = (_BoneStartArray[vid]);
				float bonesCount = (_BoneCountArray[vid]);

				float4 bonePos = (float4)0;
				float4x4 boneMat = {1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1};

				UNITY_UNROLLX(4)
				for(uint i = 0;i <bonesCount;i++) //
				{
					uint boneIndex = _BoneWeightIndexArray[startIndex+i];
					float weight =  _BoneWeightArray[startIndex+i];

					boneMat = _BonesArray[boneIndex];
					bonePos += mul(boneMat,pos) * weight;
				}
				return bonePos;
			}


            v2f vert (appdata v)
            {
                v2f o;

				float4 pos = GetBonePos(v.vertexId,v.pos);

				o.vertex = mul(UNITY_MATRIX_MVP,pos);
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
