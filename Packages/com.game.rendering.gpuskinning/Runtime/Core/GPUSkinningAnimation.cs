using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.IO;

namespace GameWish.Game
{
    public class GPUSkinningAnimation : ScriptableObject
    {
        public new string name = null;
        public GPUSkinningBone[] bones = null;
        public int rootBoneIndex = 0;
        public GPUSkinningClip[] clips = null;


        public Bounds bounds;
        public int textureWidth = 0;
        public int textureHeight = 0;


        public Mesh mesh = null;
        public TextAsset texture = null;
        public Material material = null;


        public GPUSkinningClip GetGPUSkinningClip(string name)
        {
            if (clips == null || clips.Length == 0)
            {
                return null;
            }

            for (int i = 0; i < clips.Length; ++i)
            {
                if (clips[i].name == name)
                {
                    return clips[i];
                }
            }

            return null;
        }

#if UNITY_EDITOR
        [Button]
        public void CreateTexture()
        {
            if (texture == null)
                return;

            var texture2D = GPUSkinningUtil.CreateTexture2DWithoutCache(texture, this);

            //创建在当前目录
            var path = AssetDatabase.GetAssetPath(texture);

            var directory = Path.GetDirectoryName(path);
            var savePath = directory + $"/{texture.name}.asset";
            savePath = savePath.Replace(Application.dataPath, "Assets/");

            // byte[] bytes = texture2D.EncodeToPNG();
            AssetDatabase.CreateAsset(texture2D, savePath);
            AssetDatabase.Refresh();
        }

        [Button]
        public void InitMaterial()
        {
            if (material == null)
                return;
            var path = AssetDatabase.GetAssetPath(texture);
            var directory = Path.GetDirectoryName(path);
            var savePath = directory + $"/{texture.name}.asset";
            var textureAsset = AssetDatabase.LoadAssetAtPath<Texture>(savePath);

            material.SetTexture(GPUSkinningPlayerResources.ShaderIDs.GPUSkinning_TextureMatrix, textureAsset);
            material.SetVector(GPUSkinningPlayerResources.ShaderIDs.GPUSkinning_TextureSize_NumPixelsPerFrame,
                new Vector4(textureWidth, textureHeight, bones.Length * 3 /*treat 3 pixels as a float3x4*/, 0));

            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
        }
#endif
    }
}