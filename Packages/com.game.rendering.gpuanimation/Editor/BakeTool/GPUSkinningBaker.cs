using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.GPUSkinning.Editor
{
    public partial class GPUSkinningBaker
    {

        public SkinnedMeshRenderer skinnedMeshRenderer;
        public GPUSkinningAnimation animation;
        public Transform rootBoneTransform = null;
        public GameObject target;

        public string savePath;

        public void Init()
        {
            animation.name = target.name;

            InitBones();
            CreateGPUSkinningMeshes(savePath);
            InitClip();
            InitTextureInfo();

            EditorUtility.SetDirty(animation);
            AssetDatabase.SaveAssetIfDirty(animation);
        }

        public void Bake()
        {


        }

        void InitBones()
        {
            List<GPUSkinningBone> bones_result = new List<GPUSkinningBone>();
            CollectBones(bones_result, skinnedMeshRenderer.bones, skinnedMeshRenderer.sharedMesh.bindposes, null, rootBoneTransform, 0);
            GPUSkinningBone[] newBones = bones_result.ToArray();
            GenerateBonesGUID(newBones);

            animation.bones = newBones;
            animation.rootBoneIndex = 0;
        }

        private void CollectBones(List<GPUSkinningBone> bones_result, Transform[] bones_smr, Matrix4x4[] bindposes, GPUSkinningBone parentBone, Transform currentBoneTransform, int currentBoneIndex)
        {
            GPUSkinningBone currentBone = new GPUSkinningBone();
            bones_result.Add(currentBone);

            int indexOfSmrBones = System.Array.IndexOf(bones_smr, currentBoneTransform);
            currentBone.transform = currentBoneTransform;
            currentBone.name = currentBone.transform.gameObject.name;
            currentBone.bindpose = indexOfSmrBones == -1 ? Matrix4x4.identity : bindposes[indexOfSmrBones];
            currentBone.parentBoneIndex = parentBone == null ? -1 : bones_result.IndexOf(parentBone);

            if (parentBone != null)
            {
                parentBone.childrenBonesIndices[currentBoneIndex] = bones_result.IndexOf(currentBone);
            }

            int numChildren = currentBone.transform.childCount;
            if (numChildren > 0)
            {
                currentBone.childrenBonesIndices = new int[numChildren];
                for (int i = 0; i < numChildren; ++i)
                {
                    CollectBones(bones_result, bones_smr, bindposes, currentBone, currentBone.transform.GetChild(i), i);
                }
            }
        }

        private void GenerateBonesGUID(GPUSkinningBone[] bones)
        {
            int numBones = bones == null ? 0 : bones.Length;
            for (int i = 0; i < numBones; ++i)
            {
                string boneHierarchyPath = GPUSkinningUtil.BoneHierarchyPath(bones, i);
                string guid = GPUSkinningUtil.MD5(boneHierarchyPath);
                bones[i].guid = guid;
            }
        }


        private void InitClip()
        {
            var animator = target.GetComponent<Animator>();
            if (animator == null)
            {
                return;
            }
            var animationClips = animator.runtimeAnimatorController.animationClips;
            if (animationClips.Length <= 0)
                return;

            animation.clips = new GPUSkinningClip[animationClips.Length];
            for (int i = 0; i < animationClips.Length; i++)
            {
                var clip = animationClips[i];
                var gpuSkinningClip = new GPUSkinningClip();
                gpuSkinningClip.name = clip.name;
                gpuSkinningClip.length = clip.length;
                gpuSkinningClip.frameRate = clip.frameRate;
                gpuSkinningClip.wrapMode = clip.isLooping ? GPUSkinningWrapMode.Loop : GPUSkinningWrapMode.Once;
                int frameCount = (int)(clip.frameRate * clip.length);
                gpuSkinningClip.frames = new GPUSkinningFrame[frameCount];
                for (int f = 0; f < frameCount; f++)
                {
                    var frame = new GPUSkinningFrame();
                    frame.matrices = new Matrix4x4[animation.bones.Length];
                    gpuSkinningClip.frames[f] = frame;
                }
                animation.clips[i] = gpuSkinningClip;
            }
        }

    }
}
