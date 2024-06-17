using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameWish.Game
{
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    public class GPUSkinningPlayerMono : MonoBehaviour
    {
        [SerializeField, HideInInspector] MeshRenderer m_MeshRenderer;
        [SerializeField, HideInInspector] MeshFilter m_MeshFilter;
        [SerializeField] GPUSkinningAnimation anim = null;
        [SerializeField, HideInInspector] private int defaultPlayingClipIndex = 0;

        private GPUSkinningPlayer player = null;

        public GPUSkinningPlayer Player => player;
        public GPUSkinningPlayer.OnAnimEvent onAnimEvent;


        private void Start()
        {
            Init();
        }

        void Init()
        {
            if (m_MeshRenderer == null || m_MeshFilter == null)
                return;

            GPUSkinningPlayerResources res = new();
            res.anim = anim;
            res.texture = GPUSkinningUtil.CreateTexture2D(anim.texture, anim);
            res.texture.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;

            player = new GPUSkinningPlayer(gameObject, m_MeshRenderer, res);
            player.onAnimEvent += OnAnimEvent;
            if (anim != null && anim.clips != null && anim.clips.Length > 0)
            {
                player.Play(anim.clips[Mathf.Clamp(defaultPlayingClipIndex, 0, anim.clips.Length)].name);
            }
        }

        private void Update()
        {
            if (player == null)
                return;

            player.Update(Time.deltaTime);
        }


        private void OnDestroy()
        {

        }

        private void OnAnimEvent(GPUSkinningPlayer player, int eventId)
        {
            // Debug.LogError($"OnAnimEvent:{eventId}");
        }


#if UNITY_EDITOR
        private void OnValidate()
        {
            if (anim == null)
                return;

            if (m_MeshRenderer == null) m_MeshRenderer = GetComponent<MeshRenderer>();
            if (m_MeshFilter == null) m_MeshFilter = GetComponent<MeshFilter>();

            m_MeshRenderer.sharedMaterial = anim.material;
            m_MeshFilter.sharedMesh = anim.mesh;
        }
#endif

    }
}
