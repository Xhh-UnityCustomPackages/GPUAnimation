using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ShowBones : MonoBehaviour
{
    public SkinnedMeshRenderer skinnedMeshRenderer;
    public Transform[] bones;



#if UNITY_EDITOR
    private void OnValidate()
    {
        skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
        bones = skinnedMeshRenderer.bones;
    }

    private void OnDrawGizmos()
    {
        skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
        bones = skinnedMeshRenderer.bones;

        foreach (Transform bone in bones)
        {
            Handles.color = Color.yellow;
            Handles.SphereHandleCap(0, bone.position, Quaternion.identity, 0.1f, EventType.Repaint);
            // Handles.color = Color.white;
            // Handles.ArrowHandleCap(0, bone.position, Quaternion.LookRotation(bone.forward), 0.5f, EventType.Repaint);
        }
    }
#endif
}
