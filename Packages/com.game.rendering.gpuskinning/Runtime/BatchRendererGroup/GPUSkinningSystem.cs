using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace GameWish.Game
{
    [ExecuteAlways]
    public class GPUSkinningSystem : MonoBehaviour
    {
        private static GPUSkinningSystem _Instance;

        public static GPUSkinningSystem S
        {
            get
            {
                //先去场景里面找
                if (_Instance == null)
                {
                    _Instance = FindObjectOfType<GPUSkinningSystem>();
                }


                if (_Instance == null)
                {
                    //先去场景里面找

                    var go = new GameObject("[GPUSkinningSystem]");


                    _Instance = go.AddComponent<GPUSkinningSystem>();
                    _Instance.m_AnimGroupSetting = Resources.Load<GPUSkinningAnimationGroup>("GPUSkinningAnimationGroup");

#if UNITY_EDITOR
                    if (_Instance.m_AnimGroupSetting == null)
                    {
                        GPUSkinningAnimationGroup setting = ScriptableObject.CreateInstance<GPUSkinningAnimationGroup>();
                        AssetDatabase.CreateAsset(setting, "Assets/Resources/GPUSkinningAnimationGroup.asset");
                        AssetDatabase.Refresh();
                        _Instance.m_AnimGroupSetting = Resources.Load<GPUSkinningAnimationGroup>("GPUSkinningAnimationGroup");
                    }

                    if (!Application.isPlaying)
                    {
                        go.hideFlags = HideFlags.HideAndDontSave;
                    }
#endif

                    if (_Instance.m_AnimGroupSetting == null)
                    {
                        Debug.LogError("GPUSkinningAnimationGroup 不存在,Assets/Resources/GPUSkinningAnimationGroup.asset");
                    }

                    if (Application.isPlaying)
                        DontDestroyOnLoad(go);
                }

                return _Instance;
            }
        }

        static readonly ProfilerMarker s_GPUSetData = new("BRG.GPUSetData");

        private List<GPUSkinningPlayer> m_Players = new();

        private Dictionary<int, BatchRendererGroupContainer> m_BRGContainerMap = new();
        private GPUSkinningAnimationGroup m_AnimGroupSetting;

        public GPUSkinningAnimationGroup AnimationGroup => m_AnimGroupSetting;

        public BatchRendererGroupContainer RegisterPlayer(GPUSkinningPlayer player, int animID)
        {
            if (player == null || m_Players.Contains(player)) return null;
            m_Players.Add(player);

            if (!m_BRGContainerMap.ContainsKey(animID))
            {
                var batchRendererGroupContainer = new BatchRendererGroupContainer();
                var setting = m_AnimGroupSetting.GetGPUSkinningAnimationSetting(animID);
                batchRendererGroupContainer.Init(setting.animation.mesh, setting.animation.material, setting.maxCount, setting.shadowCastingMode);
                m_BRGContainerMap.Add(animID, batchRendererGroupContainer);
            }

            m_BRGContainerMap[animID].AddRef();
            return m_BRGContainerMap[animID];
        }

        public void UnregisterPlayer(GPUSkinningPlayer player, int animID)
        {
            if (player == null || !m_Players.Contains(player)) return;
            m_Players.Remove(player);

            if (m_BRGContainerMap.ContainsKey(animID))
            {
                m_BRGContainerMap[animID].RemoveRef();
                if (m_BRGContainerMap[animID].refCount <= 0)
                {
                    m_BRGContainerMap[animID].Shutdown();
                    m_BRGContainerMap.Remove(animID);
                }
            }
        }

        private void OnDestroy()
        {
            foreach (var brg in m_BRGContainerMap.Values)
                brg.Shutdown();
            m_BRGContainerMap.Clear();
            m_BRGContainerMap = null;

            m_Players.Clear();
            m_Players = null;
            _Instance = null;

            if (m_NativeAnimUpdateData.IsCreated)
                m_NativeAnimUpdateData.Dispose();
        }

        [BurstCompile]
        public int AddRenderItem(int animID, ref BatchRendererGroupContainer.RendererItem item)
        {
            //不判断了 减少0.2MS的开销
            // if (m_BRGContainerMap.ContainsKey(animID))
            return m_BRGContainerMap[animID].AddRenderItem(ref item);
        }

        private NativeArray<AnimUpdateData> m_NativeAnimUpdateData;
        private JobHandle jobHandle = new();


        [BurstCompile]
        private void Update()
        {
            Profiler.BeginSample("GPUSkinningSystem.Update");
            if (m_Players.Count <= 0)
                return;

            // 确保在创建新数组前释放旧数组
            if (m_NativeAnimUpdateData.IsCreated)
            {
                m_NativeAnimUpdateData.Dispose();
            }

            m_NativeAnimUpdateData = new NativeArray<AnimUpdateData>(m_Players.Count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            float deltaTime = Time.deltaTime;
            // 填充数据
            for (int i = 0; i < m_Players.Count; i++)
            {
                var player = m_Players[i];
                player.animUpdateData.timeDelta = deltaTime * player.speed;
                // player.Update(Time.deltaTime);
                player.UseJob = true;
                m_NativeAnimUpdateData[i] = player.animUpdateData;
            }

            var job = new AnimUpdateJob
            {
                animData = m_NativeAnimUpdateData
            };

            // 保存 JobHandle 以便在 LateUpdate 中使用
            jobHandle = job.Schedule(m_Players.Count, 4);
            Profiler.EndSample();
        }


        private void LateUpdate()
        {
            Profiler.BeginSample("GPUSkinningSystem.LateUpdate");
            jobHandle.Complete();


            //并行写入systembuffer

            // 将更新后的数据复制回到Players
            if (m_Players.Count > 0 && m_NativeAnimUpdateData.IsCreated)
            {
                for (int i = 0; i < m_NativeAnimUpdateData.Length; i++)
                {
                    if (i >= m_Players.Count) break;
                    var player = m_Players[i];
                    player.animUpdateData = m_NativeAnimUpdateData[i];
                }

                // 释放Native数组
                m_NativeAnimUpdateData.Dispose();
            }

            foreach (var brg in m_BRGContainerMap.Values) brg.DoUpdateRenderItemJob();
            s_GPUSetData.Begin();
            foreach (var brg in m_BRGContainerMap.Values) brg.UploadGpuData();
            s_GPUSetData.End();

            // foreach (var brg in m_BRGContainerMap.Values) brg.ClearInstnce();


            foreach (var player in m_Players)
            {
                player.CheckEndEvent();
            }

            Profiler.EndSample();
        }
    }

    // 动画更新数据结构
    public struct AnimUpdateData
    {
        public bool isPlaying;
        public float time;
        public float timeDelta;
        public float lastPlayedTime;
        public float crossFadeProgress;
        public float crossFadeTime;
        public int frameIndex;
        public int crossFadeFrameIndex;
        public bool isEnd;

        //currentPlaying Info
        public float clipLength;
        public float frameRate;
        public GPUSkinningWrapMode wrapMode;
        public int playingClipPixelSegmentation;

        //lastPlaying Info
        public GPUSkinningWrapMode lastWrapMode;
        public float lastClipLength;
        public float lastFrameRate;
        public int lastPlayingClipPixelSegmentation;

        //out
        public float4 GPUSkinning_FrameIndex_PixelSegmentation;
        public float4 GPUSkinning_FrameIndex_PixelSegmentation_Blend_CrossFade;


        public bool IsNotSetPlaying()
        {
            return clipLength == 0;
        }
    }

    [BurstCompile]
    public struct AnimUpdateJob : IJobParallelFor
    {
        public NativeArray<AnimUpdateData> animData;

        public void Execute(int index)
        {
            AnimUpdateData data = animData[index];

            if (!data.isPlaying) return;
            if (data.IsNotSetPlaying()) return;


            if (data.wrapMode == GPUSkinningWrapMode.Loop)
            {
                data.time += data.timeDelta;
                if (data.time > data.clipLength)
                {
                    data.time = 0;
                    // data.isEnd = true;
                    // m_EndAction?.Invoke();
                }
            }
            else // if (m_PlayingClip.wrapMode == GPUSkinningWrapMode.Once)
            {
                if (data.time > data.clipLength)
                {
                    data.time = data.clipLength;
                }
                else
                {
                    data.time += data.timeDelta;
                    if (data.time > data.clipLength)
                    {
                        data.time = data.clipLength;
                        data.isEnd = true;
                        // m_EndAction?.Invoke();
                    }
                }
            }

            data.crossFadeProgress += data.timeDelta;
            data.lastPlayedTime += data.timeDelta;

            //TODO 如果距离太远的话或者不在相机范围内 不需要更新动画了
            if (true)
            {
                int frameIndex = GetFrameIndex(data);
                data.frameIndex = frameIndex;
                data.crossFadeFrameIndex = -1;

                int frameIndex_crossFade = GetCrossFadeFrameIndex(data);
                if (IsCrossFadeBlending(data.lastClipLength, data.crossFadeTime, data.crossFadeProgress))
                {
                    data.crossFadeFrameIndex = frameIndex_crossFade;
                    // 这里可以缓存计算结果，避免在UpdateEvents中再次计算
                }

                UpdatePlayingData(ref data,
                    data.playingClipPixelSegmentation, frameIndex,
                    data.lastClipLength, data.lastPlayingClipPixelSegmentation, frameIndex_crossFade, data.crossFadeTime, data.crossFadeProgress
                );
            }

            animData[index] = data;
        }

        public void UpdatePlayingData(ref AnimUpdateData data, int playingClipPixelSegmentation, int frameIndex, float lastPlayedClip, int lastPlayedClipPixelSegmentation, int frameIndex_crossFade,
            float crossFadeTime,
            float crossFadeProgress)
        {
            data.GPUSkinning_FrameIndex_PixelSegmentation = new float4(frameIndex, playingClipPixelSegmentation, 0, 0);

            if (IsCrossFadeBlending(lastPlayedClip, crossFadeTime, crossFadeProgress))
            {
                data.GPUSkinning_FrameIndex_PixelSegmentation_Blend_CrossFade =
                    new float4(frameIndex_crossFade, lastPlayedClipPixelSegmentation, CrossFadeBlendFactor(crossFadeProgress, crossFadeTime), 0);
            }
        }

        public float CrossFadeBlendFactor(float crossFadeProgress, float crossFadeTime)
        {
            return Mathf.Clamp01(crossFadeProgress / crossFadeTime);
        }

        public bool IsCrossFadeBlending(float lastPlayedClip, float crossFadeTime, float crossFadeProgress)
        {
            return lastPlayedClip > 0 && crossFadeTime > 0 && crossFadeProgress <= crossFadeTime;
        }

        int GetFrameIndex(AnimUpdateData data)
        {
            float time = data.time;
            if (data.clipLength == time)
            {
                return GetTheLastFrameIndex_WrapMode_Once(data.clipLength, data.frameRate);
            }
            else
            {
                return GetFrameIndex_WrapMode_Loop(data.clipLength, data.frameRate, time);
            }
        }


        int GetCrossFadeFrameIndex(AnimUpdateData data)
        {
            if (data.lastClipLength == 0)
            {
                return 0;
            }

            if (data.lastWrapMode == GPUSkinningWrapMode.Once)
            {
                if (data.lastPlayedTime >= data.lastClipLength)
                {
                    return GetTheLastFrameIndex_WrapMode_Once(data.lastClipLength, data.lastFrameRate);
                }
                else
                {
                    return GetFrameIndex_WrapMode_Loop(data.lastClipLength, data.lastFrameRate, data.lastPlayedTime);
                }
            }
            else //if (lastPlayedClip.wrapMode == GPUSkinningWrapMode.Loop)
            {
                return GetFrameIndex_WrapMode_Loop(data.lastClipLength, data.lastFrameRate, data.lastPlayedTime);
            }
        }

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
    }
}