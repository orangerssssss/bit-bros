using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelEndArea : MonoBehaviour
{
    public string nextSceneName;

    private void Start()
    {
        GameUIManager.Instance.levelEndPanel.GetComponent<Button>().onClick.AddListener(
            () => { SceneLoader.instance.LoadScene(nextSceneName, true); });
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerInputManager.Instance.CloseAllInput(true);
            GameUIManager.Instance.levelEndPanel.SetActive(true);
        }
    }
}
