Shader "GPUSkinning/GPUSkinningDemo"
{
    Properties
    {
        _MainTex ("MainTex", 2D) = "white" { }
        _BaseColor ("Color", Color) = (1, 1, 1, 1)
        // 这里不在改为
        [ReadOnly] _GPUSkinning_TextureMatrix ("GPUSkinning Tex", 2D) = "white"
        [ReadOnly] _GPUSkinning_TextureSize_NumPixelsPerFrame ("GPUSkinning Param", Vector) = (0, 0, 0, 0)

        [Hide] _GPUSkinning_FrameIndex_PixelSegmentation("Param1" , Vector)=(0,0,0,0)
        [Hide] _GPUSkinning_FrameIndex_PixelSegmentation_Blend_CrossFade("Param2" , Vector)=(0,0,0,0)
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque"
        }

        Pass
        {
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #define ROOTOFF_BLENDOFF 1

            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"


            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float3 _GPUSkinning_TextureSize_NumPixelsPerFrame; //这个变量DOTS下不需要注册
                float4 _GPUSkinning_FrameIndex_PixelSegmentation;
                float4 _GPUSkinning_FrameIndex_PixelSegmentation_Blend_CrossFade;
            CBUFFER_END

            #ifdef UNITY_DOTS_INSTANCING_ENABLED
UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
            UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
            UNITY_DOTS_INSTANCED_PROP(float4, _GPUSkinning_FrameIndex_PixelSegmentation)
            UNITY_DOTS_INSTANCED_PROP(float4, _GPUSkinning_FrameIndex_PixelSegmentation_Blend_CrossFade)
UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

            static float4 unity_DOTS_Sampled_BaseColor;
            static float4 unity_DOTS_Sampled_GPUSkinning_FrameIndex_PixelSegmentation;
            static float4 unity_DOTS_Sampled_GPUSkinning_FrameIndex_PixelSegmentation_Blend_CrossFade;

void SetupDOTSLitMaterialPropertyCaches()
{
    unity_DOTS_Sampled_BaseColor                                                            = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor);
    unity_DOTS_Sampled_GPUSkinning_FrameIndex_PixelSegmentation                             = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _GPUSkinning_FrameIndex_PixelSegmentation);
    unity_DOTS_Sampled_GPUSkinning_FrameIndex_PixelSegmentation_Blend_CrossFade             = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _GPUSkinning_FrameIndex_PixelSegmentation_Blend_CrossFade);
}

#undef UNITY_SETUP_DOTS_MATERIAL_PROPERTY_CACHES
#define UNITY_SETUP_DOTS_MATERIAL_PROPERTY_CACHES() SetupDOTSLitMaterialPropertyCaches()

            #define _BaseColor                                                  unity_DOTS_Sampled_BaseColor
            #define _GPUSkinning_FrameIndex_PixelSegmentation                   unity_DOTS_Sampled_GPUSkinning_FrameIndex_PixelSegmentation
            #define _GPUSkinning_FrameIndex_PixelSegmentation_Blend_CrossFade   unity_DOTS_Sampled_GPUSkinning_FrameIndex_PixelSegmentation_Blend_CrossFade
            #endif

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            #include "Packages/com.seikami.rendering.gpuskinning/ShaderLibrary/GPUSkinningInclude.hlsl"

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
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };


            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float4 vertex = skin4(input.positionOS, input.uv2, input.uv3);

                output.positionCS = TransformObjectToHClip(vertex.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // return _GPUSkinning_FrameIndex_PixelSegmentation.x / 50;

                half4 var_MainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _BaseColor;
                return var_MainTex;
            }
            ENDHLSL
        }
    }
    FallBack "Diffuse"
}