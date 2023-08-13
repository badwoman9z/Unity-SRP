#ifndef LIGHT_INCLUDED
#define LIGHT_INCLUDED

#include "./PBRBRDF.hlsl"
#include "./IBLinput.hlsl"

int _lightCount;
int _mainLightIndex;

struct DirectionOrPointLight{
	float4 color;
	float4 directionOrPosition;

};
struct PointLight
{
    float3 color;
    float intensity;
    float3 position;
    float radius;
};
struct LightIndex
{
    int count;
    int start;
};

StructuredBuffer<DirectionOrPointLight> lights;

StructuredBuffer<PointLight> _lightBuffer;
StructuredBuffer<uint> _lightAssignBuffer;
StructuredBuffer<LightIndex> _assignTable;

float _numClusterX;
float _numClusterY;
float _numClusterZ;





float perceptualRougnhessToLod(float perceptualRoughness)
{
    float rgh = perceptualRoughness * (1.7 - 0.7 * perceptualRoughness);
    return 6.0 * rgh;
}

//IBL高光
float3 prefilteredRadiance(float3 R, float perceptualRoughness)
{

    
    float lod = perceptualRougnhessToLod(perceptualRoughness);
    
    
    float3 Lr = texCUBElod(_SpecularIBL, float4(R, lod)).rgb;
    

    
    return Lr;
}
//IBL漫反射
float3 diffuseInrradiance(float3 diffuseNormal,float3 diffuseColor)
{
    return texCUBE(_DiffuseIBL, diffuseNormal).rgb * diffuseColor;

}
float3 evaluateIBL(BrdfData brdfData,float3 N,float3 V)
{
    
    
    float3 R = reflect(-V, N);
    
    float NoV = max(dot(N, V), 0.0000001);
    
    

    
    float3 Fr = float3(0.0, 0.0, 0.0);
    
    Fr = prefilteredRadiance(R, brdfData.perceptualRoughness);
    
    float2 Ldfg = tex2D(_BRDFLut, float2(NoV, brdfData.perceptualRoughness)).rg;
    
    float3 F90 = float3(1, 1, 1);
    
    Fr = Fr * (brdfData.F0 * Ldfg.x + F90 * Ldfg.y);
    
    
    float3 Fd = diffuseInrradiance(N, brdfData.diffuseColor);
    
    
    
    float Fc = F_Schlick(0.04, 1.0, NoV) * brdfData.clearCoat;
    
    Fd *= 1.0 - Fc;
    Fr *= (1.0 - Fc) * (1.0 - Fc);
    Fr += prefilteredRadiance(R, brdfData.clearCoatPerceptualRoughness) * Fc;
    
    return Fr + Fd;

}


float3 PhysicallyBaseDirectLighting(BrdfData brdfData, float3 N, float3 V)
{
    DirectionOrPointLight mainLight = lights[_mainLightIndex];

    float3 L = mainLight.directionOrPosition.xyz;



    
    
    
    
    float3 H = normalize(V + L);
    
    float NoV = max(dot(N, V), 0);
    float HoV = max(dot(H, V), 0);
    float LoH = max(dot(L, H), 0);
    float NoL = max(dot(N, L), 0);
    float NoH = max(dot(N, H), 0);
    
    float3 Fr = specularLobe(brdfData,NoV, NoL, NoH, LoH);
    
    float3 Fd = diffuseLobe(brdfData, NoV, NoL, LoH);
    
    float3 color = Fd + Fr;
    
    
    //
    float Fcc;
    float clearCoat = clearCoatLobe(brdfData, NoH, LoH, Fcc);
    float attenuation = 1.0 - Fcc;
    
    color *= attenuation * NoL;
    
    color += clearCoat * NoL;
    
    return mainLight.color.rgb * mainLight.color.a * color;
    
}
float3 PointLightAtten(float3 lightPos,float3 positionWS,float lightRange){
	float rangeSqr = lightRange*lightRange;
	float3 lightVector = lightPos-positionWS;
	float distanceToLightSqr = dot(lightVector,lightVector);    
	float factor = saturate(1 - distanceToLightSqr * rcp(rangeSqr));
    factor = factor * factor;
    return  factor * rcp(max(distanceToLightSqr,0.001));
}
float3 PhysicallyBasePointLighting(uint clusterIndex, float3 positionWS,float3 N, float3 V, BrdfData brdfData){

        
    
    float NoV = max(dot(N, V), 0);


    float3 pointColor = float3(0,0,0);
	
	LightIndex lIndex = _assignTable[clusterIndex];

	int start = lIndex.start;
	int end = start+lIndex.count;

	for(int i=start;i<end;i++){
		int pIndex = _lightAssignBuffer[i];
		PointLight p = _lightBuffer[pIndex];

		float3 L = normalize(p.position-positionWS);
        float3 H = normalize(V + L);
        float HoV = max(dot(H, V), 0);
        float LoH = max(dot(L, H), 0);
        float NoL = max(dot(N, L), 0);
        float NoH = max(dot(N, H), 0);
		float3 radiance = p.color;

        float3 Fr = specularLobe(brdfData,NoV, NoL, NoH, LoH);
    
        float3 Fd = diffuseLobe(brdfData, NoV, NoL, LoH);
    
        float3 brdf = Fd + Fr;
    
    
        //
        float Fcc;
        float clearCoat = clearCoatLobe(brdfData, NoH, LoH, Fcc);
        float attenuation = 1.0 - Fcc;
    
        brdf *= attenuation * NoL;
    
        brdf += clearCoat * NoL;


		pointColor	+= brdf*radiance*p.intensity*PointLightAtten(p.position,positionWS,p.radius);
		//pointColor+=radiance;
	}
	
	
	return pointColor;

}

float3 ClothShading(BrdfData brdfData, float3 N, float3 V, float visibility)
{
    DirectionOrPointLight mainLight = lights[_mainLightIndex];

    float3 L = mainLight.directionOrPosition.xyz;
    
    float3 H = normalize(V + L);
    
    float NoV = max(dot(N, V), 0);
    float HoV = max(dot(H, V), 0);
    float LoH = max(dot(L, H), 0);
    float NoL = max(dot(N, L), 0);
    float NoH = max(dot(N, H), 0);
    
    // 镜面BRDF
    float D = distributionCloth(brdfData.roughness, NoH);
    float G = visibilityCloth(NoV, NoL);
    float3 F = brdfData.sheenColor;
    float3 Fr = (D * G) * F;

    // 漫反射BRDF
    float diffuseC = diffuse(brdfData.roughness, NoV, NoL, LoH);

    diffuseC *= saturate((dot(N, L) + 0.5) / 2.25);

    float3 Fd = diffuseC * brdfData.diffuseColor;


    Fd *= saturate(brdfData.subsurfaceColor + NoL);
    float3 color = Fd + Fr * NoL;
    color *= (mainLight.color.a * visibility) * mainLight.color.rgb;


    return color;


}
      float3 ACESToneMapping(float3 color, float adapted_lum)
  {
     const float A = 2.51f;
     const float B = 0.03f;
     const float C = 2.43f;
     const float D = 0.59f;
     const float E = 0.14f;

     color *= adapted_lum;
     return (color * (A * color + B)) / (color * (C * color + D) + E);
  }


#endif