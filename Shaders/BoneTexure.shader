Shader "Unlit/BoneTexture"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

//================================================= AnimTex
		[Group(AnimTex)]
        [GroupToggle(AnimTex,_ANIM_TEX_ON)] _AnimTexOn("Anim Tex",float) = 0
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
			#pragma shader_feature _ANIM_TEX_ON

            struct appdata
            {
				uint vertexId:SV_VertexID;
				float4 pos:POSITION;
                float2 uv : TEXCOORD0;
				float3 normal:NORMAL;
            };

            struct v2f
            {
                half4 vertex : SV_POSITION;
                half2 uv : TEXCOORD0;
				float3 normal:TEXCOORD1;
				float3 worldPos:TEXCOORD2;
            };


            v2f vert (appdata v)
            {
                v2f o = (v2f)0;

				#if defined(_ANIM_TEX_ON)
				// float4 pos = GetSkinnedPos(v.vertexId,v.pos); // get from buffer
				// float4 pos = GetAnimPos(v.vertexId,v.pos);
				float4 pos = GetBlendAnimPos(v.vertexId,v.pos);
				v.pos = pos;

				float3 normal = GetBlendAnimPos(v.vertexId,float4(v.normal,0));
				v.normal = normal;
				#endif

				o.vertex = TransformObjectToHClip(v.pos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

				o.normal = TransformObjectToWorldNormal(v.normal);
				// float3 worldPos = TransformObjectToWorld(pos);
				// o.worldPos = worldPos;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
				// return float4(i.uv,0,1);
				
				float3 n = normalize(i.normal);
				float3 worldPos = i.worldPos;
				// n = normalize(cross(ddy(worldPos),ddx(worldPos)));
				
				float nl = saturate(dot(n,_MainLightPosition.xyz));
				return nl;
                // sample the texture
                half4 col = tex2D(_MainTex, i.uv);
                return col;
            }
            ENDHLSL
        }
    }
}
