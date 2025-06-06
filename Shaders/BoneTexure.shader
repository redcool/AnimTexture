Shader "Unlit/BoneTexture"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_AnimTex("Anim Tex",2d) = ""{}
		_AnimSampleRate("Anim Sample Rate",float) = 30
		_StartFrame("Start Frame",float) = 0
		_EndFrame("End Frame",float) = 1
		_Loop("Loop[0:Loop,1:Clamp]",range(0,1)) = 1
		_PlayTime("Play Time",float) = 0
		_OffsetPlayTime("Offset Play Time",float) = 0

		_NextStartFrame("Next Anim Start Frame",float) = 0
		_NextEndFrame("Next Anim End Frame",float) = 0
		_CrossLerp("Cross Lerp",range(0,1)) = 0

    }

HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

		sampler2D _AnimTex;
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

		#define _ANIMTEX_TEXELSIZE _AnimTex_TexelSize
		#include "AnimTextureLib.hlsl"
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
                v2f o;

				// float4 pos = GetSkinnedPos(v.vertexId,v.pos); // get from buffer
				// float4 pos = GetAnimPos(v.vertexId,v.pos);
				float4 pos = GetBlendAnimPos(v.vertexId,v.pos);

				o.vertex = TransformObjectToHClip(pos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

				
				// float3 normal = GetBlendAnimPos(v.vertexId,float4(v.normal,0));
				// o.normal = TransformObjectToWorldNormal(normal);
				// float3 worldPos = TransformObjectToWorld(pos);
				// o.worldPos = worldPos;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
				return float4(i.uv,0,1);
				
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
