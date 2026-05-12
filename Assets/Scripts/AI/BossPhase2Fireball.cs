using UnityEngine;

public class BossPhase2Fireball : MonoBehaviour
{
    private int damage;
    private float speed;
    private float impactRadius;
    private float groundY;
    private bool initialized;

    public void Init(int damageValue, float moveSpeed, float radius, float targetGroundY)
    {
        damage = Mathf.Max(1, damageValue);
        speed = Mathf.Max(0.1f, moveSpeed);
        impactRadius = Mathf.Max(0.1f, radius);
        groundY = targetGroundY;
        initialized = true;
    }

    private void Update()
    {
        if (!initialized)
        {
            return;
        }

        transform.position += Vector3.down * speed * Time.deltaTime;
        if (transform.position.y <= groundY)
        {
            Explode();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!initialized)
        {
            return;
        }

        if (other == null)
        {
            return;
        }

        CharacterAttributes attributes = other.GetComponent<CharacterAttributes>();
        if (attributes == null) attributes = other.GetComponentInParent<CharacterAttributes>();
        if (attributes != null && attributes.combatCamp == CombatCamp.Player)
        {
            Explode();
        }
    }

    private void Explode()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, impactRadius, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            CharacterAttributes attributes = hits[i].GetComponent<CharacterAttributes>();
            if (attributes == null) attributes = hits[i].GetComponentInParent<CharacterAttributes>();
            if (attributes != null && attributes.combatCamp == CombatCamp.Player)
            {
                attributes.GetAttack(damage, false);
            }
        }

        Destroy(gameObject);
    }
}
