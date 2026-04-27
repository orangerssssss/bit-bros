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
        EnsureSaveDataInitialized();

        // Player
        PlayerAttributes player = FindObjectOfType<PlayerAttributes>();
        if (player != null)
        {
            if (player.attributesAddedPoints != null && player.attributesAddedPoints.Length >= 3)
            {
                saveData.playerSaveData.addedConstitution = player.attributesAddedPoints[0];
                saveData.playerSaveData.addedStrength = player.attributesAddedPoints[1];
                saveData.playerSaveData.addedIntelligence = player.attributesAddedPoints[2];
            }

            saveData.playerSaveData.attributePoints = player.AttributePoints;
            saveData.playerSaveData.level = player.level;
            saveData.playerSaveData.experience = player.experience;
        }

        // Inventory
        var inv = InventoryManager.Instance;
        if (inv != null)
        {
            saveData.inventorySaveData.coin = inv.Coin;
            saveData.inventorySaveData.weaponID = inv.WeaponID;
            saveData.inventorySaveData.armorID = inv.ArmorID;

            saveData.inventorySaveData.materialItemDataList = inv.GetPackageItemDataList(ItemType.Material);
            saveData.inventorySaveData.propItemDataList = inv.GetPackageItemDataList(ItemType.Usable);
            saveData.inventorySaveData.equipmentItemDataList = inv.GetPackageItemDataList(ItemType.Weapon);
        }

        // Process（兼容不同场景故事控制器）
        if (MainSceneStory.Instance != null)
        {
            saveData.gameProcessSaveData.storyProcess = MainSceneStory.Instance.storyProcess;
        }
        else if (VillageSceneStory.Instance != null)
        {
            saveData.gameProcessSaveData.storyProcess = VillageSceneStory.Instance.storyProcess;
        }
        else if (ImaginationSceneStory.Instance != null)
        {
            saveData.gameProcessSaveData.storyProcess = ImaginationSceneStory.Instance.storyProcess;
        }

        SerializeSaveData();

        hasSave = true;
    }

    private void EnsureSaveDataInitialized()
    {
        if (saveData == null) saveData = new SaveData();
        if (saveData.playerSaveData == null) saveData.playerSaveData = new PlayerSaveData();
        if (saveData.inventorySaveData == null) saveData.inventorySaveData = new InventorySaveData();
        if (saveData.gameProcessSaveData == null) saveData.gameProcessSaveData = new GameProcessSaveData();

        if (saveData.inventorySaveData.materialItemDataList == null)
            saveData.inventorySaveData.materialItemDataList = new List<PackageItemData>();
        if (saveData.inventorySaveData.propItemDataList == null)
            saveData.inventorySaveData.propItemDataList = new List<PackageItemData>();
        if (saveData.inventorySaveData.equipmentItemDataList == null)
            saveData.inventorySaveData.equipmentItemDataList = new List<PackageItemData>();
    }

    private void DeserializeSaveData()
    {
        if (File.Exists(Application.persistentDataPath + saveFileName))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(Application.persistentDataPath + saveFileName, FileMode.Open);

            saveData = formatter.Deserialize(stream) as SaveData;

            stream.Close();

            EnsureSaveDataInitialized();

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
