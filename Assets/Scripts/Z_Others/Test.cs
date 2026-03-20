using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 无关项目的测试代码, 用来测试一些东西
/// </summary>
public class Test : MonoBehaviour
{
    private void Update()
    {


        // test
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            InventoryManager.Instance.AddItem(DataManager.Instance.itemConfig.FindItemByID(1001));
            InventoryManager.Instance.AddItem(DataManager.Instance.itemConfig.FindItemByID(1002));
        }
        //if (Input.GetKeyDown(KeyCode.Alpha2))
        //{
        //    GetItem(DataManager.instance.itemConfig.FindItemByID(2001));
        //    GetItem(DataManager.instance.itemConfig.FindItemByID(2002));
        //}
        //if (Input.GetKeyDown(KeyCode.Alpha3))
        //{
        //    //    GetItem(DataManager.instance.itemConfig.FindItemByID(3001));
        //    //    GetItem(DataManager.instance.itemConfig.FindItemByID(3002));
        //    InventoryManager.Instance.GetItem(DataManager.instance.itemConfig.FindItemByID(4001));
        //    //    GetItem(DataManager.instance.itemConfig.FindItemByID(4002));
        //    //    GetItem(DataManager.instance.itemConfig.FindItemByID(4003));
        //    //    GetItem(DataManager.instance.itemConfig.FindItemByID(4004));
        //}

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            InventoryManager.Instance.AddItem(DataManager.Instance.itemConfig.FindItemByID(4001));
            InventoryManager.Instance.AddItem(DataManager.Instance.itemConfig.FindItemByID(4002));
            InventoryManager.Instance.AddItem(DataManager.Instance.itemConfig.FindItemByID(4003));
            InventoryManager.Instance.AddItem(DataManager.Instance.itemConfig.FindItemByID(3001));
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            GameUIManager.Instance.messageTip.ShowTip("背包已满");
        }

        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            GameObject.FindObjectOfType<PlayerAttributes>().GetAttack(1000, true);
        }
    }
}
