using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;


namespace GameWish.Game
{
    public class GPUSkinningPlayer
    {
        public delegate void OnAnimEvent(GPUSkinningPlayer player, int eventId);

        protected GPUSkinningAnimation m_Anim = null;

        public float speed { get; set; } = 1;

        private System.Action m_EndAction = null;

        protected GPUSkinningClip m_LastPlayedClip = null;
        protected GPUSkinningClip m_PlayingClip = null;


        public event OnAnimEvent onAnimEvent;

        public bool IsPlaying => m_AnimUpdateData.isPlaying;


        protected AnimUpdateData m_AnimUpdateData;
        public ref AnimUpdateData animUpdateData => ref m_AnimUpdateData;
        public bool UseJob { get; set; } = false;
        public bool UseEvent { get; set; } = false;

        public GPUSkinningPlayer(GPUSkinningAnimation anim)
        {
            m_Anim = anim;
            m_AnimUpdateData.wrapMode = GPUSkinningWrapMode.Once;
        }

        public static BatchRendererGroupContainer.RendererItem GetRendererItem(float3 position, float scale, Vector4 color, AnimUpdateData animUpdateData)
        {
            return new BatchRendererGroupContainer.RendererItem()
            {
                position = position,
                scale = scale,
                color = color,
                gpuskinParam1 = animUpdateData.GPUSkinning_FrameIndex_PixelSegmentation,
                gpuskinParam2 = animUpdateData.GPUSkinning_FrameIndex_PixelSegmentation_Blend_CrossFade,
            };
        }


        [BurstCompile]
        public void Update(float timeDelta)
        {
            if (!UseJob)
            {
                m_AnimUpdateData.timeDelta = timeDelta * speed;
                Update_Internal();
                UpdateMaterial();
            }


            if (UseEvent)
                UpdateEvents(m_PlayingClip, m_AnimUpdateData.frameIndex, m_LastPlayedClip == null || m_AnimUpdateData.crossFadeFrameIndex >= 0 ? m_LastPlayedClip : null,
                    m_AnimUpdateData.crossFadeFrameIndex);
        }

        public void CheckEndEvent()
        {
            if (m_AnimUpdateData.isEnd)
            {
                m_AnimUpdateData.isEnd = false;
                m_EndAction?.Invoke();
            }
        }


        public bool IsTimeAtTheEndOfLoop
        {
            get
            {
                if (m_AnimUpdateData.IsNotSetPlaying())
                {
                    return false;
                }
                else
                {
                    return GetFrameIndex() == ((int)(m_AnimUpdateData.clipLength * m_AnimUpdateData.frameRate) - 1);
                }
            }
        }

        private float GetCurrentTime()
        {
            return m_AnimUpdateData.time;
        }

        protected int GetFrameIndex()
        {
            float time = GetCurrentTime();
            if (m_AnimUpdateData.clipLength == time)
            {
                return GetTheLastFrameIndex_WrapMode_Once(m_AnimUpdateData.clipLength, m_AnimUpdateData.frameRate);
            }
            else
            {
                return GetFrameIndex_WrapMode_Loop(m_AnimUpdateData.clipLength, m_AnimUpdateData.frameRate, time);
            }
        }

        protected int GetCrossFadeFrameIndex()
        {
            if (m_AnimUpdateData.lastClipLength == 0)
            {
                return 0;
            }

            if (m_AnimUpdateData.lastWrapMode == GPUSkinningWrapMode.Once)
            {
                if (m_AnimUpdateData.lastPlayedTime >= m_AnimUpdateData.lastClipLength)
                {
                    return GetTheLastFrameIndex_WrapMode_Once(m_AnimUpdateData.lastClipLength, m_AnimUpdateData.lastFrameRate);
                }
                else
                {
                    return GetFrameIndex_WrapMode_Loop(m_AnimUpdateData.lastClipLength, m_AnimUpdateData.lastFrameRate, m_AnimUpdateData.lastPlayedTime);
                }
            }
            else //if (lastPlayedClip.wrapMode == GPUSkinningWrapMode.Loop)
            {
                return GetFrameIndex_WrapMode_Loop(m_AnimUpdateData.lastClipLength, m_AnimUpdateData.lastFrameRate, m_AnimUpdateData.lastPlayedTime);
            }
        }

