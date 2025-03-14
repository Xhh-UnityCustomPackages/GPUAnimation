using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameWish.Game
{
    public class GPUSkinningPlayerResources
    {
        public GPUSkinningAnimation anim = null;

        public GPUSkinningPlayerResources(GPUSkinningAnimation anim)
        {
            this.anim = anim;
        }


        public Vector4 GPUSkinning_FrameIndex_PixelSegmentation { get; private set; }
        public Vector4 GPUSkinning_FrameIndex_PixelSegmentation_Blend_CrossFade { get; private set; }

        public static class ShaderIDs
        {
            public static int GPUSkinning_TextureMatrix = Shader.PropertyToID("_GPUSkinning_TextureMatrix"); //提前转递到材质球
            public static int GPUSkinning_TextureSize_NumPixelsPerFrame = Shader.PropertyToID("_GPUSkinning_TextureSize_NumPixelsPerFrame"); //提前转递到材质球


            public static int GPUSkinning_FrameIndex_PixelSegmentation = Shader.PropertyToID("_GPUSkinning_FrameIndex_PixelSegmentation");

            // public static int GPUSkinning_RootMotion = Shader.PropertyToID("_GPUSkinning_RootMotion");
            public static int GPUSkinning_FrameIndex_PixelSegmentation_Blend_CrossFade = Shader.PropertyToID("_GPUSkinning_FrameIndex_PixelSegmentation_Blend_CrossFade");
            // public static int GPUSkinning_RootMotion_CrossFade = Shader.PropertyToID("_GPUSkinning_RootMotion_CrossFade");
        }

        public void UpdatePlayingData(GPUSkinningClip playingClip, int frameIndex, GPUSkinningClip lastPlayedClip, int frameIndex_crossFade, float crossFadeTime,
            float crossFadeProgress)
        {
            GPUSkinning_FrameIndex_PixelSegmentation = new Vector4(frameIndex, playingClip.pixelSegmentation, 0, 0);

            if (IsCrossFadeBlending(lastPlayedClip, crossFadeTime, crossFadeProgress))
            {
                GPUSkinning_FrameIndex_PixelSegmentation_Blend_CrossFade =
                    new Vector4(frameIndex_crossFade, lastPlayedClip.pixelSegmentation, CrossFadeBlendFactor(crossFadeProgress, crossFadeTime));
            }
        }

        public float CrossFadeBlendFactor(float crossFadeProgress, float crossFadeTime)
        {
            return Mathf.Clamp01(crossFadeProgress / crossFadeTime);
        }

        public bool IsCrossFadeBlending(GPUSkinningClip lastPlayedClip, float crossFadeTime, float crossFadeProgress)
        {
            return lastPlayedClip != null && crossFadeTime > 0 && crossFadeProgress <= crossFadeTime;
        }
    }
}