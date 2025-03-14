using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameWish.Game
{
    public class GPUSkinningPlayerWithRenderer : GPUSkinningPlayer
    {
        private MeshRenderer meshRenderer = null;
        private MaterialPropertyBlock mpb = null;

        public MaterialPropertyBlock materialPropertyBlock => mpb;
        public MeshRenderer MeshRenderer => meshRenderer;


        public GPUSkinningPlayerWithRenderer(MeshRenderer meshRenderer, GPUSkinningPlayerResources resources) : base(resources)
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
            if (res.IsCrossFadeBlending(m_LastPlayedClip, m_AnimUpdateData.crossFadeTime, m_AnimUpdateData.crossFadeProgress))
            {
                frameIndex_crossFade = GetCrossFadeFrameIndex();
                // frame_crossFade = lastPlayedClip.frames[frameIndex_crossFade];
                // blend_crossFade = res.CrossFadeBlendFactor(crossFadeProgress, crossFadeTime);
            }

            // GPUSkinningFrame frame = playingClip.frames[frameIndex];
            if (true)
            {
                res.UpdatePlayingData(
                    m_PlayingClip, frameIndex,
                    m_LastPlayedClip, GetCrossFadeFrameIndex(), m_AnimUpdateData.crossFadeTime, m_AnimUpdateData.crossFadeProgress
                );
                mpb.SetVector(GPUSkinningPlayerResources.ShaderIDs.GPUSkinning_FrameIndex_PixelSegmentation, res.GPUSkinning_FrameIndex_PixelSegmentation);
                mpb.SetVector(GPUSkinningPlayerResources.ShaderIDs.GPUSkinning_FrameIndex_PixelSegmentation_Blend_CrossFade, res.GPUSkinning_FrameIndex_PixelSegmentation_Blend_CrossFade);
                meshRenderer.SetPropertyBlock(mpb);
            }

            // UpdateEvents(m_PlayingClip, frameIndex);
            UpdateEvents(m_PlayingClip, frameIndex, m_LastPlayedClip == null || frameIndex_crossFade >= m_LastPlayedClip.totalFrameCount ? null : m_LastPlayedClip, frameIndex_crossFade);
        }
    }
}