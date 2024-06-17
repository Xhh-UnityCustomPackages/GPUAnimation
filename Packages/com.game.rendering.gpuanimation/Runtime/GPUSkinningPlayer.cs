using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.GPUSkinning
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
        private float timeDiff = 0;



        public bool IsPlaying => m_IsPlaying;
        public GPUSkinningWrapMode WrapMode => playingClip == null ? GPUSkinningWrapMode.Once : playingClip.wrapMode;

        // private GPUSkinningClip lastPlayedClip = null;
        private int lastPlayingFrameIndex = -1;
        // private float lastPlayedTime = 0;
        // private float crossFadeProgress = 0;
        // private float crossFadeTime = -1;
        private int rootMotionFrameIndex = -1;
        private GPUSkinningClip lastPlayingClip = null;
        private GPUSkinningClip playingClip = null;


        public event OnAnimEvent onAnimEvent;


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
            Update_Internal(timeDelta);
        }


        public bool IsTimeAtTheEndOfLoop
        {
            get
            {
                if (playingClip == null)
                {
                    return false;
                }
                else
                {
                    return GetFrameIndex() == ((int)(playingClip.length * playingClip.frameRate) - 1);
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
            if (playingClip.length == time)
            {
                return GetTheLastFrameIndex_WrapMode_Once(playingClip);
            }
            else
            {
                return GetFrameIndex_WrapMode_Loop(playingClip, time);
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
            if (!m_IsPlaying || playingClip == null)
            {
                return;
            }

            if (playingClip.wrapMode == GPUSkinningWrapMode.Loop)
            {
                UpdateMaterial(timeDelta);
            }
            else if (playingClip.wrapMode == GPUSkinningWrapMode.Once)
            {
                if (time >= playingClip.length)
                {
                    time = playingClip.length;
                    UpdateMaterial(timeDelta);
                }
                else
                {
                    UpdateMaterial(timeDelta);
                    time += timeDelta;
                    if (time > playingClip.length)
                    {
                        time = playingClip.length;
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
            if (lastPlayingClip == playingClip && lastPlayingFrameIndex == frameIndex)
            {
                res.Update(deltaTime);
                return;
            }

            lastPlayingClip = playingClip;
            lastPlayingFrameIndex = frameIndex;

            // float blend_crossFade = 1;
            // int frameIndex_crossFade = -1;
            // GPUSkinningFrame frame_crossFade = null;
            // if (res.IsCrossFadeBlending(lastPlayedClip, crossFadeTime, crossFadeProgress))
            // {
            //     frameIndex_crossFade = GetCrossFadeFrameIndex();
            //     frame_crossFade = lastPlayedClip.frames[frameIndex_crossFade];
            //     blend_crossFade = res.CrossFadeBlendFactor(crossFadeProgress, crossFadeTime);
            // }

            GPUSkinningFrame frame = playingClip.frames[frameIndex];
            if (true)
            {
                // Debug.LogError($"frameIndex:{frameIndex}");
                res.Update(deltaTime);
                res.UpdatePlayingData(mpb, playingClip, frameIndex, frame);
                meshRenderer.SetPropertyBlock(mpb);
            }


            UpdateEvents(playingClip, frameIndex);
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
            if (playingClip != null)
            {
                m_IsPlaying = true;
            }
        }


        public void Play(string clipName)
        {
            GPUSkinningClip[] clips = res.anim.clips;
            int numClips = clips == null ? 0 : clips.Length;
            for (int i = 0; i < numClips; ++i)
            {
                if (clips[i].name == clipName)
                {
                    if (playingClip != clips[i] ||
                        (playingClip != null && playingClip.wrapMode == GPUSkinningWrapMode.Once && IsTimeAtTheEndOfLoop) ||
                        (playingClip != null && !m_IsPlaying))
                    {
                        SetNewPlayingClip(clips[i]);
                    }
                    return;
                }
            }
        }

        public void CrossFade(string clipName, float fadeLength)
        {

        }

        private void SetNewPlayingClip(GPUSkinningClip clip)
        {
            // lastPlayedClip = playingClip;
            // lastPlayedTime = GetCurrentTime();

            m_IsPlaying = true;
            playingClip = clip;
            rootMotionFrameIndex = -1;
            time = 0;
            timeDiff = Random.Range(0, playingClip.length);
        }

    }
}
