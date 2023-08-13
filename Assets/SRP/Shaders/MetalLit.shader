Shader "My Pipeline/MetalLit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _DiffuseIBL("Diffuse IBL",Cube) = "defaulttexture" {}
        _SpecularIBL("Specular IBL",Cube) = "defaulttexture" {}
        _BRDFLut("BRDF LUT",2D) = "defaulttexture" {}
    }
    SubShader
    {
       Tags { "RenderType"="Opaque" }

        Cull Off ZWrite Off ZTest Always 
        Pass
        {
            Stencil{
              Ref 1
              Comp Equal
                                 
            }
            HLSLPROGRAM
			        #pragma vertex DefferedLightVertex
                    #pragma fragment DefferedLightVertexFragment
                    #pragma multi_compile X_SHADOW_PCF
                    #include "../ShaderLibrary/Common/SpaceTransform.hlsl"
                    #include "../ShaderLibrary/Shadow.hlsl"
                    #include "../ShaderLibrary/PBR20/PBRLight.hlsl"
                    sampler2D _Albedo;
                    sampler2D _Normal;
                    sampler2D _MetalOrClearCoat;
                    sampler2D _Sheen;
                    sampler2D _DepthTexture;
                    sampler2D _Material;
                    uint Index3DTo1D(uint3 i)
                    {
                        return i.z * _numClusterX * _numClusterY
                            + i.y * _numClusterX
                            + i.x;
                    }




                      struct appdata
                      {
                          float4 vertex : POSITION;
                          float2 uv : TEXCOORD0;
                      };

                      struct v2f
                      {
                          float2 uv : TEXCOORD0;
                          float4 vertex : SV_POSITION;
                      };

                    v2f DefferedLightVertex(appdata v){
                           v2f o;
                           o.vertex = UnityObjectToClipPos(v.vertex);
                           //o.vertex = v.vertex;
                           o.uv = v.uv;
                           return o;

                    }


                    void InitializationBRDFData(out BrdfData brdfData,float reflection,float4 GT2,float4 GT3,float3 albedo){
      
                          brdfData.metallic = min(0.96,GT2.a);
                          brdfData.perceptualRoughness = GT2.b;
                          brdfData.roughness = max(brdfData.perceptualRoughness*brdfData.perceptualRoughness,0.0004);
                          brdfData.baseColor = albedo;
                          brdfData.diffuseColor = (1.0 - brdfData.metallic) * brdfData.baseColor.rgb;
                          brdfData.clearCoat = GT2.r;
                          brdfData.clearCoatPerceptualRoughness = GT2.g;
                          brdfData.clearCoatRoughness = brdfData.clearCoatPerceptualRoughness*brdfData.clearCoatPerceptualRoughness;
                          brdfData.F0 = 0.16 * reflection * reflection * (1.0 - brdfData.metallic) + albedo *  brdfData.metallic;
                          brdfData.F0 = lerp(brdfData.F0,f0ClearCoatToSurface(brdfData.F0),brdfData.clearCoat);
                          brdfData.sheenColor = GT3.a;
                          brdfData.subsurfaceColor = GT3.rgb;

                    }
                    void  DefferedLightVertexFragment(v2f i,
                                                        out float4 col:SV_TARGET0,
                                                        out float depth:SV_DEPTH){
                                    float2 uv = i.uv;
                                    // 从 Gbuffer 解码数据
                                    float3 albedo = tex2D(_Albedo, uv).rgb;

                                    float4 NwithRlect = tex2D(_Normal, uv);
                                    float3 N = NwithRlect.rgb * 2 - 1;
                                    float4 GT2 = tex2D(_MetalOrClearCoat, uv);
                                    float4 GT3 = tex2D(_Sheen, uv);
                                    float d = tex2D(_DepthTexture, uv).r;

                                    depth = d;
                                    float d_lin = Linear01Depth(d);

                                   // depthOut = d;

                                    // 反投影重建世界坐标
                                    float4 ndcPos = float4(uv*2-1, d, 1);
                                    float3 worldPos = ReconstructPositionWS(uv,d);


                                    //阴影计算

                                    float visibility = GetMainLightShadowAtten(worldPos, N);
               
                                    float3 V = normalize(_WorldSpaceCameraPos-worldPos);

                                    BrdfData brdfData;
                                    InitializationBRDFData(brdfData,NwithRlect.a,GT2,GT3,albedo);


                                    //直接光照

	                                float3 sunLight = PhysicallyBaseDirectLighting(brdfData,N,V);


                                    uint x = floor(uv.x*_numClusterX);
                                    uint y = floor(uv.y*_numClusterY);
                                    uint z = floor((1-d_lin)*_numClusterZ);

                                    uint3 cluster_3D = uint3(x,y,z);

                                    uint cluster_1D = Index3DTo1D(cluster_3D);

                                    float3 pointLight = PhysicallyBasePointLighting(cluster_1D,worldPos.xyz,N,V,brdfData);


          


                                    //间接光照

                                    float3 IBLlight = evaluateIBL(brdfData,N,V);


                                    //float3 pointLight= lights[4].color.rgb;
                                    col = float4((sunLight)*visibility+pointLight+IBLlight,1.0);
                
                                    col.rgb = ACESToneMapping(col.rgb,0.3);
                                   // float4 debugColor[3] = {float4(1.0,0.0,0.0,1.0),float4(0.0,1.0,0.0,1.0),float4(0.0,0.0,1.0,1.0)};       
                                   // float materialID = tex2D(_Material,uv).r;
                                   // float4 materialDebug = debugColor[materialID];
                                    //return col;
                    }


            ENDHLSL
        }
    }
}
