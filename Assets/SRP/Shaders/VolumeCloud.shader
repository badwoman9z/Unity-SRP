Shader "My Pipeline/VolumeCloud"
{
     Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _WeatherTex ("WeatherTex", 2D) = "white" {}
        _Noise3D("Noise 3D",3D) = "while"{}
        _Detail3D("Detail 3D",3D) = "while"{}
        _ColorA("ColorA",COLOR) =  (1,1,1,1)
        _ColorB("ColorB",COLOR) =  (1,1,1,1)
        _Shape_Scale("ShapeScale",Range(0,100)) = 0.002
        _NumSteps("Steps Num",Range(0,100)) = 3
        _NumStepsLight("Num Steps Light",Range(0,100)) = 10
        _LightAbsorptionTowardSun("LightAbsorptionTowardSun",Range(0,1))=0.16
        _LightAbsorptionTowardCloud("LightAbsorptionTowardCloud",Range(0,1))=0.16
        _colorOffset1("ColorOffset1",Float) = 0.86
        _colorOffset2("ColorOffset2",Float) = 0.82
        _DensityThreshold("DensityThreshold",Range(0,1)) = 0
        _DensityMultiplier("DensityMultiplier",Range(0,10)) = 1
        _phaseParams("Phase Params",Vector) = (0.78,0.25,0.29,0.6)
         [Range(RealTime)]_StratusRange ("层云范围", vector) = (0.1, 0.4, 0, 1)
    }
     SubShader
    {
   
        LOD 100

        HLSLINCLUDE
        #pragma enable_cbuffer
        
        #include "../ShaderLibrary/Common/SpaceTransform.hlsl"
        #include "../ShaderLibrary/AA/FXAA.hlsl"
        #include "../ShaderLibrary/LightShading/LightInput.hlsl"
        ENDHLSL

        Pass
        {
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            

            #pragma vertex Vertex
            #pragma fragment Fragment



            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                half4 positionCS    : SV_POSITION;
                half2 uv            : TEXCOORD0;
            };

            Texture2D _MainTex;
            SamplerState sampler_MainTex;
            sampler2D _DepthTexture;

            //体积云采样相关参数
            float _DensityThreshold;
            float _DensityMultiplier;
            float _Shape_Scale;

            //RayMarch相关参数
            int _NumSteps;
            int _NumStepsLight;


            //体积云相关参数
            sampler3D _Noise3D;
            sampler2D _WeatherTex;
            sampler3D _Detail3D;
            
            float4 _boundsMin;
            float4 _boundsMax;

            //体积云形状相关参数




            //体积云光照相关参数
            float _LightAbsorptionTowardSun;
            float _LightAbsorptionTowardCloud;
            float4 _ColorA;
            float4 _ColorB;
            float _colorOffset1;
            float _colorOffset2;
            float _darknessThreshold;
            float4 _phaseParams;

            
            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionCS = ObjectToHClipPosition(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }


            float2 rayBoxDst(float3 boundsMin, float3 boundsMax, 
                            //世界相机位置      光线方向倒数
                            float3 rayOrigin, float3 invRaydir) 
            {
                float3 t0 = (boundsMin - rayOrigin) * invRaydir;
                float3 t1 = (boundsMax - rayOrigin) * invRaydir;
                float3 tmin = min(t0, t1);
                float3 tmax = max(t0, t1);

                float dstA = max(max(tmin.x, tmin.y), tmin.z); //进入点
                float dstB = min(tmax.x, min(tmax.y, tmax.z)); //出去点

                float dstToBox = max(0, dstA);
                float dstInsideBox = max(0, dstB - dstToBox);
                return float2(dstToBox, dstInsideBox);
            }
            float Remap(float original_value, float original_min, float original_max, float new_min, float new_max)
            {
                return new_min + ((original_value - original_min) / (original_max - original_min)) * (new_max - new_min);
            }

            //获取云类型密度
            float GetCloudTypeDensity(float heightFraction, float botom, float cloud_min,float cloud_max)
            {
                
                return saturate(Remap(heightFraction, 0, botom, 0, 1)) * saturate(Remap(heightFraction,cloud_min, cloud_max, 1, 0));
            }

            float sampleDensity(float3 rayPos){
            
                // Constants:
                const int mipLevel = 0;
                const float baseScale = 1/1000.0;
                const float offsetSpeed = 1/100.0;

                // Calculate texture sample positions
                
                float3 size = _boundsMax - _boundsMin;
                float3 boundsCentre = (_boundsMax+_boundsMin) * .5;
                float3 uvw = (size * .5 + rayPos) * baseScale * _Shape_Scale;
                float3 shapeSamplePos = uvw ;


                float heightPercent = (rayPos.y - _boundsMin.y) / size.y;
                

                // Calculate base shape density
                float4 shapeNoise = tex3Dlod(_Noise3D,float4(shapeSamplePos,mipLevel));       
                float4 detailNoise = tex3Dlod(_Detail3D,float4(shapeSamplePos,mipLevel));         
                float weaeherData = tex2Dlod(_WeatherTex,float4(shapeSamplePos.xz,0,0));


                float heightGradient = saturate(Remap(heightPercent, 0.0, weaeherData.r, 1, 0));




                float fbm = dot(shapeNoise.gba,float3(0.5,0.25,0.125));

                float baseShape = Remap(shapeNoise.r,fbm,1.0,0.0,1.0);


                return heightGradient;
            
            }
            float hg(float a, float g) 
            {
              float g2 = g * g;
              return (1 - g2) / (4 * 3.1415 * pow(1 + g2 - 2 * g * (a), 1.5));
            }
            float phase(float a) 
            {
              float blend = 0.5;
              float hgBlend = hg(a, _phaseParams.x) * (1 - blend) + hg(a, -_phaseParams.y) * blend;
              return _phaseParams.z + hgBlend * _phaseParams.w;
            }
           float3 lightMarch(float3 position,DirectionOrPointLight mainLight){
                
	            float3 L = mainLight.directionOrPosition.xyz;
                float dstInsideBox = rayBoxDst(_boundsMin, _boundsMax, position, 1 / L).y;
                float stepSize = dstInsideBox / _NumStepsLight;
                float totalDensity = 0;
                for(int step = 0;step<_NumStepsLight;step++){
                    position +=L*stepSize;
                    totalDensity+=max(0,sampleDensity(position)*stepSize);
                
                }
                float transmittance = exp(-totalDensity * _LightAbsorptionTowardSun);
                float3 cloudColor = lerp(_ColorA, mainLight.color.rgb, saturate(transmittance * _colorOffset1));
                cloudColor = lerp(_ColorB, cloudColor, saturate(pow(transmittance * _colorOffset2, 3)));
                return _darknessThreshold + transmittance * (1 - _darknessThreshold) * cloudColor;
           
           }

               
            float4 sampleRayMarch3D(float3 worldPos,float3 worldViewDir){
                
                float depthEyeLinear = length(worldPos.xyz - _WorldSpaceCameraPos); 
                DirectionOrPointLight mainLight = lights[_mainLightIndex];
                //向灯光方向的散射更强一些
                float cosAngle = dot(worldViewDir, mainLight.directionOrPosition.xyz);
                float3 phaseVal = phase(cosAngle);


                float2 rayToContainerInfo = rayBoxDst(_boundsMin.xyz, _boundsMax.xyz, _WorldSpaceCameraPos, (1 / worldViewDir));
                float dstToBox = rayToContainerInfo.x; //相机到容器的距离
                float dstInsideBox = rayToContainerInfo.y; //返回光线是否在容器中

                float dstLimit = min(depthEyeLinear - dstToBox, dstInsideBox);
                float3 entryPoint = _WorldSpaceCameraPos + worldViewDir * dstToBox; 

               
                float  sumDensity  = 0;
                float  dstTravelled = 0;
                float3 lightEnergy = 0;
                float transmittance = 1;


                float stepSize = dstInsideBox/_NumSteps;

                while(dstTravelled<dstLimit){
                    float3 rayPos = entryPoint + (worldViewDir*dstTravelled);

                    float density = sampleDensity(rayPos);


                    if(density>0){

                        float3 lightTransmittance = lightMarch(rayPos,mainLight);
                        lightEnergy += density*stepSize*transmittance*lightTransmittance*phaseVal;
                        transmittance *= exp(-density*stepSize*_LightAbsorptionTowardCloud);

                        if(transmittance<0.01){
                        
                            break;
                        }
                    
                    }
                    
                    dstTravelled+=stepSize;
                }



                
                return float4(lightEnergy,transmittance);

            
            }
            

            float4 Fragment(Varyings input) : SV_Target
            {
                float d = tex2D(_DepthTexture, input.uv).r;
   
                float3 worldPos = ReconstructPositionWS(input.uv,d);

                float3 worldViewDir = normalize(worldPos.xyz - _WorldSpaceCameraPos) ;


               float4 cloud = sampleRayMarch3D(worldPos,worldViewDir);

                float3 envColor = _MainTex.Sample(sampler_MainTex,input.uv).rgb;

                float3 finalColor = cloud.rgb+ envColor*cloud.a;
                //return _MainTex.Sample(sampler_MainTex,input.uv);
                return float4(finalColor,0);
                
            }
            ENDHLSL
        }
    }
}
