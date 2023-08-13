Shader "My Pipeline/PostProcess"
{

     SubShader
    {
   
        LOD 100

        HLSLINCLUDE
        #pragma enable_cbuffer
        
        #include "../ShaderLibrary/Common/SpaceTransform.hlsl"
        #include "../ShaderLibrary/AA/FXAA.hlsl"
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

            sampler2D _BlitTex;
            SamplerState sampler_BlitTex;

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionCS = ObjectToHClipPosition(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                

                return tex2D(_BlitTex,input.uv);

                
            }
            ENDHLSL
        }
    }
}
