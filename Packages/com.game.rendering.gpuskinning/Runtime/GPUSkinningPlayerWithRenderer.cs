using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace GameWish.Game
{
    public class GPUSkinningPlayerWithRenderer : GPUSkinningPlayer
    {
        private MeshRenderer meshRenderer = null;
        private MaterialPropertyBlock mpb = null;

        public MaterialPropertyBlock materialPropertyBlock => mpb;
        public MeshRenderer MeshRenderer => meshRenderer;


        public GPUSkinningPlayerWithRenderer(MeshRenderer meshRenderer, GPUSkinningAnimation anim) : base(anim)
        {
            this.meshRenderer = meshRenderer;
            mpb = new MaterialPropertyBlock();
        }

        protected override void UpdateMaterial()
        {
            int frameIndex = GetFrameIndex();

            // if (m_LastPlayingClip == m_PlayingClip && m_AnimUpdateData.frameIndex == frameIndex)
            // {
            //     res.Update(m_AnimUpdateData.timeDelta);
            //     return;
            // }
            //
            // m_LastPlayingClip = m_PlayingClip;
            m_AnimUpdateData.frameIndex = frameIndex;

            // float blend_crossFade = 1;
            int frameIndex_crossFade = -1;
            // GPUSkinningFrame frame_crossFade = null;
            if (GPUSkinningPlayerResources.IsCrossFadeBlending(m_LastPlayedClip, m_AnimUpdateData.crossFadeTime, m_AnimUpdateData.crossFadeProgress))
            {
                frameIndex_crossFade = GetCrossFadeFrameIndex();
                // frame_crossFade = lastPlayedClip.frames[frameIndex_crossFade];
                // blend_crossFade = res.CrossFadeBlendFactor(crossFadeProgress, crossFadeTime);
            }

            // GPUSkinningFrame frame = playingClip.frames[frameIndex];
            if (true)
            {
                GPUSkinningPlayerResources.UpdatePlayingData(
                    m_PlayingClip, frameIndex,
                    m_LastPlayedClip, GetCrossFadeFrameIndex(), m_AnimUpdateData.crossFadeTime, m_AnimUpdateData.crossFadeProgress,
                    out float4 GPUSkinning_FrameIndex_PixelSegmentation,
                    out float4 GPUSkinning_FrameIndex_PixelSegmentation_Blend_CrossFade
                );

                m_AnimUpdateData.GPUSkinning_FrameIndex_PixelSegmentation = GPUSkinning_FrameIndex_PixelSegmentation;
                m_AnimUpdateData.GPUSkinning_FrameIndex_PixelSegmentation_Blend_CrossFade = GPUSkinning_FrameIndex_PixelSegmentation_Blend_CrossFade;

                mpb.SetVector(GPUSkinningPlayerResources.ShaderIDs.GPUSkinning_FrameIndex_PixelSegmentation, m_AnimUpdateData.GPUSkinning_FrameIndex_PixelSegmentation);
                mpb.SetVector(GPUSkinningPlayerResources.ShaderIDs.GPUSkinning_FrameIndex_PixelSegmentation_Blend_CrossFade, m_AnimUpdateData.GPUSkinning_FrameIndex_PixelSegmentation_Blend_CrossFade);
                meshRenderer.SetPropertyBlock(mpb);
            }

            // UpdateEvents(m_PlayingClip, frameIndex);
            UpdateEvents(m_PlayingClip, frameIndex, m_LastPlayedClip == null || frameIndex_crossFade >= m_LastPlayedClip.totalFrameCount ? null : m_LastPlayedClip, frameIndex_crossFade);
        }
    }
}