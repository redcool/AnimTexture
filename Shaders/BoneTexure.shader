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

		_BoneCount("_BoneCount",int) = 1
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

		StructuredBuffer<BoneInfoPerVertex> _BoneInfoPerVertexBuffer;
		StructuredBuffer<BoneWeight1> _BoneWeightBuffer;
		StructuredBuffer<float4x4> _Bones;

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

			float _BoneCount; 
		CBUFFER_END

		struct AnimInfo {
			uint frameRate;
			uint startFrame;
			uint endFrame;
			half loop;
			half playTime;
			uint offsetPlayTime;
		};


		AnimInfo GetAnimInfo(){
			AnimInfo info =(AnimInfo)0;

			info.frameRate = _AnimSampleRate;
			info.startFrame = _StartFrame;
			info.endFrame = _EndFrame;
			info.loop = _Loop;
			info.playTime = _PlayTime;
			info.offsetPlayTime = _OffsetPlayTime;
			return info;
		}

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


		float4 GetAnimPos(uint vid,float4 pos){
			float4 bonePos = (float4)0;

			AnimInfo info = GetAnimInfo();			
			half y = GetY(info);
			half x = (vid + 0.5) * _AnimTex_TexelSize.x;

			BoneInfoPerVertex boneInfo = _BoneInfoPerVertexBuffer[vid];
			float bonesCount = boneInfo.bonesCount;
			float boneStart = boneInfo.bonesStartIndex;

			float4x4 boneMat = {1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1};

			UNITY_UNROLLX(4)
			for(int i=0;i<bonesCount;i++){
				BoneWeight1 bw = _BoneWeightBuffer[boneStart + i];
				float weight = bw.weight;
				uint boneIndex = bw.boneIndex;

				boneMat = _Bones[boneIndex];

				// get float3x4 from boneTex
				
				x = (boneIndex/_BoneCount+0.5);

				// boneMat[0] = tex2Dlod(_AnimTex,float4(x,y,0,0));
				// boneMat[1] = tex2Dlod(_AnimTex,float4(x + _AnimTex_TexelSize.x,y,0,0));
				// boneMat[2] = tex2Dlod(_AnimTex,float4(x + _AnimTex_TexelSize.x * 2,y,0,0));


				bonePos += mul(boneMat,pos) * weight;
			}

			return bonePos;
		}

		float4 GetAnimPos(uint vid,AnimInfo info) {
			half y = GetY(info);
			half x = (vid + 0.5) * _AnimTex_TexelSize.x;

			float4 animPos = tex2Dlod(_AnimTex, half4(x, y, 0, 0));
			return animPos;
		}		

		float4 GetBlendAnimPos(uint vid) {
			AnimInfo info = GetAnimInfo();
			half crossLerp = _CrossLerp;
			float4 curPos = GetAnimPos(vid, info);

			info.startFrame = _NextStartFrame;
			info.endFrame = _NextEndFrame;
			float4 nextPos = GetAnimPos(vid, info);

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


            v2f vert (appdata v)
            {
                v2f o;

				float4 pos = GetAnimPos(v.vertexId,v.pos);
				o.vertex = TransformObjectToHClip(pos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
				return float4(i.uv,0,1);
                // sample the texture
                half4 col = tex2D(_MainTex, i.uv);
                return col;
            }
            ENDHLSL
        }
    }
}
