using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Unity.EditorCoroutines.Editor;

namespace GameWish.Game.Editor
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
            var animator = target.GetComponent<Animator>();

            var clips = animator.runtimeAnimatorController.animationClips;
            Debug.LogError($"clips.length:{clips.Length}");

            EditorCoroutineUtility.StartCoroutineOwnerless(BakeAllClip(clips));

            //材质球必须开启Instance
            if (animation.material != null)
            {
                animation.material.enableInstancing = true;
                EditorUtility.SetDirty(animation.material);
                AssetDatabase.SaveAssetIfDirty(animation.material);
            }
        }

        private IEnumerator BakeAllClip(AnimationClip[] clips)
        {
            Debug.LogError($"Bake Start------------");
            AnimationMode.StartAnimationMode();

            int clipIndex = 0;
            for (int i = 0; i < clips.Length; i++)
            {
                var clip = clips[clipIndex];
                yield return EditorCoroutineUtility.StartCoroutineOwnerless(BakeClip(clip));
                clipIndex++;
            }
            AnimationMode.StopAnimationMode();

            CreateTextureMatrix(savePath);

            // Bake完成 清除Frame数据 Clear 
            for (int i = 0; i < animation.clips.Length; i++)
            {
                animation.clips[i].frames = null;
            }

            EditorUtility.SetDirty(animation);
            AssetDatabase.SaveAssetIfDirty(animation);
            Debug.LogError($"Bake Over------------------");
        }

        private IEnumerator BakeClip(AnimationClip clip)
        {
            Debug.LogError($"Start Bake Clip:{clip.name}");
            samplingFrameIndex = 0;

            var skinningClip = animation.GetGPUSkinningClip(clip.name);
            yield return EditorCoroutineUtility.StartCoroutineOwnerless(BakeClipFrame(clip, skinningClip, 0));
        }

        private IEnumerator BakeClipFrame(AnimationClip clip, GPUSkinningClip skinningClip, float rate)
        {
            int totalFrameCount = skinningClip.totalFrameCount;



            Debug.LogError($"Bake Clip Frame:{clip.name}---{samplingFrameIndex} {totalFrameCount} {rate}");
            BakeAnimEvent(clip, skinningClip);
            AnimationMode.BeginSampling();
            AnimationMode.SampleAnimationClip(target, clip, rate);
            AnimationMode.EndSampling();

            var frame = skinningClip.frames[samplingFrameIndex];
            yield return EditorCoroutineUtility.StartCoroutineOwnerless(SamplingAnimation(frame));

            if (samplingFrameIndex < totalFrameCount)
            {
                float animRate = clip.length / totalFrameCount * samplingFrameIndex;
                yield return EditorCoroutineUtility.StartCoroutineOwnerless(BakeClipFrame(clip, skinningClip, animRate));
            }
        }

        void BakeAnimEvent(AnimationClip clip, GPUSkinningClip skinningClip)
        {
            var animEvents = clip.events;
            if (animEvents.Length <= 0)
            {
                skinningClip.events = null;
                return;
            }

            skinningClip.events = new GPUSkinningAnimEvent[animEvents.Length];
            for (int i = 0; i < animEvents.Length; ++i)
            {
                var animEvent = animEvents[i];
                GPUSkinningAnimEvent evt = new GPUSkinningAnimEvent();
                evt.frameIndex = (int)(animEvent.time * clip.frameRate);
                evt.eventId = animEvent.intParameter;
                skinningClip.events[i] = evt;
            }
        }



        int samplingFrameIndex;
        // private Vector3 rootMotionPosition;
        // private Quaternion rootMotionRotation;
        // private GPUSkinningClip gpuSkinningClip = null;
        private IEnumerator SamplingAnimation(GPUSkinningFrame frame)
        {
            yield return new WaitForEndOfFrame();

            var bones = animation.bones;
            for (int i = 0; i < bones.Length; ++i)
            {
                Transform boneTransform = bones[i].transform;
                GPUSkinningBone currentBone = GetBoneByTransform(boneTransform);
                frame.matrices[i] = currentBone.bindpose;

                do
                {
                    //层层传递
                    Matrix4x4 mat = Matrix4x4.TRS(currentBone.transform.localPosition, currentBone.transform.localRotation, currentBone.transform.localScale);
                    frame.matrices[i] = mat * frame.matrices[i];
                    if (currentBone.parentBoneIndex == -1)
                    {
                        break;
                    }
                    else
                    {
                        currentBone = bones[currentBone.parentBoneIndex];
                    }
                }
                while (true);
            }


            // if (samplingFrameIndex == 0)
            // {
            //     rootMotionPosition = bones[animation.rootBoneIndex].transform.localPosition;
            //     rootMotionRotation = bones[animation.rootBoneIndex].transform.localRotation;
            // }
            // else
            // {
            //     Vector3 newPosition = bones[animation.rootBoneIndex].transform.localPosition;
            //     Quaternion newRotation = bones[animation.rootBoneIndex].transform.localRotation;
            //     Vector3 deltaPosition = newPosition - rootMotionPosition;
            //     frame.rootMotionDeltaPositionQ = Quaternion.Inverse(Quaternion.Euler(target.transform.forward.normalized)) * Quaternion.Euler(deltaPosition.normalized);
            //     frame.rootMotionDeltaPositionL = deltaPosition.magnitude;
            //     frame.rootMotionDeltaRotation = Quaternion.Inverse(rootMotionRotation) * newRotation;
            //     rootMotionPosition = newPosition;
            //     rootMotionRotation = newRotation;

            //     if (samplingFrameIndex == 1)
            //     {
            //         gpuSkinningClip.frames[0].rootMotionDeltaPositionQ = gpuSkinningClip.frames[1].rootMotionDeltaPositionQ;
            //         gpuSkinningClip.frames[0].rootMotionDeltaPositionL = gpuSkinningClip.frames[1].rootMotionDeltaPositionL;
            //         gpuSkinningClip.frames[0].rootMotionDeltaRotation = gpuSkinningClip.frames[1].rootMotionDeltaRotation;
            //     }
            // }

            ++samplingFrameIndex;


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
            }
        }


        private void InitClip()
        {
            var animator = target.GetComponent<Animator>();
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
