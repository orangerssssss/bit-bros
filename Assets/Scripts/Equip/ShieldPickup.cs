using UnityEngine;

public class ShieldPickup : MonoBehaviour
{
    [Tooltip("Prefab of the shield that will be equipped on pickup.")]
    public GameObject shieldPrefab;

    [Tooltip("Destroy the pickup object after player picks it up.")]
    public bool destroyOnPickup = true;

    private void OnTriggerEnter(Collider other)
    {
        var equip = other.GetComponent<ShieldEquip>();
        if (equip != null)
        {
            equip.Equip(shieldPrefab);
            if (destroyOnPickup) Destroy(gameObject);
        }
    }
}