        // private int GetTheLastFrameIndex_WrapMode_Once(GPUSkinningClip clip)
        // {
        //     return GetTheLastFrameIndex_WrapMode_Once(clip.length, clip.frameRate);
        // }

        // private int GetFrameIndex_WrapMode_Loop(GPUSkinningClip clip, float time)
        // {
        //     return GetFrameIndex_WrapMode_Loop(clip.length, clip.frameRate, time);
        // }

        public static int GetTheLastFrameIndex_WrapMode_Once(float clipLength, float frameRate)
        {
            return (int)(clipLength * frameRate) - 1;
        }

        public static int GetFrameIndex_WrapMode_Loop(float clipLength, float frameRate, float time)
        {
            // 预先计算总帧数，避免每次都计算
            int totalFrames = (int)(clipLength * frameRate);
            return (int)(time * frameRate) % totalFrames;
        }

        [BurstCompile]
        private void Update_Internal()
        {
            if (!m_AnimUpdateData.isPlaying || m_AnimUpdateData.IsNotSetPlaying())
            {
                return;
            }

            if (m_AnimUpdateData.wrapMode == GPUSkinningWrapMode.Loop)
            {
                m_AnimUpdateData.time += m_AnimUpdateData.timeDelta;
                if (m_AnimUpdateData.time > m_AnimUpdateData.clipLength)
                {
                    m_AnimUpdateData.time = 0;
                    // m_AnimUpdateData.isEnd = true;

                    // m_EndAction?.Invoke();
                }
            }
            else // if (m_PlayingClip.wrapMode == GPUSkinningWrapMode.Once)
            {
                if (m_AnimUpdateData.time > m_AnimUpdateData.clipLength)
                {
                    m_AnimUpdateData.time = m_AnimUpdateData.clipLength;
                }
                else
                {
                    m_AnimUpdateData.time += m_AnimUpdateData.timeDelta;
                    if (m_AnimUpdateData.time > m_AnimUpdateData.clipLength)
                    {
                        m_AnimUpdateData.time = m_AnimUpdateData.clipLength;
                        m_AnimUpdateData.isEnd = true;
                        // m_EndAction?.Invoke();
                    }
                }
            }

            m_AnimUpdateData.crossFadeProgress += m_AnimUpdateData.timeDelta;
            m_AnimUpdateData.lastPlayedTime += m_AnimUpdateData.timeDelta;
        }

        protected virtual void UpdateMaterial()
        {
            int frameIndex = GetFrameIndex();


            //TODO ???
            // if (m_LastPlayingClip == m_PlayingClip && m_AnimUpdateData.frameIndex == frameIndex)
            // {
            //     res.Update(m_AnimUpdateData.timeDelta);
            //     return;
            // }
            //
            // m_LastPlayingClip = m_PlayingClip;
            m_AnimUpdateData.frameIndex = frameIndex;

            // float blend_crossFade = 1;
            // int frameIndex_crossFade = -1;
            m_AnimUpdateData.crossFadeFrameIndex = -1;
            // GPUSkinningFrame frame_crossFade = null;
            int frameIndex_crossFade = GetCrossFadeFrameIndex();
            if (GPUSkinningPlayerResources.IsCrossFadeBlending(m_LastPlayedClip, m_AnimUpdateData.crossFadeTime, m_AnimUpdateData.crossFadeProgress))
            {
                m_AnimUpdateData.crossFadeFrameIndex = frameIndex_crossFade;
                // 这里可以缓存计算结果，避免在UpdateEvents中再次计算
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
            }

            // 优化：只在需要时计算crossFadeFrameIndex
            // UpdateEvents(m_PlayingClip, m_AnimUpdateData.frameIndex, lastPlayedClip == null || m_AnimUpdateData.crossFadeFrameIndex >= 0 ? lastPlayedClip : null, m_AnimUpdateData.crossFadeFrameIndex);
        }


        protected void UpdateEvents(GPUSkinningClip playingClip, int playingFrameIndex, GPUSkinningClip corssFadeClip, int crossFadeFrameIndex)
        {
            UpdateClipEvent(playingClip, playingFrameIndex);
            UpdateClipEvent(corssFadeClip, crossFadeFrameIndex);
        }

