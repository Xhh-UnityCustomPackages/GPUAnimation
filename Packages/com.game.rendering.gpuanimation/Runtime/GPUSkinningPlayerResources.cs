using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.GPUSkinning
{
    public class GPUSkinningPlayerResources
    {
        public GPUSkinningAnimation anim = null;
        public Texture2D texture = null;


        private float time = 0;
        public float Time
        {
            get
            {
                return time;
            }
            set
            {
                time = value;
            }
        }


        internal class ShaderIDs
        {
            public static int GPUSkinning_TextureMatrix = Shader.PropertyToID("_GPUSkinning_TextureMatrix");
            public static int GPUSkinning_TextureSize_NumPixelsPerFrame = Shader.PropertyToID("_GPUSkinning_TextureSize_NumPixelsPerFrame");
            public static int GPUSkinning_FrameIndex_PixelSegmentation = Shader.PropertyToID("_GPUSkinning_FrameIndex_PixelSegmentation");
            public static int GPUSkinning_RootMotion = Shader.PropertyToID("_GPUSkinning_RootMotion");
            public static int GPUSkinning_FrameIndex_PixelSegmentation_Blend_CrossFade = Shader.PropertyToID("_GPUSkinning_FrameIndex_PixelSegmentation_Blend_CrossFade");
            public static int GPUSkinning_RootMotion_CrossFade = Shader.PropertyToID("_GPUSkinning_RootMotion_CrossFade");
        }

        public void InitMaterial(Material material)
        {
            material.SetTexture(ShaderIDs.GPUSkinning_TextureMatrix, texture);
            material.SetVector(ShaderIDs.GPUSkinning_TextureSize_NumPixelsPerFrame,
                new Vector4(anim.textureWidth, anim.textureHeight, anim.bones.Length * 3/*treat 3 pixels as a float3x4*/, 0));
        }

        public void Update(float deltaTime)
        {
            time += deltaTime;
        }

        public void UpdatePlayingData(MaterialPropertyBlock mpb, GPUSkinningClip playingClip, int frameIndex, GPUSkinningFrame frame)
        {
            mpb.SetVector(ShaderIDs.GPUSkinning_FrameIndex_PixelSegmentation, new Vector4(frameIndex, playingClip.pixelSegmentation, 0, 0));
        }

        // public float CrossFadeBlendFactor(float crossFadeProgress, float crossFadeTime)
        // {
        //     return Mathf.Clamp01(crossFadeProgress / crossFadeTime);
        // }

        // public bool IsCrossFadeBlending(GPUSkinningClip lastPlayedClip, float crossFadeTime, float crossFadeProgress)
        // {
        //     return lastPlayedClip != null && crossFadeTime > 0 && crossFadeProgress <= crossFadeTime;
        // }

    }
}
