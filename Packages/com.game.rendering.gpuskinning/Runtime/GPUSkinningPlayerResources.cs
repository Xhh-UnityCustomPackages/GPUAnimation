using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace GameWish.Game
{
    public static class GPUSkinningPlayerResources
    {
        public static class ShaderIDs
        {
            public static int GPUSkinning_TextureMatrix = Shader.PropertyToID("_GPUSkinning_TextureMatrix"); //提前转递到材质球
            public static int GPUSkinning_TextureSize_NumPixelsPerFrame = Shader.PropertyToID("_GPUSkinning_TextureSize_NumPixelsPerFrame"); //提前转递到材质球


            public static int GPUSkinning_FrameIndex_PixelSegmentation = Shader.PropertyToID("_GPUSkinning_FrameIndex_PixelSegmentation");

            // public static int GPUSkinning_RootMotion = Shader.PropertyToID("_GPUSkinning_RootMotion");
            public static int GPUSkinning_FrameIndex_PixelSegmentation_Blend_CrossFade = Shader.PropertyToID("_GPUSkinning_FrameIndex_PixelSegmentation_Blend_CrossFade");
            // public static int GPUSkinning_RootMotion_CrossFade = Shader.PropertyToID("_GPUSkinning_RootMotion_CrossFade");
        }

        public static void UpdatePlayingData(GPUSkinningClip playingClip, int frameIndex, GPUSkinningClip lastPlayedClip, int frameIndex_crossFade, float crossFadeTime, float crossFadeProgress,
            out float4 GPUSkinning_FrameIndex_PixelSegmentation, out float4 GPUSkinning_FrameIndex_PixelSegmentation_Blend_CrossFade)
        {
            GPUSkinning_FrameIndex_PixelSegmentation = new float4(frameIndex, playingClip.pixelSegmentation, 0, 0);
            GPUSkinning_FrameIndex_PixelSegmentation_Blend_CrossFade = 0;
            

            if (IsCrossFadeBlending(lastPlayedClip, crossFadeTime, crossFadeProgress))
            {
                GPUSkinning_FrameIndex_PixelSegmentation_Blend_CrossFade =
                    new float4(frameIndex_crossFade, lastPlayedClip.pixelSegmentation, CrossFadeBlendFactor(crossFadeProgress, crossFadeTime), 0);
            }
        }

        private static float CrossFadeBlendFactor(float crossFadeProgress, float crossFadeTime)
        {
            return Mathf.Clamp01(crossFadeProgress / crossFadeTime);
        }

        public static bool IsCrossFadeBlending(GPUSkinningClip lastPlayedClip, float crossFadeTime, float crossFadeProgress)
        {
            return lastPlayedClip != null && crossFadeTime > 0 && crossFadeProgress <= crossFadeTime;
        }
    }
}