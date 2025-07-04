﻿Shader "AnimTexture/BoneTexture_DRP"
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

// ================================================== alpha      
        [Group(Alpha)]
        [GroupHeader(Alpha,BlendMode)]
        [GroupPresetBlendMode(Alpha,,_SrcMode,_DstMode)]_PresetBlendMode("_PresetBlendMode",int)=0
        [HideInInspector]_SrcMode("_SrcMode",int) = 1
        [HideInInspector]_DstMode("_DstMode",int) = 0

        // [GroupHeader(Alpha,Premultiply)]
        // [GroupToggle(Alpha)]_AlphaPremultiply("_AlphaPremultiply",int) = 0

        [GroupHeader(Alpha,AlphaTest)]
        [GroupToggle(Alpha,ALPHA_TEST)]_AlphaTestOn("_AlphaTestOn",int) = 0
        [GroupSlider(Alpha)]_Cutoff("_Cutoff",range(0,1)) = 0.5
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
	// #include "../../../PowerShaderLib/Lib/UnityLib.hlsl"
	#include "UnityCG.cginc"
	#define TransformObjectToHClip(pos) UnityObjectToClipPos(pos)
	#define TransformObjectToWorldNormal(normal) mul(normal,(float3x3)unity_WorldToObject)
	#define _MainLightPosition _WorldSpaceLightPos0
	sampler2D _MainTex;

	UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
		UNITY_DEFINE_INSTANCED_PROP(half,_StartFrame)
		UNITY_DEFINE_INSTANCED_PROP(half,_EndFrame)
		UNITY_DEFINE_INSTANCED_PROP(half,_AnimSampleRate)
		UNITY_DEFINE_INSTANCED_PROP(half,_Loop)
		UNITY_DEFINE_INSTANCED_PROP(half,_NextStartFrame)
		UNITY_DEFINE_INSTANCED_PROP(half,_NextEndFrame)
		UNITY_DEFINE_INSTANCED_PROP(half,_CrossLerp)
		UNITY_DEFINE_INSTANCED_PROP(half,_PlayTime)
		UNITY_DEFINE_INSTANCED_PROP(half,_OffsetPlayTime)
		UNITY_DEFINE_INSTANCED_PROP(half4,_AnimTex_TexelSize)
		UNITY_DEFINE_INSTANCED_PROP(half4,_MainTex_ST)
		UNITY_DEFINE_INSTANCED_PROP(half,_Cutoff)
		
	UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

		#define _StartFrame UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_StartFrame)
		#define _EndFrame UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_EndFrame)
		#define _AnimSampleRate UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_AnimSampleRate)
		#define _Loop UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_Loop)
		#define _NextStartFrame UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_NextStartFrame)
		#define _NextEndFrame UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_NextEndFrame)
		#define _CrossLerp UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_CrossLerp)
		#define _PlayTime UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_PlayTime)
		#define _OffsetPlayTime UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_OffsetPlayTime)
					
		#define _AnimTex_TexelSize UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_AnimTex_TexelSize)
		#define _MainTex_ST UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_MainTex_ST)
		#define _Cutoff UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_Cutoff)

	#include "../../../PowerShaderLib/Lib/Skinned/AnimTextureLib.hlsl"
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
			#pragma multi_compile_instancing
			#pragma shader_feature_vertex _ _ANIM_TEX_ON _GPU_SKINNED_ON
			#pragma shader_feature ALPHA_TEST
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
				col.xyz *= col.a;
				#if defined(ALPHA_TEST)
				clip(col.a - _Cutoff);
				#endif
                return col ;
            }
            ENDHLSL
        }
    }
}
