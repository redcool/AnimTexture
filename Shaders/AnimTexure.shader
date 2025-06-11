Shader "Unlit/AnimTexture"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
//================================================= AnimTex
		[Group(AnimTex)]
        // [GroupToggle(AnimTex,_ANIM_TEX_ON)] _AnimTexOn("Anim Tex ON",float) = 0
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

            struct appdata
            {
                float2 uv : TEXCOORD0;
				uint vertexId:SV_VertexID;
				UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
			
				float4 pos = GetBlendAnimPos(v.vertexId);

				o.vertex = TransformObjectToHClip(pos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                // sample the texture
                half4 col = tex2D(_MainTex, i.uv);
                return col;
            }
            ENDHLSL
        }
    }
}
