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
        [SerializeField] private int m_AnimSettingID;
        [SerializeField, HideInInspector] private int defaultPlayingClipIndex = 0;
        [SerializeField] private Color color = Color.white;


        //这边添加需要渲染的信息
        protected GPUSkinningPlayer player = null;

        public GPUSkinningPlayer Player => player;

        private BatchRendererGroupContainer m_BRGContainer;
        private BatchRendererGroupContainer.RendererItem m_RenderItem;
        private int m_RendererID;

        private void OnEnable()
        {
            var animSetting = GPUSkinningSystem.S.AnimationGroup.GetGPUSkinningAnimationSetting(m_AnimSettingID);
            if (animSetting == null)
                return;

            var anim = animSetting.animation;

            player = new GPUSkinningPlayer(new GPUSkinningPlayerResources(anim));
            m_BRGContainer = GPUSkinningSystem.S.RegisterPlayer(player, m_AnimSettingID);
            // player.onAnimEvent += OnAnimEvent;
            if (anim != null && anim.clips != null && anim.clips.Length > 0)
            {
                player.Play(anim.clips[Mathf.Clamp(defaultPlayingClipIndex, 0, anim.clips.Length)].name);
            }

            UpdateRendererItem();
            m_RendererID = m_BRGContainer.AddRenderItem(ref m_RenderItem);
        }

        private void OnDisable()
        {
            GPUSkinningSystem.S.UnregisterPlayer(player, m_AnimSettingID);
            player = null;
        }

        void UpdateRendererItem()
        {
            m_RenderItem.position = transform.position;
            m_RenderItem.scale = transform.localScale.x;
            m_RenderItem.color = new float4(1, 1, 1, 1);
            m_RenderItem.gpuskinParam1 = player.animUpdateData.GPUSkinning_FrameIndex_PixelSegmentation;
            m_RenderItem.gpuskinParam2 = player.animUpdateData.GPUSkinning_FrameIndex_PixelSegmentation_Blend_CrossFade;
        }

        private void Update()
        {
            if (player == null)
                return;

            UpdateRendererItem();
            m_BRGContainer.UpdateRenderItem(m_RendererID, ref m_RenderItem);
        }
    }
}