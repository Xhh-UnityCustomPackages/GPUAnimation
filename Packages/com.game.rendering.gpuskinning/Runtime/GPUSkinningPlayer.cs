using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace GameWish.Game
{
    public class GPUSkinningPlayer
    {
        public delegate void OnAnimEvent(GPUSkinningPlayer player, int eventId);

        private GameObject go = null;
        private Transform transform = null;
        private MeshRenderer meshRenderer = null;
        private MaterialPropertyBlock mpb = null;
        private GPUSkinningPlayerResources res = null;


        private bool m_IsPlaying = false;
        private float time = 0;
        private float m_Speed = 1;

        private System.Action m_EndAction = null;
        // private float timeDiff = 0;




        // private GPUSkinningClip lastPlayedClip = null;
        private int m_LastPlayingFrameIndex = -1;
        // private float lastPlayedTime = 0;
        // private float crossFadeProgress = 0;
        // private float crossFadeTime = -1;
        // private int rootMotionFrameIndex = -1;
        private GPUSkinningClip m_LastPlayingClip = null;
        private GPUSkinningClip m_PlayingClip = null;


        public event OnAnimEvent onAnimEvent;
        public bool IsPlaying => m_IsPlaying;
        public GPUSkinningWrapMode WrapMode => m_PlayingClip == null ? GPUSkinningWrapMode.Once : m_PlayingClip.wrapMode;
        public GPUSkinningClip playingClip => m_PlayingClip;

        public GPUSkinningPlayer(GameObject target, MeshRenderer meshRenderer, GPUSkinningPlayerResources resources)
        {
            go = target;
            transform = target.transform;
            this.res = resources;
            this.meshRenderer = meshRenderer;

            res.InitMaterial(meshRenderer.sharedMaterial);
            mpb = new MaterialPropertyBlock();
        }

        public void Update(float timeDelta)
        {
            timeDelta *= m_Speed;
            Update_Internal(timeDelta);
        }


        public bool IsTimeAtTheEndOfLoop
        {
            get
            {
                if (m_PlayingClip == null)
                {
                    return false;
                }
                else
                {
                    return GetFrameIndex() == ((int)(m_PlayingClip.length * m_PlayingClip.frameRate) - 1);
                }
            }
        }

        private float GetCurrentTime()
        {
            float time = 0;
            if (WrapMode == GPUSkinningWrapMode.Once)
            {
                time = this.time;
            }
            else if (WrapMode == GPUSkinningWrapMode.Loop)
            {
                time = res.Time;//+ (playingClip.individualDifferenceEnabled ? this.timeDiff : 0);
            }
            else
            {
                throw new System.NotImplementedException();
            }
            return time;
        }

        private int GetFrameIndex()
        {
            float time = GetCurrentTime();
            if (m_PlayingClip.length == time)
            {
                return GetTheLastFrameIndex_WrapMode_Once(m_PlayingClip);
            }
            else
            {
                return GetFrameIndex_WrapMode_Loop(m_PlayingClip, time);
            }
        }

        private int GetTheLastFrameIndex_WrapMode_Once(GPUSkinningClip clip)
        {
            return (int)(clip.length * clip.frameRate) - 1;
        }

        private int GetFrameIndex_WrapMode_Loop(GPUSkinningClip clip, float time)
        {
            return (int)(time * clip.frameRate) % (int)(clip.length * clip.frameRate);
        }

        private void Update_Internal(float timeDelta)
        {
            if (!m_IsPlaying || m_PlayingClip == null)
            {
                return;
            }

            if (m_PlayingClip.wrapMode == GPUSkinningWrapMode.Loop)
            {
                UpdateMaterial(timeDelta);
                time += timeDelta;
                if (time > m_PlayingClip.length)
                {
                    time = 0;
                    m_EndAction?.Invoke();
                }
            }
            else if (m_PlayingClip.wrapMode == GPUSkinningWrapMode.Once)
            {
                if (time >= m_PlayingClip.length)
                {
                    time = m_PlayingClip.length;
                    UpdateMaterial(timeDelta);
                }
                else
                {
                    UpdateMaterial(timeDelta);
                    time += timeDelta;
                    if (time > m_PlayingClip.length)
                    {
                        time = m_PlayingClip.length;
                        m_EndAction?.Invoke();
                    }
                }
            }
            else
            {
                throw new System.NotImplementedException();
            }

            // crossFadeProgress += timeDelta;
            // lastPlayedTime += timeDelta;
        }

        private void UpdateMaterial(float deltaTime)
        {
            int frameIndex = GetFrameIndex();

            if (m_LastPlayingClip == m_PlayingClip && m_LastPlayingFrameIndex == frameIndex)
            {
                res.Update(deltaTime);
                return;
            }

            m_LastPlayingClip = m_PlayingClip;
            m_LastPlayingFrameIndex = frameIndex;

            // float blend_crossFade = 1;
            // int frameIndex_crossFade = -1;
            // GPUSkinningFrame frame_crossFade = null;
            // if (res.IsCrossFadeBlending(lastPlayedClip, crossFadeTime, crossFadeProgress))
            // {
            //     frameIndex_crossFade = GetCrossFadeFrameIndex();
            //     frame_crossFade = lastPlayedClip.frames[frameIndex_crossFade];
            //     blend_crossFade = res.CrossFadeBlendFactor(crossFadeProgress, crossFadeTime);
            // }

            // GPUSkinningFrame frame = playingClip.frames[frameIndex];
            if (true)
            {
                // Debug.LogError($"frameIndex:{frameIndex}");
                res.Update(deltaTime);
                res.UpdatePlayingData(mpb, m_PlayingClip, frameIndex);
                meshRenderer.SetPropertyBlock(mpb);
            }


            UpdateEvents(m_PlayingClip, frameIndex);
        }


        private void UpdateEvents(GPUSkinningClip clip, int frameIndex)
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
            m_IsPlaying = false;
        }

        public void Resume()
        {
            if (m_PlayingClip != null)
            {
                m_IsPlaying = true;
            }
        }

        public void SetSpeed(float speed)
        {
            m_Speed = speed;
        }


        public void Play(string clipName, System.Action endAction = null)
        {
            if (!IsAnimSupported(clipName))
            {
                return;
            }

            GPUSkinningClip[] clips = res.anim.clips;
            int numClips = clips == null ? 0 : clips.Length;
            for (int i = 0; i < numClips; ++i)
            {
                if (clips[i].name == clipName)
                {
                    if (m_PlayingClip != clips[i] ||
                        (m_PlayingClip != null && m_PlayingClip.wrapMode == GPUSkinningWrapMode.Once && IsTimeAtTheEndOfLoop) ||
                        (m_PlayingClip != null && !m_IsPlaying))
                    {
                        m_EndAction = endAction;
                        SetNewPlayingClip(clips[i]);
                    }
                    return;
                }
            }
        }

        public bool IsAnimSupported(string clipName)
        {
            GPUSkinningClip[] clips = res.anim.clips;
            int numClips = clips == null ? 0 : clips.Length;
            for (int i = 0; i < numClips; ++i)
            {
                if (clips[i].name == clipName)
                {
                    return true;
                }
            }
            return false;
        }

        public void CrossFade(string clipName, float fadeLength)
        {

        }

        private void SetNewPlayingClip(GPUSkinningClip clip)
        {
            // lastPlayedClip = playingClip;
            // lastPlayedTime = GetCurrentTime();

            m_IsPlaying = true;
            m_PlayingClip = clip;
            // rootMotionFrameIndex = -1;
            time = 0;
            // timeDiff = Random.Range(0, playingClip.length);
        }

    }
}
