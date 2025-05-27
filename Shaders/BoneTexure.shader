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

		struct BoneInfoPerVertex{
			uint bonesCount;
			uint bonesStartIndex;
		};
		struct BoneWeight1{
			float weight;
			uint boneIndex;
		};

		StructuredBuffer<float3x4> _BindPoses;
		StructuredBuffer<BoneInfoPerVertex> _BoneInfoPerVertexBuffer;
		StructuredBuffer<BoneWeight1> _BoneWeight1Buffer;
		StructuredBuffer<float3x4> _Bones;
		
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
				float4 pos:POSITION;
            };

            struct v2f
            {
                half4 vertex : SV_POSITION;
                half2 uv : TEXCOORD0;
            };
/**
	_BoneWeight1Buffer
	_BoneInfoPerVertexBuffer
	_BindPoses
*/

			float4 GetPos(uint vid,float4 pos){
				BoneInfoPerVertex boneInfo = _BoneInfoPerVertexBuffer[vid];
				// float3x4 mat = _Bones[vid];
				// BoneWeight1 bw =_BoneWeight1Buffer[vid];
				uint startIndex = asuint(boneInfo.bonesStartIndex);
				uint bonesCount = asuint(boneInfo.bonesCount);
				for(uint i = 0;i <bonesCount;i++) //
				{
					BoneWeight1 bw =_BoneWeight1Buffer[startIndex + i];
					// float3x4 bindpose = _BindPoses[bw.boneIndex];
					float3x4 boneMat = _Bones[bw.boneIndex];

					// float3x3 mat = mul((float3x3) bindpose,(float3x3) boneMat);
					float3x3 mat = (float3x3)boneMat;

					float3 bonePos = mul(mat,pos.xyz);
					bonePos += boneMat._14_24_34;
// return float4(bonePos,1);
					return mul(UNITY_MATRIX_MVP,float4(bonePos.xyz,1));
				}
				return pos;
			}

            v2f vert (appdata v)
            {
                v2f o;

				// float4 pos = GetBlendAnimPos(v.vertexId);
				float4 pos = GetPos(v.vertexId,v.pos);
				o.vertex = pos;
				// o.vertex = TransformObjectToHClip(v.pos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
				return _BindPoses[0]._11_12_13_14;
                // sample the texture
                half4 col = tex2D(_MainTex, i.uv);
                return col;
            }
            ENDHLSL
        }
    }
}
