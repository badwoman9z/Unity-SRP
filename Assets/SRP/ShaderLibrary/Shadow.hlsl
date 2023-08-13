#ifndef SHADOW_INCLUDED
#define SHADOW_INCLUDED
#define MAX_CASCADESHADOW_COUNT 4
float4 _ShadowParams; //x is depthBias,y is normal bias,z is strength
#define ACTIVED_CASCADE_COUNT _ShadowParams.w
float4x4 _XWorldToMainLightCascadeShadowMapSpaceMatrices[MAX_CASCADESHADOW_COUNT];

float4 _XCascadeCullingSpheres[MAX_CASCADESHADOW_COUNT];

#if X_SHADOW_PCF
Texture2D _XMainShadowMap;
SamplerComparisonState sampler_XMainShadowMap;
half4 _ShadowPCFParams; //x is PCF tap count, current support 1 & 4
#else
Texture2D _XMainShadowMap;
SamplerState sampler_XMainShadowMap_point_clamp;
#endif
float4 _ShadowMapSize; //x = 1/shadowMap.width, y = 1/shadowMap.height,z = shadowMap.width,w = shadowMap.height

#include "./ShadowTentFilter.hlsl"

int GetCascadeIndex(float3 positionWS){
    for(int i = 0; i < ACTIVED_CASCADE_COUNT; i ++){
        float4 cullingSphere = _XCascadeCullingSpheres[i];
        float3 center = cullingSphere.xyz;
        float radiusSqr = cullingSphere.w * cullingSphere.w;
        float3 d = (positionWS - center);
        //�������������Ƿ��ڰ�Χ���ڡ�
        if(dot(d,d) <= radiusSqr){
            return i;
        }
    }
    return - 1;
}


///����������ת����ShadowMapTexture�ռ�,����ֵ��xyΪuv��zΪ���
float3 WorldToShadowMapPos(float3 positionWS,int cascadeIndex){
    if(cascadeIndex >= 0){
        float4x4 worldToCascadeMatrix = _XWorldToMainLightCascadeShadowMapSpaceMatrices[cascadeIndex];
        float4 shadowMapPos = mul(worldToCascadeMatrix,float4(positionWS,1));
        shadowMapPos /= shadowMapPos.w;
        return shadowMapPos;
    }else{
        //��ʾ����ShadowMap. ����ʾ��Ӱ��
        #if UNITY_REVERSED_Z
        return float3(0,0,1);
        #else
        return float3(0,0,0);
        #endif
    }
}

float3 WorldToShadowMapPos(float3 positionWS){
    int cascadeIndex = GetCascadeIndex(positionWS);
    return WorldToShadowMapPos(positionWS,cascadeIndex);
}



///������Ӱǿ�ȣ���������[0,1]
float SampleShadowStrength(float3 uvd){
    #if X_SHADOW_PCF
        float atten = 0;
        if(_ShadowPCFParams.x == 1){
            atten = SampleShadowPCF(uvd);
        }else if(_ShadowPCFParams.x == 2){
            atten = SampleShadowPCF3x3_4Tap_Fast(uvd);
        }else if(_ShadowPCFParams.x == 3){
            atten = SampleShadowPCF3x3_4Tap(uvd);
        }else if(_ShadowPCFParams.x == 4){
            atten = SampleShadowPCF5x5_9Tap(uvd);
        }else{
            atten = SampleShadowPCF(uvd);
        }
        return 1-atten;
    #else
        float depth = _XMainShadowMap.Sample(sampler_XMainShadowMap_point_clamp,uvd.xy);
        // float depth = UNITY_SAMPLE_TEX2D(_XMainShadowMap,uvd.xy);
        #if UNITY_REVERSED_Z
        //depth > z
        return step(uvd.z,depth);
        #else   
        return step(depth,uvd.z);
        #endif

    #endif
}
///������������Ƿ�λ�����ƹ����Ӱ֮��(1��ʾ������Ӱ�У�С��1��ʾ����Ӱ��,��ֵ��������Ӱ˥��)
float GetMainLightShadowAtten(float3 positionWS,float3 normalWS){
    #if _RECEIVE_SHADOWS_OFF
        return 1;
    #else
        if(_ShadowParams.z == 0){
            return 1;
        }
        int cascadeIndex = GetCascadeIndex(positionWS);
        float3 shadowMapPos = WorldToShadowMapPos(positionWS + normalWS * _ShadowParams.y,cascadeIndex);
        float shadowStrength = SampleShadowStrength(shadowMapPos);
        return 1 - shadowStrength * _ShadowParams.z;
    #endif
}
#endif