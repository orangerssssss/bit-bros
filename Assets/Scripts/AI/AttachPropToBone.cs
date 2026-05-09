using System;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("AI/Attach Prop To Bone")]
public class AttachPropToBone : MonoBehaviour
{
    [Tooltip("The prop (weapon) Transform to attach. If empty, script will try to find a child like 'item' or 'sword'.")]
    public Transform prop;

    [Tooltip("If true, use the Animator/Humanoid mapping to find the bone (Animator.GetBoneTransform).")]
    public bool useHumanoidBone = true;

    [Tooltip("Humanoid bone to attach to when using humanoid lookup.")]
    public HumanBodyBones humanoidBone = HumanBodyBones.RightHand;

    [Tooltip("If not using humanoid lookup, this partial name/path will be searched for in the hierarchy.")]
    public string boneNameOrPath;

    [Tooltip("If true, preserve the prop's local transform after parenting. Otherwise reset to zero.")]
    public bool preserveLocalTransform = false;

    [Tooltip("Optional root to search from. Defaults to this GameObject's transform.")]
    public Transform searchRoot;

    private void Start()
    {
        if (prop == null)
        {
            prop = FindLikelyProp();
            if (prop == null)
            {
                Debug.LogWarning($"{name} AttachPropToBone: prop not set and no candidate child found.");
                return;
            }
        }

        Transform targetBone = null;
        Animator animator = GetComponentInChildren<Animator>();
        if (useHumanoidBone && animator != null && animator.isHuman)
        {
            targetBone = animator.GetBoneTransform(humanoidBone);
        }

        if (targetBone == null)
        {
            Transform root = searchRoot != null ? searchRoot : transform;
            if (!string.IsNullOrEmpty(boneNameOrPath))
            {
                targetBone = root.Find(boneNameOrPath);
                if (targetBone == null)
                    targetBone = FindChildByName(root, boneNameOrPath);
            }
        }

        if (targetBone == null && animator != null)
        {
            string[] candidates = { "RightHand", "Hand_R", "hand_r", "mixamorig:RightHand", "RightHandTarget" };
            foreach (var c in candidates)
            {
                var t = FindChildByName(transform, c);
                if (t != null)
                {
                    targetBone = t;
                    break;
                }
            }
        }

        if (targetBone == null)
        {
            Debug.LogWarning($"{name} AttachPropToBone: failed to find target bone to attach '{prop.name}'.");
            return;
        }

        Vector3 savedPos = prop.localPosition;
        Quaternion savedRot = prop.localRotation;
        Vector3 savedScale = prop.localScale;

        prop.SetParent(targetBone, worldPositionStays: false);

        if (!preserveLocalTransform)
        {
            prop.localPosition = Vector3.zero;
            prop.localRotation = Quaternion.identity;
            prop.localScale = Vector3.one;
        }
        else
        {
            prop.localPosition = savedPos;
            prop.localRotation = savedRot;
            prop.localScale = savedScale;
        }

        Debug.Log($"{name} AttachPropToBone: attached {prop.name} to {targetBone.name}");
    }

    private Transform FindLikelyProp()
    {
        foreach (Transform t in transform)
        {
            string n = t.name.ToLower();
            if (n.Contains("item") || n.Contains("sword") || n.Contains("weapon") || n.Contains("prop"))
                return t;
        }
        return null;
    }

    private Transform FindChildByName(Transform root, string partialName)
    {
        string pn = partialName.ToLower();
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name.ToLower().Contains(pn) || t.name.ToLower() == pn)
                return t;
        }
        return null;
    }
}
