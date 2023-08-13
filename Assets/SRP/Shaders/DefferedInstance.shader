Shader "My Pipeline/DefferedInstance"
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
    }
    SubShader
    {
        Pass
        {
            Tags { "LightMode"="Gbuffer" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
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

        //#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            //StructuredBuffer<float4x4> _validMatrixBuffer;
        //#endif

            // https://answers.unity.com/questions/218333/shader-inversefloat4x4-function.html
            float4x4 inverse(float4x4 input)
            {
                #define minor(a,b,c) determinant(float3x3(input.a, input.b, input.c))
                
                float4x4 cofactors = float4x4(
                    minor(_22_23_24, _32_33_34, _42_43_44), 
                    -minor(_21_23_24, _31_33_34, _41_43_44),
                    minor(_21_22_24, _31_32_34, _41_42_44),
                    -minor(_21_22_23, _31_32_33, _41_42_43),
                    
                    -minor(_12_13_14, _32_33_34, _42_43_44),
                    minor(_11_13_14, _31_33_34, _41_43_44),
                    -minor(_11_12_14, _31_32_34, _41_42_44),
                    minor(_11_12_13, _31_32_33, _41_42_43),
                    
                    minor(_12_13_14, _22_23_24, _42_43_44),
                    -minor(_11_13_14, _21_23_24, _41_43_44),
                    minor(_11_12_14, _21_22_24, _41_42_44),
                    -minor(_11_12_13, _21_22_23, _41_42_43),
                    
                    -minor(_12_13_14, _22_23_24, _32_33_34),
                    minor(_11_13_14, _21_23_24, _31_33_34),
                    -minor(_11_12_14, _21_22_24, _31_32_34),
                    minor(_11_12_13, _21_22_23, _31_32_33)
                );
                #undef minor
                return transpose(cofactors) / determinant(input);
            }

            StructuredBuffer<float4x4> _validMatrixBuffer;

            v2f vert (appdata v, uint instanceID : SV_InstanceID)
            {
                // model matrix
                unity_ObjectToWorld = _validMatrixBuffer[instanceID];
                unity_WorldToObject = inverse(unity_ObjectToWorld);

                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            void frag (
                v2f input,
                out float4 Albedo : SV_Target0,
                out float4 Normal : SV_Target1,
                out float4 MetalOrClearCoat : SV_Target2,
                out float4 Sheen : SV_Target3
            )
            {
                  float4 color = _BaseColor;

	              float3 normal = input.normal;
	              float metallic = _Metallic;
	              float roughness = _Roughness;
	              float reflection = _Reflectance;
		
	              Albedo = color;
	              Normal = float4(normal*0.5+0.5, reflection);
	              MetalOrClearCoat = float4(_ClearCoat, _ClearCoatRoughness, roughness,metallic);
	              Sheen = float4(_SubsurfaceColor.rgb,_SheenColor);
            }
            ENDCG
        }
    }
}