        private void UpdateClipEvent(GPUSkinningClip clip, int frameIndex)
        {
            if (clip == null || clip.events == null || clip.events.Length == 0)
            {
                return;
            }

            GPUSkinningAnimEvent[] events = clip.events;
            int numEvents = events.Length;
            for (int i = 0; i < numEvents; ++i)
            {
                if (events[i].frameIndex == frameIndex && onAnimEvent != null)
                {
                    onAnimEvent(this, events[i].eventId);
                    break;
                }
            }
        }


        public void Stop()
        {
            m_AnimUpdateData.isPlaying = false;
        }

        public void Resume()
        {
            if (m_PlayingClip != null)
            {
                m_AnimUpdateData.isPlaying = true;
            }
        }

        public void SetSpeed(float speed)
        {
            this.speed = speed;
        }


        public void Play(string clipName, System.Action endAction = null)
        {
            if (FindClipByName(clipName) == null)
            {
                return;
            }

            GPUSkinningClip clip = FindClipByName(clipName);
            if (clip != null)
            {
                if (m_PlayingClip != clip ||
                    (m_PlayingClip != null && m_AnimUpdateData.wrapMode == GPUSkinningWrapMode.Once && IsTimeAtTheEndOfLoop) ||
                    (m_PlayingClip != null && !m_AnimUpdateData.isPlaying))
                {
                    m_EndAction = endAction;
                    SetNewPlayingClip(clip);
                }
            }
        }

        public void CrossFade(string clipName, float fadeLength, System.Action endAction = null)
        {
            if (m_AnimUpdateData.IsNotSetPlaying())
            {
                Play(clipName, endAction);
            }
            else
            {
                GPUSkinningClip clip = FindClipByName(clipName);
                if (clip != null)
                {
                    if (m_PlayingClip != clip)
                    {
                        m_AnimUpdateData.crossFadeProgress = 0;
                        m_AnimUpdateData.crossFadeTime = fadeLength;
                        m_EndAction = endAction;
                        SetNewPlayingClip(clip);
                        return;
                    }

                    if ((m_PlayingClip != null && m_AnimUpdateData.wrapMode == GPUSkinningWrapMode.Once && IsTimeAtTheEndOfLoop) ||
                        (m_PlayingClip != null && !m_AnimUpdateData.isPlaying))
                    {
                        SetNewPlayingClip(clip);
                    }
                }
            }
        }

        public GPUSkinningClip FindClipByName(string clipName)
        {
            GPUSkinningClip[] clips = m_Anim.clips;
            int numClips = clips == null ? 0 : clips.Length;
            for (int i = 0; i < numClips; ++i)
            {
                if (clips[i].name == clipName)
                {
                    return clips[i];
                }
            }

            return null;
        }

        private void SetNewPlayingClip(GPUSkinningClip clip)
        {
            m_LastPlayedClip = m_PlayingClip;
            m_PlayingClip = clip;

            if (m_LastPlayedClip != null)
            {
                m_AnimUpdateData.lastClipLength = m_LastPlayedClip.length;
                m_AnimUpdateData.lastFrameRate = m_LastPlayedClip.frameRate;
                m_AnimUpdateData.lastWrapMode = m_LastPlayedClip.wrapMode;
                m_AnimUpdateData.lastPlayingClipPixelSegmentation = m_LastPlayedClip.pixelSegmentation;
            }
            else
            {
                m_AnimUpdateData.lastClipLength = 0;
                m_AnimUpdateData.lastFrameRate = 0;
                m_AnimUpdateData.lastWrapMode = GPUSkinningWrapMode.Once;
                m_AnimUpdateData.lastPlayingClipPixelSegmentation = 0;
            }

            m_AnimUpdateData.lastPlayedTime = GetCurrentTime();
            m_AnimUpdateData.isPlaying = true;
            m_AnimUpdateData.time = 0;
            m_AnimUpdateData.wrapMode = clip.wrapMode;
            m_AnimUpdateData.clipLength = clip.length;
            m_AnimUpdateData.frameRate = clip.frameRate;
            m_AnimUpdateData.playingClipPixelSegmentation = clip.pixelSegmentation;
        }
    }
}