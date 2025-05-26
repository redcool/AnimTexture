Shader "Unlit/AnimTexture"
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
		
		#define Props UnityPerMaterial

		UNITY_INSTANCING_BUFFER_START(Props)
			UNITY_DEFINE_INSTANCED_PROP(half, _StartFrame)
			UNITY_DEFINE_INSTANCED_PROP(half, _EndFrame)
			UNITY_DEFINE_INSTANCED_PROP(half, _AnimSampleRate)
			UNITY_DEFINE_INSTANCED_PROP(half, _Loop)
			UNITY_DEFINE_INSTANCED_PROP(half, _NextStartFrame)
			UNITY_DEFINE_INSTANCED_PROP(half, _NextEndFrame)
			UNITY_DEFINE_INSTANCED_PROP(half, _CrossLerp)
			UNITY_DEFINE_INSTANCED_PROP(half, _PlayTime)
			UNITY_DEFINE_INSTANCED_PROP(half, _OffsetPlayTime)
			
			UNITY_DEFINE_INSTANCED_PROP(half4, _AnimTex_TexelSize)
			UNITY_DEFINE_INSTANCED_PROP(half4, _MainTex_ST)
		UNITY_INSTANCING_BUFFER_END(Props)
		// shortcuts
		#define _StartFrame UNITY_ACCESS_INSTANCED_PROP(Props,_StartFrame)
		#define _EndFrame UNITY_ACCESS_INSTANCED_PROP(Props,_EndFrame)
		#define _AnimSampleRate UNITY_ACCESS_INSTANCED_PROP(Props,_AnimSampleRate)
		#define _Loop UNITY_ACCESS_INSTANCED_PROP(Props,_Loop)
		#define _NextStartFrame UNITY_ACCESS_INSTANCED_PROP(Props,_NextStartFrame)

		#define _NextEndFrame UNITY_ACCESS_INSTANCED_PROP(Props,_NextEndFrame)
		#define _CrossLerp UNITY_ACCESS_INSTANCED_PROP(Props,_CrossLerp)
		#define _PlayTime UNITY_ACCESS_INSTANCED_PROP(Props,_PlayTime)
		#define _OffsetPlayTime UNITY_ACCESS_INSTANCED_PROP(Props,_OffsetPlayTime)
		#define _AnimTex_TexelSize UNITY_ACCESS_INSTANCED_PROP(Props,_AnimTex_TexelSize)

		#define _MainTex_ST UNITY_ACCESS_INSTANCED_PROP(Props,_MainTex_ST)


		struct AnimInfo {
			uint frameRate;
			uint startFrame;
			uint endFrame;
			half loop;
			half playTime;
			uint offsetPlayTime;
		};

		half GetY(AnimInfo info) {
			// length = fps/sampleRatio
			half totalLen = _AnimTex_TexelSize.w / info.frameRate;
			half start = info.startFrame / _AnimTex_TexelSize.w;
			half end = info.endFrame / _AnimTex_TexelSize.w;
			half len = end - start;
			half y = start + (info.playTime + info.offsetPlayTime) / totalLen % len;
			y = lerp(y, end, info.loop);
			return y;
		}

		half4 GetAnimPos(uint vertexId, AnimInfo info) {
			half y = GetY(info);
			half x = (vertexId + 0.5) * _AnimTex_TexelSize.x;

			half4 animPos = tex2Dlod(_AnimTex, half4(x, y, 0, 0));
			return animPos;
		}
		half4 GetBlendAnimPos(uint vertexId) {
			AnimInfo info =(AnimInfo)0;

			info.frameRate = _AnimSampleRate;
			info.startFrame = _StartFrame;
			info.endFrame = _EndFrame;
			info.loop = _Loop;
			info.playTime = _PlayTime;
			info.offsetPlayTime = _OffsetPlayTime;
			half crossLerp = _CrossLerp;
			half4 curPos = GetAnimPos(vertexId, info);

			info.startFrame = _NextStartFrame;
			info.endFrame = _NextEndFrame;
			half4 nextPos = GetAnimPos(vertexId, info);

			return lerp(curPos, nextPos, crossLerp);
		}		
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
                half2 uv : TEXCOORD0;
				uint vertexId:SV_VertexID;
				UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                half4 vertex : SV_POSITION;
                half2 uv : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
				UNITY_SETUP_INSTANCE_ID(v);

				half4 pos = GetBlendAnimPos(v.vertexId);

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
