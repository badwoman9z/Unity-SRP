Shader "My Pipeline/MetalGbuffer"
{
    Properties
    {       
        _BaseColor ("Color", Color) = (1,1,1,1)
        _Metallic("Metallic",Range(0,1)) = 0.5
        _Roughness("Roughness",Range(0,1)) = 0.5
        _Reflectance("Reflectance",Range(0,1)) = 0.5
        _ClearCoat("ClearCoat",Range(0,1)) = 0.5
        _ClearCoatRoughness("ClearCoatRoughness",Range(0,1)) = 0.5
        _SheenColor("SheenColor",Range(0,1)) = 0.04
        _SubsurfaceColor("SubsurfaceColor",Color) = (1,1,1,1)
		_MaterialID("MaterialID",Float)=0

    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {

           Stencil{
              Ref 2
              Comp Always
              Pass Replace                       
           }
            Tags{"LightMode" = "Gbuffer"}
            HLSLPROGRAM
            		#pragma target 3.5
					#pragma multi_compile_instancing
			        #pragma vertex GbufferVertex
                    #pragma fragment GbufferFragment
                    #include "../ShaderLibrary/Common/SpaceTransform.hlsl"

					struct VertexInput{
						float4 position:POSITION;
						float3 normal:NORMAL;
						float2 uv:TEXCOORD0;
					};

					struct VertexOutput{
						float4 clipPos:SV_POSITION;
						float3 positionWS:VAR_POSITION;
						float2 uv:TEXCOORD0;
						float3 normal:TEXCOORD1;	
					};
					float4 _MainTex_ST;

					 float4 _BaseColor;

					 float _Metallic;
					 float _Roughness;
					 float _Reflectance;
					 float _ClearCoat;
					 float _ClearCoatRoughness;
					 float _SheenColor;
					 float4 _SubsurfaceColor;
					 int _MaterialID;
					VertexOutput GbufferVertex(VertexInput input){
						VertexOutput output;

						output.clipPos = UnityObjectToClipPos(input.position);
						output.uv = TRANSFORM_TEX(input.uv, _MainTex);;
						output.normal = mul(unity_ObjectToWorld, float4( input.normal, 0.0 )).xyz;
						output.positionWS = mul(unity_ObjectToWorld,input.position).xyz;
						return output;
					}

					void GbufferFragment(VertexOutput input,
										out float4 Albedo : SV_Target0,
										out float4 Normal : SV_Target1,
										out float4 MetalOrClearCoat : SV_Target2,
										out float4 Sheen : SV_Target3,
										out float MaterialID:SV_TARGET4){
						  float4 color = _BaseColor;

						  float3 normal = input.normal;
						  float metallic = _Metallic;
						  float roughness = _Roughness;
						  float reflection = _Reflectance;
		
						  Albedo = float4(_BaseColor.rgb,_MaterialID);
						  Normal = float4(normal*0.5+0.5, reflection);
						  MetalOrClearCoat = float4(_ClearCoat, _ClearCoatRoughness, roughness,metallic);
						  Sheen = float4(_SubsurfaceColor.rgb,_SheenColor);
						  MaterialID = _MaterialID;
					}
            ENDHLSL
        }
        Pass
			{
				Name "ShadowCaster"
				Tags{"LightMode" = "ShadowCaster"}

				ZWrite On
				ZTest LEqual
				ColorMask 0
				Cull Back

				HLSLPROGRAM

				#pragma vertex ShadowCasterVertex
				#pragma fragment ShadowCasterFragment
				#include "UnityCG.cginc"
				#include "../ShaderLibrary/ShadowCaster.hlsl"
        
				ENDHLSL
			}
    }
    
}
