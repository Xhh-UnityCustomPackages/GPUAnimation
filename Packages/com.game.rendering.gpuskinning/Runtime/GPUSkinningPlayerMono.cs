using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GameWish.Game
{
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    [ExecuteAlways]
    [Obsolete]//废弃的方法 不在支持了 现在也不走instance了 直接使用RednerRequire
    public class GPUSkinningPlayerMono : MonoBehaviour
    {
        [SerializeField, HideInInspector] MeshRenderer m_MeshRenderer;
        [SerializeField, HideInInspector] MeshFilter m_MeshFilter;
        [SerializeField] protected GPUSkinningAnimation anim = null;
        [SerializeField, HideInInspector] private int defaultPlayingClipIndex = 0;

        [SerializeField, Range(0f, 5f), OnValueChanged("OnSpeedChanged")]
        protected float m_Speed = 1;

        protected GPUSkinningPlayerWithRenderer player = null;

        public GPUSkinningPlayer Player => player;
        public GPUSkinningPlayer.OnAnimEvent onAnimEvent;


        private void Awake()
        {
            Init();
        }

        protected virtual void Init()
        {
            if (m_MeshRenderer == null || m_MeshFilter == null)
                return;

            GPUSkinningPlayerResources res = new GPUSkinningPlayerResources(anim);

            player = new GPUSkinningPlayerWithRenderer(m_MeshRenderer, res);
            player.onAnimEvent += OnAnimEvent;
            if (anim != null && anim.clips != null && anim.clips.Length > 0)
            {
                player.Play(anim.clips[Mathf.Clamp(defaultPlayingClipIndex, 0, anim.clips.Length)].name);
            }
        }

        protected virtual void Update()
        {
            if (player == null)
                return;

            player.Update(Time.deltaTime);
        }


        private void OnAnimEvent(GPUSkinningPlayer player, int eventId)
        {
            // Debug.LogError($"OnAnimEvent:{eventId}");
        }


        void OnSpeedChanged()
        {
            player?.SetSpeed(m_Speed);
        }


#if UNITY_EDITOR
        private void OnValidate()
        {
            if (anim == null)
                return;

            if (m_MeshRenderer == null) m_MeshRenderer = GetComponent<MeshRenderer>();
            if (m_MeshFilter == null) m_MeshFilter = GetComponent<MeshFilter>();

            if (anim.material != null) m_MeshRenderer.sharedMaterial = anim.material;
            if (anim.mesh != null) m_MeshFilter.sharedMesh = anim.mesh;
        }
#endif
    }
}