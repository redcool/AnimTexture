Shader "AnimTexture/AnimTexture"
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
// ================================================== alpha      
        [Group(Alpha)]
        [GroupHeader(Alpha,BlendMode)]
        [GroupPresetBlendMode(Alpha,,_SrcMode,_DstMode)]_PresetBlendMode("_PresetBlendMode",int)=0
        [HideInInspector]_SrcMode("_SrcMode",int) = 1
        [HideInInspector]_DstMode("_DstMode",int) = 0

        // [GroupHeader(Alpha,Premultiply)]
        // [GroupToggle(Alpha)]_AlphaPremultiply("_AlphaPremultiply",int) = 0

        // [GroupHeader(Alpha,AlphaTest)]
        // [GroupToggle(Alpha,ALPHA_TEST)]_AlphaTestOn("_AlphaTestOn",int) = 0
        // [GroupSlider(Alpha)]_Cutoff("_Cutoff",range(0,1)) = 0.5
// ================================================== Settings
        [Group(Settings)]
        [GroupEnum(Settings,UnityEngine.Rendering.CullMode)]_CullMode("_CullMode",int) = 2
		[GroupToggle(Settings)]_ZWriteMode("ZWriteMode",int) = 1

		/*
		Disabled,Never,Less,Equal,LessEqual,Greater,NotEqual,GreaterEqual,Always
		*/
		[GroupEnum(Settings,UnityEngine.Rendering.CompareFunction)]_ZTestMode("_ZTestMode",float) = 4

        [GroupHeader(Settings,Color Mask)]
        [GroupEnum(Settings,RGBA 16 RGB 15 RG 12 GB 6 RB 10 R 8 G 4 B 2 A 1 None 0)] _ColorMask("_ColorMask",int) = 15
// ================================================== stencil settings
        [Group(Stencil)]
		[GroupEnum(Stencil,UnityEngine.Rendering.CompareFunction)]_StencilComp ("Stencil Comparison", Float) = 0
        [GroupStencil(Stencil)] _Stencil ("Stencil ID", int) = 0
        [GroupEnum(Stencil,UnityEngine.Rendering.StencilOp)]_StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] 
        [GroupItem(Stencil)] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] 
        [GroupItem(Stencil)] _StencilReadMask ("Stencil Read Mask", Float) = 255        
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

        Stencil
		{
			Ref [_Stencil]
			Comp [_StencilComp]
			Pass [_StencilOp]
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
		}

		ZWrite[_ZWriteMode]
		Blend [_SrcMode][_DstMode]
		// BlendOp[_BlendOp]
		Cull[_CullMode]
		ztest[_ZTestMode]
		// ColorMask [_ColorMask]
        
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
