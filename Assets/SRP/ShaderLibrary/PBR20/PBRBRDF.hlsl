#ifndef PBR20_INCLUDED
#define PBR20_INCLUDED
#include "./IBLinput.hlsl"
#define UNITY_PI 3.14159265359
struct BrdfData
{
    float perceptualRoughness;
    float roughness;
    float3 F0;
    float3 baseColor;
    float3 diffuseColor;
    float metallic;
    
    float clearCoat;
    float clearCoatPerceptualRoughness;
    float clearCoatRoughness;
    
    float sheenColor;
    float3 subsurfaceColor;
};


float sqr(float x)
{
    return x * x;
}

float pow5(float src)
{
    float res = src * src;
    return res * res * src;
}
float pow2(float src)
{
    return src * src;
}


float D_Charlie(float roughness, float NoH)
{
    float invAlpha = 1.0 / roughness;
    float cos2h = NoH * NoH;
    float sin2h = max(1.0 - cos2h, 0.0078125);
    return (2.0 + invAlpha) * pow(sin2h, invAlpha * 0.5) / (2.0 * UNITY_PI);
}


float D_GGX(float roughness, float NoH)
{
    float a = roughness * roughness;
    float a2 = a * a;
    float d = (NoH * a2 - NoH) * NoH + 1; // 2 mad
    return a2 / (UNITY_PI * d * d);
}

float V_SmithGGXCorrelated(float roughness, float NoV, float NoL)
{
    float a2 = roughness * roughness;
    float lambdaV = NoL * sqrt((NoV - a2 * NoV) * NoV + a2);
    float lambdaL = NoV * sqrt((NoL - a2 * NoL) * NoL + a2);
    float v = 0.5 / (lambdaV + lambdaL);
    return v;
}



float3 f0ClearCoatToSurface(const float3 f0)
{

    return saturate(f0 * (f0 * (0.941892 - 0.263008 * f0) + 0.346479) - 0.0285998);

}

float3 F_Schlick(const float3 f0, float f90, float VoH)
{
    // Schlick 1994, "An Inexpensive BRDF Model for Physically-Based Rendering"
    return f0 + (f90 - f0) * pow5(1.0 - VoH);
}

float3 F_Schlick(const float3 f0, float VoH)
{
    float f = pow(1.0 - VoH, 5.0);
    return f + f0 * (1.0 - f);
}

float F_Schlick(float f0, float f90, float VoH)
{
    return f0 + (f90 - f0) * pow5(1.0 - VoH);
}
float V_Kelemen(float LdotH)
{
    return .25f / max(pow2(LdotH), .00001f);
}
float V_Neubelt(float NoV, float NoL)
{
    
    return 1.0 / (4.0 * (NoL + NoV - NoL * NoV));
}

float Fd_Burley(float roughness, float NoV, float NoL, float LoH)
{
    float f90 = 0.5 + 2.0 * roughness * LoH * LoH;
    float lightScatter = F_Schlick(1.0, f90, NoL);
    float viewScatter = F_Schlick(1.0, f90, NoV);
    return lightScatter * viewScatter * (1.0 / UNITY_PI);
}
float diffuse(float roughness, float NoV, float NoL, float LoH)
{

    return Fd_Burley(roughness, NoV, NoL, LoH);

}


//---------------------------------------
float distribution(float roughness, float NoH)
{
    return D_GGX(roughness, NoH);
}

float distributionClearCoat(float roughness, float NoH)
{
    return D_GGX(roughness, NoH);
}

float distributionCloth(float roughness, float NoH)
{
    return D_Charlie(roughness, NoH);
}

float visibility(float roughness, float NoV, float NoL)
{

    return V_SmithGGXCorrelated(roughness, NoV, NoL);

}

float visibilityClearCoat(float LoH)
{
    return V_Kelemen(LoH);
}

float visibilityCloth(float NoV, float NoL)
{
    return V_Neubelt(NoV, NoL);
}

float3 fresnel(float3 f0, float LoH)
{
    return F_Schlick(f0, LoH); // f90 = 1.0

}
//---------------------------------------
float3 specularLobe(BrdfData brdfData, float NoV, float NoL, float NoH, float LoH)
{
    float D = distribution(brdfData.roughness, NoH);
    
    float V = visibility(brdfData.roughness, NoV, NoL);
    
    float3 F = fresnel(brdfData.F0, LoH);
    
    return (D * V) * F;
    
}


float3 diffuseLobe(BrdfData brdfData, float NoV, float NoL, float LoH)
{
    return brdfData.diffuseColor * diffuse(brdfData.roughness, NoV, NoL, LoH);

}
//---------------------------------------

float clearCoatLobe(BrdfData brdfData, float NoH, float LoH, out float Fcc)
{

    float D = distributionClearCoat(brdfData.clearCoatRoughness, NoH);
    float V = visibilityClearCoat(LoH);
    float F = F_Schlick(0.04, 1.0, LoH) * brdfData.clearCoat; // fix IOR to 1.5

    Fcc = F;
    return D * V * F;
}




#endif

