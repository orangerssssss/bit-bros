using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class BossAnimationEventsEditor : EditorWindow
{
    private AnimationClip clip;
    private float impactNormalizedTime = 0.5f;
    private bool addSFXEvent = true;
    private bool addEndEvent = true;
    private string attackEventName = "AttackEvent";
    private string sfxEventName = "PlayAttackSFX";
    private string attackEndEventName = "AttackEndEvent";

    [MenuItem("Window/Boss Tools/Add Animation Events to Clip")]
    public static void ShowWindow()
    {
        var w = GetWindow<BossAnimationEventsEditor>("Boss Anim Events");
        w.minSize = new Vector2(380, 140);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Add boss attack events to an AnimationClip", EditorStyles.boldLabel);
        clip = (AnimationClip)EditorGUILayout.ObjectField("Animation Clip", clip, typeof(AnimationClip), false);
        impactNormalizedTime = EditorGUILayout.Slider(new GUIContent("Impact time (normalized)", "0=clip start, 1=clip end"), impactNormalizedTime, 0f, 1f);
        addSFXEvent = EditorGUILayout.Toggle(new GUIContent("Add SFX Event", "Also add audio play event near impact"), addSFXEvent);

        GUILayout.Space(6);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Events to Selected Clip", GUILayout.Height(28)))
        {
            if (clip == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select an AnimationClip first.", "OK");
            }
            else
            {
                AddEventsToClip(clip);
            }
        }

        if (GUILayout.Button("Refresh Selected From Project", GUILayout.Height(28)))
        {
            // try to set clip automatically from selection
            var obj = Selection.activeObject as AnimationClip;
            if (obj != null) clip = obj;
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(6);
        EditorGUILayout.LabelField("Event function names (optional):");
        attackEventName = EditorGUILayout.TextField("Attack event", attackEventName);
        sfxEventName = EditorGUILayout.TextField("SFX event", sfxEventName);
        addEndEvent = EditorGUILayout.Toggle(new GUIContent("Add End Event", "Add an AttackEndEvent near clip end to release attack lock"), addEndEvent);
        attackEndEventName = EditorGUILayout.TextField("End event", attackEndEventName);
    }

    private void AddEventsToClip(AnimationClip c)
    {
        if (c == null) return;

        // get existing events and remove any with same names to avoid duplicates
        var existing = AnimationUtility.GetAnimationEvents(c);
        var list = new List<AnimationEvent>(existing.Length + 2);
        foreach (var e in existing)
        {
            if (e.functionName == attackEventName || e.functionName == sfxEventName) continue;
            list.Add(e);
        }

        float impactTime = Mathf.Clamp01(impactNormalizedTime) * c.length;

        var ev = new AnimationEvent();
        ev.functionName = attackEventName;
        ev.time = impactTime;
        list.Add(ev);

        if (addSFXEvent)
        {
            var ev2 = new AnimationEvent();
            ev2.functionName = sfxEventName;
            ev2.time = Mathf.Max(0f, impactTime - Mathf.Min(0.05f, c.length * 0.02f));
            list.Add(ev2);
        }

        if (addEndEvent)
        {
            var ev3 = new AnimationEvent();
            ev3.functionName = attackEndEventName;
            ev3.time = Mathf.Clamp(c.length - Mathf.Min(0.03f, c.length * 0.02f), 0f, c.length - 0.001f);
            list.Add(ev3);
        }

        AnimationUtility.SetAnimationEvents(c, list.ToArray());
        EditorUtility.SetDirty(c);
        AssetDatabase.SaveAssets();
        Debug.Log($"BossAnimationEventsEditor: Added events to '{c.name}' at {impactTime:F3}s (normalized {impactNormalizedTime:F2}).");
    }
}
