using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

/// <summary>
/// 跨场景游戏数据管理器
/// </summary>
public class DataManager : MonoBehaviour
{
    private static DataManager instance;// 单例
    public static DataManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<DataManager>();

                DontDestroyOnLoad(instance.gameObject);
                instance.DeserializeSaveData();
            }
            return instance;
        }
    }

    public ItemConfig itemConfig;// 物品配置表
    public SaveData saveData = new SaveData();

    public bool hasSave = false; // 是否有存在的存档文件，用于判断能否继续游戏
    public bool loadSave = false;// 是否读取存档文件，用于游戏中数据的加载

    private string saveFileName = "/ArthurGameData.dat";

    private void Awake()
    {
        if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void NewGame()
    {
        loadSave = false;
    }

    public void LoadGame()
    {
        if (hasSave) loadSave = true;
    }

    public void SaveGame()
    {
        // Player
        PlayerAttributes player = FindObjectOfType<PlayerAttributes>();

        saveData.playerSaveData.addedConstitution = player.attributesAddedPoints[0];
        saveData.playerSaveData.addedStrength = player.attributesAddedPoints[1];
        saveData.playerSaveData.addedIntelligence = player.attributesAddedPoints[2];
        saveData.playerSaveData.attributePoints = player.AttributePoints;

        saveData.playerSaveData.level = player.level;
        saveData.playerSaveData.experience = player.experience;

        // Inventory
        saveData.inventorySaveData.coin = InventoryManager.Instance.Coin;

        saveData.inventorySaveData.weaponID = InventoryManager.Instance.WeaponID;
        saveData.inventorySaveData.armorID = InventoryManager.Instance.ArmorID;

        saveData.inventorySaveData.materialItemDataList = InventoryManager.Instance.GetPackageItemDataList(ItemType.Material);
        saveData.inventorySaveData.propItemDataList = InventoryManager.Instance.GetPackageItemDataList(ItemType.Usable);
        saveData.inventorySaveData.equipmentItemDataList = InventoryManager.Instance.GetPackageItemDataList(ItemType.Weapon);

        // Process
        saveData.gameProcessSaveData.storyProcess = MainSceneStory.Instance.storyProcess;

        SerializeSaveData();

        hasSave = true;
    }

    private void DeserializeSaveData()
    {
        if (File.Exists(Application.persistentDataPath + saveFileName))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(Application.persistentDataPath + saveFileName, FileMode.Open);

            saveData = formatter.Deserialize(stream) as SaveData;

            stream.Close();

            hasSave = true;
        }
    }

    private void SerializeSaveData()
    {
        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream(Application.persistentDataPath + saveFileName, FileMode.Create);

        formatter.Serialize(stream, saveData);

        stream.Close();
    }
}
