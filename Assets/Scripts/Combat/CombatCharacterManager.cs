using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    }


    private List<CharacterAttributes> noneCampCharacters = new List<CharacterAttributes>();
    private List<CharacterAttributes> playerCampCharacters = new List<CharacterAttributes>();
    private List<CharacterAttributes> enemyCampCharacters = new List<CharacterAttributes>();

    public void Register(CharacterAttributes attributes)
    {
        if (attributes == null) return;

        if (attributes.combatCamp == CombatCamp.None && !noneCampCharacters.Contains(attributes))
            noneCampCharacters.Add(attributes);
        if (attributes.combatCamp == CombatCamp.Player && !playerCampCharacters.Contains(attributes))
            playerCampCharacters.Add(attributes);
        if (attributes.combatCamp == CombatCamp.Enemy && !enemyCampCharacters.Contains(attributes))
            enemyCampCharacters.Add(attributes);
    }

    public void Unregister(CharacterAttributes attributes)
    {
        if (attributes == null) return;

        if (attributes.combatCamp == CombatCamp.None && noneCampCharacters.Contains(attributes))
            noneCampCharacters.Remove(attributes);
        if (attributes.combatCamp == CombatCamp.Player && playerCampCharacters.Contains(attributes))
            playerCampCharacters.Remove(attributes);
        if (attributes.combatCamp == CombatCamp.Enemy && enemyCampCharacters.Contains(attributes))
            enemyCampCharacters.Remove(attributes);
    }

    public CharacterAttributes FindNearestNoneCampCharacter(Vector3 selfPosition)
    {
        return FindNearestCharacter(noneCampCharacters, selfPosition);
    }

    public CharacterAttributes FindNearestPlayerCampCharacter(Vector3 selfPosition)
    {
        return FindNearestCharacter(playerCampCharacters, selfPosition);
    }

    public CharacterAttributes FindNearestEnemyCampCharacter(Vector3 selfPosition)
    {
        return FindNearestCharacter(enemyCampCharacters, selfPosition);
    }

    private CharacterAttributes FindNearestCharacter(List<CharacterAttributes> characters, Vector3 selfPosition)
    {
        if (characters == null || characters.Count == 0) return null;

        float minDistance = float.MaxValue;
        CharacterAttributes nearestCharacter = null;

        foreach (CharacterAttributes character in characters)
        {
            float dist = Vector3.Distance(character.transform.position, selfPosition);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearestCharacter = character;
            }
        }

        return nearestCharacter;
    }
}
