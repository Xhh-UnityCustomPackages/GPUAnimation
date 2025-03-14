using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Unity.EditorCoroutines.Editor;
using UnityEditor.Animations;

namespace GameWish.Game.Editor
{
    public partial class GPUSkinningBaker
    {
        public SkinnedMeshRenderer skinnedMeshRenderer;
        public GPUSkinningAnimation animation;
        public Transform rootBoneTransform = null;
        public GameObject target;
        private List<AnimatorStateClip> stateClips = new List<AnimatorStateClip>();

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
            EditorCoroutineUtility.StartCoroutineOwnerless(BakeAllClip(stateClips));

            //材质球必须开启Instance
            if (animation.material != null)
            {
                animation.material.enableInstancing = true;
                EditorUtility.SetDirty(animation.material);
                AssetDatabase.SaveAssetIfDirty(animation.material);
            }
        }

        private IEnumerator BakeAllClip(List<AnimatorStateClip> stateClips)
        {
            Debug.LogError($"Bake Start------------");
            AnimationMode.StartAnimationMode();
            Debug.LogError($"---{stateClips.Count}");
            int clipIndex = 0;
            for (int i = 0; i < stateClips.Count; i++)
            {
                var stateClip = stateClips[clipIndex];
                Debug.LogError($"Bake Clip:{stateClip.stateName}");
                yield return EditorCoroutineUtility.StartCoroutineOwnerless(BakeClip(stateClip));
                clipIndex++;
            }

            AnimationMode.StopAnimationMode();

            CreateTextureMatrix(savePath);

            // Bake完成 清除Frame数据 Clear 
            for (int i = 0; i < animation.clips.Length; i++)
            {
                animation.clips[i].frames = null;
            }

            animation.InitMaterial();

            EditorUtility.SetDirty(animation);
            AssetDatabase.SaveAssetIfDirty(animation);
            Debug.LogError($"Bake Over------------------");
        }

        private IEnumerator BakeClip(AnimatorStateClip stateClip)
        {
            Debug.LogError($"Start Bake State:{stateClip.stateName}--{stateClip.clip.name}");
            samplingFrameIndex = 0;

            var skinningClip = animation.GetGPUSkinningClip(stateClip.stateName);
            yield return EditorCoroutineUtility.StartCoroutineOwnerless(BakeClipFrame(stateClip.clip, skinningClip, 0));
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
                } while (true);
            }

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
            var ac = animator.runtimeAnimatorController as AnimatorController;

            stateClips.Clear();
            foreach (var layer in ac.layers)
            {
                var stateMachine = layer.stateMachine;
                foreach (var state in stateMachine.states)
                {
                    AnimatorState animatorState = state.state;
                    stateClips.Add(new AnimatorStateClip() { stateName = animatorState.name, clip = animatorState.motion as AnimationClip });
                }
            }


            Debug.LogError($"stateClips.Count:{stateClips.Count}");
            if (stateClips.Count <= 0)
                return;

            animation.clips = new GPUSkinningClip[stateClips.Count];
            for (int i = 0; i < stateClips.Count; i++)
            {
                var clip = stateClips[i].clip;
                var gpuSkinningClip = new GPUSkinningClip();
                gpuSkinningClip.name = stateClips[i].stateName;
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


        internal class AnimatorStateClip
        {
            public string stateName;
            public AnimationClip clip;
        }
    }
}