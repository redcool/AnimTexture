Shader "AnimTexture/GpuSkinned"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

//================================================= AnimTex
		[Group(GPUSkin)]
        [GroupToggle(GPUSkin,_GPU_SKINNED_ON)] _GpuSkinnedOn("_GpuSkinOn",float) = 0

    }

HLSLINCLUDE
	#include "../../PowerShaderLib/Lib/UnityLib.hlsl"

		sampler2D _MainTex;

		CBUFFER_START(UnityPerMaterial)
			half _StartFrame;
			half _EndFrame;
			half _AnimSampleRate;
			half _Loop;
			half _NextStartFrame;
			half _NextEndFrame;
			half _CrossLerp;
			half _PlayTime;
			half _OffsetPlayTime;
						
			half4 _AnimTex_TexelSize;

			half4 _MainTex_ST;
		CBUFFER_END

        #define USE_BUFFER
		#include "../../PowerShaderLib/Lib/Skinned/SkinnedLib.hlsl"
ENDHLSL
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma shader_feature _GPU_SKINNED_ON
			#pragma target 3.0

            struct appdata
            {
				uint vertexId:SV_VertexID;
                float2 uv : TEXCOORD0;
				float4 pos:POSITION;
				float4 normal:NORMAL;
				float4 tangent:TANGENT;
				float4 weights:BLENDWEIGHTS;
				uint4 indices:BLENDINDICES;
            };

            struct v2f
            {
                half4 vertex : SV_POSITION;
                half2 uv : TEXCOORD0;
				float3 normal:TEXCOORD1;
				float4 weights:TECOORD2;
            };


            v2f vert (appdata v)
            {
                v2f o = (v2f)0;

				#if defined(_GPU_SKINNED_ON)
				v.pos = GetSkinnedPos(v.vertexId,v.pos); // get from buffer
				#endif 
				o.vertex = TransformObjectToHClip(v.pos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

				o.normal = TransformObjectToWorldNormal(v.normal);
				o.weights = v.weights;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
				// return i.weights.x;
				
				float3 n = normalize(i.normal);
				
				float nl = saturate(dot(n,_MainLightPosition.xyz));
				
                // sample the texture
                half4 col = tex2D(_MainTex, i.uv);
                return col * nl;
            }
            ENDHLSL
        }
    }
}
