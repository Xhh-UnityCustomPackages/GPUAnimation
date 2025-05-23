using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

namespace GameWish.Game
{
    // [ExecuteAlways]
    public class RendererRequire : MonoBehaviour
    {
        [SerializeField, HideInInspector] protected int m_AnimSettingID;
        [SerializeField, HideInInspector] protected int defaultPlayingClipIndex = 0;
        [SerializeField] private Color color = Color.white;

        [SerializeField, Range(0f, 5f), OnValueChanged("OnSpeedChanged")]
        protected float m_Speed = 1;

        //这边添加需要渲染的信息
        protected GPUSkinningPlayer player = null;

        public GPUSkinningPlayer Player => player;

        protected BatchRendererGroupContainer m_BRGContainer;
        protected BatchRendererGroupContainer.RendererItem m_RenderItem;

        protected GPUSkinningAnimation anim = null;

        protected virtual void Awake()
        {
            Init();
        }

        protected virtual void Init()
        {
            var animSetting = GPUSkinningSystem.S.AnimationGroup.GetGPUSkinningAnimationSetting(m_AnimSettingID);
            if (animSetting == null)
                return;

            anim = animSetting.animation;

            player = new GPUSkinningPlayer(anim);
            m_BRGContainer = GPUSkinningSystem.S.RegisterPlayer(player, m_AnimSettingID);
            // player.onAnimEvent += OnAnimEvent;
            if (anim != null && anim.clips != null && anim.clips.Length > 0)
            {
                player.Play(anim.clips[Mathf.Clamp(defaultPlayingClipIndex, 0, anim.clips.Length)].name);
            }

            UpdateRendererItem();
            m_BRGContainer.AddRenderItem(ref m_RenderItem);
        }

        private void OnDestroy()
        {
            GPUSkinningSystem.S.UnregisterPlayer(player, m_AnimSettingID);
            player = null;
        }


        protected virtual void UpdateRendererItem()
        {
            m_RenderItem.position = transform.position;
            m_RenderItem.scale = transform.localScale.x;
            m_RenderItem.rotation = transform.rotation;
            m_RenderItem.color = new float4(color.r, color.g, color.b, color.a);
            m_RenderItem.gpuskinParam1 = player.animUpdateData.GPUSkinning_FrameIndex_PixelSegmentation;
            m_RenderItem.gpuskinParam2 = player.animUpdateData.GPUSkinning_FrameIndex_PixelSegmentation_Blend_CrossFade;
        }

        protected virtual void Update()
        {
            if (player == null)
                return;

            UpdateRendererItem();
            m_BRGContainer.AddRenderItem(ref m_RenderItem);
        }

        void OnSpeedChanged()
        {
            player?.SetSpeed(m_Speed);
        }
    }
}