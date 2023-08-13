#ifndef X_FXAA_INCLUDED
#define X_FXAA_INCLUDED


#include "../ShaderLibrary/Common/CommonInput.hlsl"

#define FXAA_ABSOLUTE_LUMA_THRESHOLD 0.03
#define FXAA_RELATIVE_LUMA_THRESHOLD 0.5


SamplerState _linear_clamp_sampler;
struct FXAACrossData
{
	half4 M;
	half4 N;
	half4 S;
	half4 W;
	half4 E;
};
static float rgb2luma(half3 color)
{
	return dot(color, half3(0.299, 0.587, 0.114));
}
static half4 SampleLinear(Texture2D tex, float2 uv)
{
	return tex.Sample(_linear_clamp_sampler, uv);
}
static half4 SampleRGBLumaLinear(Texture2D tex, float2 uv)
{
	half3 color = SampleLinear(tex, uv).rgb;
	return half4(color, rgb2luma(color));
}

static FXAACrossData SampleCross(Texture2D tex, float2 uv, float4 offset)
{
	FXAACrossData crossData;
	crossData.M = SampleRGBLumaLinear(tex, uv);
	crossData.S = SampleRGBLumaLinear(tex, uv + float2(0, -offset.y));
	crossData.N = SampleRGBLumaLinear(tex, uv + float2(0, offset.y));
	crossData.W = SampleRGBLumaLinear(tex, uv + float2(-offset.x, 0));
	crossData.E = SampleRGBLumaLinear(tex, uv + float2(offset.x, 0));
	return crossData;
}

half4 FXAA(Texture2D tex, float2 uv)
{
	float2 invTexturesiez = (_ScreenParams.zw - 1);
	
	float4 offset = float4(1, 1, -1, -1) * invTexturesiez.xyxy;
	
	FXAACrossData cross = SampleCross(tex, uv, offset);
	
	half lumaMinNS = min(cross.N.a, cross.S.a);
	half lumaMinWE = min(cross.W.a, cross.E.a);
	half lumaMin = min(cross.M.a, min(lumaMinNS, lumaMinWE));
	half lumaMaxNS = max(cross.N.a, cross.S.a);
	half lumaMaxWE = max(cross.W.a, cross.E.a);
	half lumaMax = max(cross.M.a, max(lumaMaxNS, lumaMaxWE));
	half lumaContrast = lumaMax - lumaMin;
	
	
	float edgeThreshold = max(FXAA_ABSOLUTE_LUMA_THRESHOLD, lumaMax * FXAA_RELATIVE_LUMA_THRESHOLD);
	bool isEdge = lumaContrast > edgeThreshold;
	
	if (isEdge)
	{
		half lumaM = cross.M.a;
		half lumaN = cross.N.a;
		half lumaS = cross.S.a;
		half lumaW = cross.W.a;
		half lumaE = cross.E.a;
		float lumaGradS = lumaS - lumaM;
		float lumaGradN = lumaN - lumaM;
		float lumaGradW = lumaW - lumaM;
		float lumaGradE = lumaE - lumaM;
		
		float lumaGradV = abs(lumaGradS + lumaGradN);
		float lumaGradH = abs(lumaGradW + lumaGradE);
		bool isHorz = lumaGradV > lumaGradH;
		
		float2 normal = float2(0, 0);
		
		if (isHorz)
		{
			normal.y = sign(abs(lumaGradN) - abs(lumaGradS));
		}
		else
		{
			normal.x = sign(abs(lumaGradE) - abs(lumaGradW));
		}
	
		half lumaL = (lumaN + lumaS + lumaE + lumaW) * 0.25;
		half lumaDeltaML = abs(lumaM - lumaL);
		float blend = lumaDeltaML / lumaContrast;
		half4 finalColor = SampleLinear(tex, uv + normal * blend);
		return finalColor;
			

	}
	else
	{
	

		return half4(cross.M.rgb, 1);

	}

 }

#endif