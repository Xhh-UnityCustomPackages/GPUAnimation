using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
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


        [Button]
        void CreateTexture()
        {
            if (texture == null)
                return;

            var texture2D = GPUSkinningUtil.CreateTexture2DWithoutCache(texture, this);

            //创建在当前目录
            var path = AssetDatabase.GetAssetPath(texture);

            var directory = Path.GetDirectoryName(path);
            var savePath = directory + "/Texture.asset";
            savePath = savePath.Replace(Application.dataPath, "Assets/");

            // byte[] bytes = texture2D.EncodeToPNG();
            AssetDatabase.CreateAsset(texture2D, savePath);
        }

    }
}
