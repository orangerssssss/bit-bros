using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 自动生成 King Boss 预制体，并在 FinalScene 中放置 Boss 与 SpawnPoint。
/// </summary>
public static class FinalSceneKingBossSetup
{
    private const string FinalScenePath = "Assets/Scenes/FinalScene.scene";
    private const string SourceEnemyPrefabPath = "Assets/Prefabs/Characters/Enemy_Soldier.prefab";
    private const string BirthScenePath = "Assets/Scenes/BirthScene.scene";
    private const string KingModelPath = "Assets/King/KingBlend.fbx";
    private const string BossPrefabPath = "Assets/Prefabs/Characters/Enemy_KingBoss.prefab";
    private const string SceneBossName = "KingBoss";
    private const string BossVisualName = "KingVisual";
    private static readonly string[] EssentialRootNames =
    {
        "GameManager",
        "GlobalGameManager",
        "PlayerControl",
        "UICamera",
        "GameUI"
    };

    static FinalSceneKingBossSetup()
    {
        EditorApplication.delayCall += TrySetup;
    }

    [MenuItem("Tools/Final Scene/Setup King Boss")]
    public static void ForceSetup()
    {
        TrySetup();
    }

    private static void TrySetup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        GameObject bossPrefab = EnsureBossPrefab();
        if (bossPrefab != null)
        {
            EnsureBossInFinalScene(bossPrefab);
        }
    }

    private static GameObject EnsureBossPrefab()
    {
        GameObject sourceEnemy = AssetDatabase.LoadAssetAtPath<GameObject>(SourceEnemyPrefabPath);
        GameObject kingModel = AssetDatabase.LoadAssetAtPath<GameObject>(KingModelPath);
        if (sourceEnemy == null || kingModel == null)
        {
            Debug.LogWarning("King Boss setup skipped: source enemy prefab or king model is missing.");
            return null;
        }

        Avatar kingAvatar = LoadHumanoidAvatar(KingModelPath);
        if (kingAvatar == null)
        {
            Debug.LogWarning("King Boss setup skipped: could not find a humanoid avatar on KingBlend.fbx.");
            return null;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(SourceEnemyPrefabPath);
        try
        {
            root.name = SceneBossName;

            DisableExistingRenderers(root);
            RemoveExistingKingVisual(root.transform);

            GameObject kingInstance = Object.Instantiate(kingModel);
            kingInstance.name = BossVisualName;
            kingInstance.transform.SetParent(root.transform, false);
            CleanupNestedAnimators(kingInstance);
            SetLayerRecursively(kingInstance, root.layer);
            FitVisualToHeight(kingInstance.transform, 2.4f);

            Animator animator = root.GetComponent<Animator>();
            if (animator != null)
            {
                animator.avatar = kingAvatar;
                animator.applyRootMotion = false;
            }

            CapsuleCollider capsule = root.GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                capsule.height = 2.2f;
                capsule.radius = 0.45f;
                capsule.center = new Vector3(0f, 1.1f, 0f);
            }

            NavMeshAgent agent = root.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.height = 2.2f;
                agent.radius = 0.35f;
                agent.speed = 4.1f;
                agent.angularSpeed = 720f;
                agent.acceleration = 60f;
            }

            FightAISoldier oldAI = root.GetComponent<FightAISoldier>();
            BoxAttackArea attackBox = null;
            ParticleSystem damageParticle = null;
            AudioSource damageSfx = null;
            AudioSource attackAudio = null;
            float corpseDelay = 3f;
            if (oldAI != null)
            {
                SerializedObject oldAiSerialized = new SerializedObject(oldAI);
                attackBox = oldAiSerialized.FindProperty("attackBox")?.objectReferenceValue as BoxAttackArea;
                damageParticle = oldAiSerialized.FindProperty("damageParticle")?.objectReferenceValue as ParticleSystem;
                damageSfx = oldAiSerialized.FindProperty("damageSFX")?.objectReferenceValue as AudioSource;
                attackAudio = oldAiSerialized.FindProperty("attackAudioSource")?.objectReferenceValue as AudioSource;
                corpseDelay = oldAiSerialized.FindProperty("corpseDelay")?.floatValue ?? 3f;
                Object.DestroyImmediate(oldAI, true);
            }

            FightAIKingBoss bossAI = root.GetComponent<FightAIKingBoss>();
            if (bossAI == null)
            {
                bossAI = root.AddComponent<FightAIKingBoss>();
            }

            SerializedObject bossAiSerialized = new SerializedObject(bossAI);
            bossAiSerialized.FindProperty("corpseDelay").floatValue = corpseDelay;
            bossAiSerialized.FindProperty("damageParticle").objectReferenceValue = damageParticle;
            bossAiSerialized.FindProperty("damageSFX").objectReferenceValue = damageSfx;
            bossAiSerialized.FindProperty("attackBox").objectReferenceValue = attackBox;
            bossAiSerialized.FindProperty("attackAudioSource").objectReferenceValue = attackAudio;
            bossAiSerialized.ApplyModifiedPropertiesWithoutUndo();

            FightAttributes attributes = root.GetComponent<FightAttributes>();
            if (attributes != null)
            {
                attributes.level = 8;
                attributes.experience = 80;
                attributes.career = CharacterAttributes.CharacterCareer.骑士;
                attributes.health = 0;
                attributes.mana = 0;
                attributes.fightName = "King";
                attributes.combatCamp = CombatCamp.Enemy;
                attributes.ChangeAttributes(12, 12, 6);
            }

            EnemyHeadDisplayer headDisplayer = root.GetComponent<EnemyHeadDisplayer>();
            if (headDisplayer != null)
            {
                EnsureHeadDisplayerReferences(root, headDisplayer);
            }

            PrefabUtility.SaveAsPrefabAsset(root, BossPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);
    }

    private static void EnsureBossInFinalScene(GameObject bossPrefab)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        bool sceneAlreadyOpen = activeScene.path == FinalScenePath;
        Scene finalScene = sceneAlreadyOpen
            ? activeScene
            : EditorSceneManager.OpenScene(FinalScenePath, OpenSceneMode.Additive);

        try
        {
            EnsureSceneEssentials(finalScene);

            GameObject bossObject = finalScene.GetRootGameObjects().FirstOrDefault(go => go.name == SceneBossName);
            Bounds arenaBounds = CollectSceneBounds(finalScene);
            Vector3 center = arenaBounds.size.sqrMagnitude > 0.001f ? arenaBounds.center : Vector3.zero;
            float groundY = arenaBounds.size.sqrMagnitude > 0.001f ? arenaBounds.min.y : 0f;
            float depthOffset = Mathf.Max(5f, arenaBounds.extents.z * 0.35f);

            if (bossObject == null)
            {
                bossObject = (GameObject)PrefabUtility.InstantiatePrefab(bossPrefab, finalScene);
                bossObject.name = SceneBossName;
            }

            bossObject.transform.position = new Vector3(center.x, groundY + 0.05f, center.z + 1.4f);
            bossObject.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            EnsureSpawnPoint(finalScene, center, groundY, depthOffset);
            EnsurePlayerAtSpawnPoint(finalScene);

            EditorSceneManager.MarkSceneDirty(finalScene);
            EditorSceneManager.SaveScene(finalScene);
        }
        finally
        {
            if (!sceneAlreadyOpen)
            {
                EditorSceneManager.CloseScene(finalScene, true);
            }
        }
    }

    private static void EnsureSpawnPoint(Scene scene, Vector3 center, float groundY, float depthOffset)
    {
        GameObject spawnPoint = scene.GetRootGameObjects().FirstOrDefault(go => go.name == "SpawnPoint");
        if (spawnPoint == null)
        {
            spawnPoint = new GameObject("SpawnPoint");
            SceneManager.MoveGameObjectToScene(spawnPoint, scene);
        }

        spawnPoint.transform.position = new Vector3(center.x, groundY + 0.05f, center.z - depthOffset);
        spawnPoint.transform.rotation = Quaternion.identity;
    }

    private static void EnsureSceneEssentials(Scene finalScene)
    {
        Scene birthScene = SceneManager.GetSceneByPath(BirthScenePath);
        bool openedSourceScene = !birthScene.isLoaded;
        if (openedSourceScene)
        {
            birthScene = EditorSceneManager.OpenScene(BirthScenePath, OpenSceneMode.Additive);
        }

        try
        {
            foreach (string rootName in EssentialRootNames)
            {
                if (finalScene.GetRootGameObjects().Any(go => go.name == rootName))
                {
                    continue;
                }

                GameObject sourceRoot = birthScene.GetRootGameObjects().FirstOrDefault(go => go.name == rootName);
                if (sourceRoot == null)
                {
                    continue;
                }

                GameObject clone = Object.Instantiate(sourceRoot);
                clone.name = sourceRoot.name;
                SceneManager.MoveGameObjectToScene(clone, finalScene);
            }
        }
        finally
        {
            if (openedSourceScene)
            {
                EditorSceneManager.CloseScene(birthScene, true);
            }
        }
    }

    private static void EnsurePlayerAtSpawnPoint(Scene finalScene)
    {
        GameObject spawnPoint = finalScene.GetRootGameObjects().FirstOrDefault(go => go.name == "SpawnPoint");
        GameObject player = finalScene.GetRootGameObjects().FirstOrDefault(go => go.name == "PlayerControl");
        if (spawnPoint == null || player == null)
        {
            return;
        }

        player.transform.position = spawnPoint.transform.position;
        player.transform.rotation = spawnPoint.transform.rotation;
    }


    private static Bounds CollectSceneBounds(Scene scene)
    {
        Renderer[] renderers = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
            .Where(renderer => renderer.enabled)
            .ToArray();

        if (renderers.Length == 0)
        {
            return new Bounds(Vector3.zero, Vector3.zero);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        return bounds;
    }

    private static void DisableExistingRenderers(GameObject root)
    {
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer is ParticleSystemRenderer)
            {
                continue;
            }

            renderer.enabled = false;
        }
    }

    private static void RemoveExistingKingVisual(Transform root)
    {
        Transform old = root.Find(BossVisualName);
        if (old != null)
        {
            Object.DestroyImmediate(old.gameObject);
        }
    }

    private static void CleanupNestedAnimators(GameObject visualRoot)
    {
        foreach (Animator nestedAnimator in visualRoot.GetComponentsInChildren<Animator>(true))
        {
            Object.DestroyImmediate(nestedAnimator, true);
        }
    }

    private static void SetLayerRecursively(GameObject gameObject, int layer)
    {
        gameObject.layer = layer;
        foreach (Transform child in gameObject.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private static void FitVisualToHeight(Transform visualRoot, float desiredHeight)
    {
        Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        float currentHeight = Mathf.Max(0.001f, bounds.size.y);
        float scale = desiredHeight / currentHeight;
        visualRoot.localScale = Vector3.one * scale;

        renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        Vector3 localPosition = visualRoot.localPosition;
        localPosition.y -= bounds.min.y;
        visualRoot.localPosition = localPosition;
    }

    private static Avatar LoadHumanoidAvatar(string modelPath)
    {
        return AssetDatabase.LoadAllAssetsAtPath(modelPath)
            .OfType<Avatar>()
            .FirstOrDefault(avatar => avatar != null && avatar.isValid && avatar.isHuman);
    }

    private static void EnsureHeadDisplayerReferences(GameObject root, EnemyHeadDisplayer headDisplayer)
    {
        Canvas canvas = root.GetComponentsInChildren<Canvas>(true).FirstOrDefault();
        Slider[] sliders = root.GetComponentsInChildren<Slider>(true);
        Text[] texts = root.GetComponentsInChildren<Text>(true);

        if (canvas == null || sliders.Length < 2 || texts.Length == 0)
        {
            return;
        }

        SerializedObject serializedObject = new SerializedObject(headDisplayer);
        serializedObject.FindProperty("state").objectReferenceValue = canvas.gameObject;
        serializedObject.FindProperty("stateText").objectReferenceValue = texts.FirstOrDefault(text => text.name.Contains("Text"));
        serializedObject.FindProperty("healthBar").objectReferenceValue = sliders.FirstOrDefault(slider => slider.name == "HealthBar");
        serializedObject.FindProperty("healthBarSlow").objectReferenceValue = sliders.FirstOrDefault(slider => slider.name == "HealthBarSlow");
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }
}
