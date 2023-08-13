#ifndef SHADOW_CASTER_INCLUDED
#define SHADOW_CASTER_INCLUDED
struct ShadowCasterAttributes
{
    float4 positionOS   : POSITION;
};

struct ShadowCasterVaryings
{
    float4 positionCS   : SV_POSITION;
};

ShadowCasterVaryings ShadowCasterVertex(ShadowCasterAttributes input)
{
    ShadowCasterVaryings output;
    float4 positionCS = UnityObjectToClipPos(input.positionOS);
    output.positionCS = positionCS;
    return output;
}

half4 ShadowCasterFragment(ShadowCasterVaryings input) : SV_Target
{
    return 0;
}
#endif