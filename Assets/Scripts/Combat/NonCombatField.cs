using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NonCombatField : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<PlayerCombatController>(out PlayerCombatController player))
        {
            player.SetWeaponVisible(false);
            MainSceneStory.Instance.PlayVillageBGM();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<PlayerCombatController>(out PlayerCombatController player))
        {
            PlayerTirggerExit();
        }
    }

    public void PlayerTirggerExit()
    {
        PlayerInputManager.Instance.combatController.SetWeaponVisible(true);
        MainSceneStory.Instance.PlayFightBGM();
    }
}
