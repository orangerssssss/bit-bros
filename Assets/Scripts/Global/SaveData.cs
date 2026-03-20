using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    public PlayerSaveData playerSaveData;
    public InventorySaveData inventorySaveData;
    public GameProcessSaveData gameProcessSaveData;

    public SaveData()
    {
        playerSaveData = new PlayerSaveData();
        inventorySaveData = new InventorySaveData();
        gameProcessSaveData = new GameProcessSaveData();
    }
}

[System.Serializable]
public class PlayerSaveData
{
    public int addedConstitution;
    public int addedStrength;
    public int addedIntelligence;
    public int attributePoints;

    public int level;
    public int experience;
}

[System.Serializable]
public class InventorySaveData
{
    public int coin;

    public int weaponID;
    public int armorID;

    public List<PackageItemData> materialItemDataList;
    public List<PackageItemData> propItemDataList;
    public List<PackageItemData> equipmentItemDataList;
}

[System.Serializable]
public class GameProcessSaveData
{
    public int storyProcess;
}

[System.Serializable]
public struct PackageItemData
{
    public int itemID;
    public int itemQuantity;

    public PackageItemData(int id, int quantity)
    {
        itemID = id;
        itemQuantity = quantity;
    }
}