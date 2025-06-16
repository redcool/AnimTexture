Shader "AnimTexture/BoneTexture"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

		//================================================= AnimTex,get matrix from _AnimTexture
		[Group(GPUSkin)]
        [GroupEnum(GPUSkin,_None _ANIM_TEX_ON _GPU_SKINNED_ON,true,use AnimTex or GpuSkin)] _GpuSkinnedOn("_GpuSkinOn",float) = 0

		[Group(AnimTex)]
		[GroupItem(AnimTex)] _AnimTex("Anim Tex",2d) = ""{}
		[GroupItem(AnimTex)] _AnimSampleRate("Anim Sample Rate",float) = 30
		[GroupItem(AnimTex)] _StartFrame("Start Frame",float) = 0
		[GroupItem(AnimTex)] _EndFrame("End Frame",float) = 1
		[GroupItem(AnimTex)] _Loop("Loop[0:Loop,1:Clamp]",range(0,1)) = 1
		[GroupItem(AnimTex)] _PlayTime("Play Time",float) = 0
		[GroupItem(AnimTex)] _OffsetPlayTime("Offset Play Time",float) = 0

		[GroupItem(AnimTex)] _NextStartFrame("Next Anim Start Frame",float) = 0
		[GroupItem(AnimTex)] _NextEndFrame("Next Anim End Frame",float) = 0
		[GroupItem(AnimTex)] _CrossLerp("Cross Lerp",range(0,1)) = 0

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

		#include "../../PowerShaderLib/Lib/Skinned/AnimTextureLib.hlsl"
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
			#pragma shader_feature_vertex _ _ANIM_TEX_ON _GPU_SKINNED_ON
			#pragma target 4.0

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
				// #if !UNITY_PLATFORM_WEBGL
				// uint4 indices = asuint(v.indices);
				// #else
				// float4 indices = v.indices;
				// #endif

				#if defined(_ANIM_TEX_ON)
					CalcBlendAnimPos(v.vertexId,v.pos/**/,v.normal/**/,v.tangent/**/,v.weights,v.indices);
				#elif defined(_GPU_SKINNED_ON)
				// v.pos = GetSkinnedPos(v.vertexId,v.pos); // get from buffer
                    CalcSkinnedPos(v.vertexId,v.pos/**/,v.normal/**/,v.tangent/**/,v.weights,v.indices);
				#endif

				o.vertex = TransformObjectToHClip(v.pos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

				o.normal = TransformObjectToWorldNormal(v.normal);
				o.weights = v.indices;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
				
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
