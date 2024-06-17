Shader "GPUSkinning/GPUSkinningDemo"
{
    Properties
    {
        _MainTex ("MainTex", 2D) = "white" { }
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            
            Cull Back
            
            HLSLPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag

            #define ROOTOFF_BLENDOFF 1

            #pragma multi_compile_instancing


            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
            #include "Packages/com.seikami.rendering.gpuskinning/ShaderLibrary/GPUSkinningInclude.hlsl"
            
            CBUFFER_START(UnityPerMaterial)


            CBUFFER_END

            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 uv2 : TEXCOORD1;
                float4 uv3 : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };


            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : NORMAL;
            };


            
            Varyings vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float4 vertex = skin4(input.positionOS, input.uv2, input.uv3);

                Varyings output;
                output.positionCS = TransformObjectToHClip(vertex.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;


                return output;
            }


            half4 frag(Varyings input) : SV_Target
            {
                half4 var_MainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                return var_MainTex;
            }
            
            ENDHLSL
        }
    }
    FallBack "Diffuse"
}