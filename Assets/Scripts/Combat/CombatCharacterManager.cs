using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum CombatCamp
{
    None,
    Player,
    Enemy
}

public class CombatCharacterManager : MonoBehaviour
{
    private static CombatCharacterManager instance;// 单例
    public static CombatCharacterManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = GameObject.FindObjectOfType<CombatCharacterManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("CombatCharacterManager");
                    instance = go.AddComponent<CombatCharacterManager>();
                    Object.DontDestroyOnLoad(go);
                    Debug.Log("CombatCharacterManager: created singleton at runtime");
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            Object.DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            // If another instance exists, destroy this duplicate to avoid stale singletons when switching scenes
            Destroy(gameObject);
        }

        // When the manager initializes, rebuild registry from existing CharacterAttributes
        if (instance == this)
        {
            RebuildRegistry();
        }
        // Subscribe to sceneLoaded so registry is rebuilt after each scene load
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"CombatCharacterManager: Scene loaded ({scene.name}), rebuilding registry.");
        RebuildRegistry();
    }


    private List<CharacterAttributes> noneCampCharacters = new List<CharacterAttributes>();
    private List<CharacterAttributes> playerCampCharacters = new List<CharacterAttributes>();
    private List<CharacterAttributes> enemyCampCharacters = new List<CharacterAttributes>();

    public void Register(CharacterAttributes attributes)
    {
        if (attributes == null) return;

        Debug.Log($"CombatCharacterManager: Register called -> {attributes.gameObject.name} (camp={attributes.combatCamp})");

        if (attributes.combatCamp == CombatCamp.None && !noneCampCharacters.Contains(attributes))
        {
            noneCampCharacters.Add(attributes);
            Debug.Log($"CombatCharacterManager: Added to NoneCamp -> {attributes.gameObject.name}");
        }
        if (attributes.combatCamp == CombatCamp.Player && !playerCampCharacters.Contains(attributes))
        {
            playerCampCharacters.Add(attributes);
            Debug.Log($"CombatCharacterManager: Added to PlayerCamp -> {attributes.gameObject.name}");
        }
        if (attributes.combatCamp == CombatCamp.Enemy && !enemyCampCharacters.Contains(attributes))
        {
            enemyCampCharacters.Add(attributes);
            Debug.Log($"CombatCharacterManager: Added to EnemyCamp -> {attributes.gameObject.name}");
        }
    }

    public void Unregister(CharacterAttributes attributes)
    {
        if (attributes == null) return;

        Debug.Log($"CombatCharacterManager: Unregister called -> {attributes.gameObject.name} (camp={attributes.combatCamp})");

        if (attributes.combatCamp == CombatCamp.None && noneCampCharacters.Contains(attributes))
        {
            noneCampCharacters.Remove(attributes);
            Debug.Log($"CombatCharacterManager: Removed from NoneCamp -> {attributes.gameObject.name}");
        }
        if (attributes.combatCamp == CombatCamp.Player && playerCampCharacters.Contains(attributes))
        {
            playerCampCharacters.Remove(attributes);
            Debug.Log($"CombatCharacterManager: Removed from PlayerCamp -> {attributes.gameObject.name}");
        }
        if (attributes.combatCamp == CombatCamp.Enemy && enemyCampCharacters.Contains(attributes))
        {
            enemyCampCharacters.Remove(attributes);
            Debug.Log($"CombatCharacterManager: Removed from EnemyCamp -> {attributes.gameObject.name}");
        }
    }

    public CharacterAttributes FindNearestNoneCampCharacter(Vector3 selfPosition)
    {
        return FindNearestCharacter(noneCampCharacters, selfPosition);
    }

    public CharacterAttributes FindNearestPlayerCampCharacter(Vector3 selfPosition)
    {
        // Prefer objects tagged 'Player' (main player) to avoid dummy/workspace placeholders being selected
        CharacterAttributes nearestTagged = null;
        float minTaggedDist = float.MaxValue;
        foreach (var c in playerCampCharacters)
        {
            if (c == null) continue;
            if (!c.gameObject.activeInHierarchy) continue;
            if (c.gameObject.tag == "Player")
            {
                float d = Vector3.Distance(c.transform.position, selfPosition);
                if (d < minTaggedDist)
                {
                    minTaggedDist = d;
                    nearestTagged = c;
                }
            }
        }
        if (nearestTagged != null)
        {
            Debug.Log($"CombatCharacterManager: Prefer tagged Player -> {nearestTagged.gameObject.name} dist={minTaggedDist:F2}");
            return nearestTagged;
        }

        return FindNearestCharacter(playerCampCharacters, selfPosition);
    }

    public CharacterAttributes FindNearestEnemyCampCharacter(Vector3 selfPosition)
    {
        return FindNearestCharacter(enemyCampCharacters, selfPosition);
    }

    private CharacterAttributes FindNearestCharacter(List<CharacterAttributes> characters, Vector3 selfPosition)
    {
        if (characters == null || characters.Count == 0)
        {
            Debug.Log("CombatCharacterManager: FindNearestCharacter called but list is empty/null.");
            return null;
        }

        float minDistance = float.MaxValue;
        CharacterAttributes nearestCharacter = null;

        // Log all candidates for diagnostics
        string listSummary = "";
        foreach (CharacterAttributes character in characters)
        {
            float dist = Vector3.Distance(character.transform.position, selfPosition);
            listSummary += $"[{character.gameObject.name}:{dist:F2}] ";
            if (dist < minDistance)
            {
                minDistance = dist;
                nearestCharacter = character;
            }
        }
        Debug.Log($"CombatCharacterManager: FindNearestCharacter candidates={characters.Count}, selfPos={selfPosition}, list={listSummary}, nearest={(nearestCharacter!=null?nearestCharacter.gameObject.name:"null")} dist={minDistance:F2}");

        return nearestCharacter;
    }

    /// <summary>
    /// Scan scene for existing CharacterAttributes and ensure registry lists are up to date.
    /// Useful for cases where persistent objects (DontDestroyOnLoad) exist across scene loads.
    /// </summary>
    private void RebuildRegistry()
    {
        noneCampCharacters.Clear();
        playerCampCharacters.Clear();
        enemyCampCharacters.Clear();

        CharacterAttributes[] all = GameObject.FindObjectsOfType<CharacterAttributes>();
        Debug.Log($"CombatCharacterManager: Rebuilding registry, found {all.Length} CharacterAttributes.");
        foreach (var c in all)
        {
            if (c != null)
            {
                Register(c);
            }
        }
    }
}
