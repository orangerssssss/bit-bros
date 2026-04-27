using UnityEngine;

public class PlayerShield : MonoBehaviour
{
    [Header("Shield Settings")]
    [SerializeField] private float maxShieldHealth = 100f;
    [SerializeField] private float shieldRechargeRate = 15f;
    [SerializeField] private float shieldRechargeDelay = 2f;
    [SerializeField] private KeyCode shieldKey = KeyCode.Mouse1;
    
    [Header("References")]
    [SerializeField] private GameObject shieldVisual;
    
    private float currentShieldHealth;
    private bool isShielding;
    private float lastDamageTime;
    private Animator animator;
    
    // Public getter
    public bool IsShielding => isShielding;
    public bool ShieldActive => currentShieldHealth > 0;
    
    void Start()
    {
        animator = GetComponent<Animator>();
        currentShieldHealth = maxShieldHealth;
        
        if (shieldVisual != null)
            shieldVisual.SetActive(false);
    }
    
    void Update()
    {
        HandleShieldInput();
        HandleShieldRecharge();
    }
    
    void HandleShieldInput()
    {
        if (Input.GetKey(shieldKey) && currentShieldHealth > 0)
        {
            if (!isShielding)
                ActivateShield();
        }
        else
        {
            if (isShielding)
                DeactivateShield();
        }
    }
    
    void ActivateShield()
    {
        isShielding = true;
        
        if (shieldVisual != null)
            shieldVisual.SetActive(true);
        
        if (animator != null)
            animator.SetBool("isShielding", true);
    }
    
    void DeactivateShield()
    {
        isShielding = false;
        
        if (shieldVisual != null)
            shieldVisual.SetActive(false);
        
        if (animator != null)
            animator.SetBool("isShielding", false);
    }
    
    public void TakeShieldDamage(float damage)
    {
        if (!isShielding) return;
        
        currentShieldHealth -= damage;
        lastDamageTime = Time.time;
        
        // Kalau shield habis
        if (currentShieldHealth <= 0)
        {
            currentShieldHealth = 0;
            DeactivateShield();
        }
    }
    
    void HandleShieldRecharge()
    {
        if (!isShielding && currentShieldHealth < maxShieldHealth)
        {
            if (Time.time - lastDamageTime >= shieldRechargeDelay)
            {
                currentShieldHealth += shieldRechargeRate * Time.deltaTime;
                currentShieldHealth = Mathf.Clamp(currentShieldHealth, 0, maxShieldHealth);
            }
        }
    }
    
    public float GetShieldPercentage()
    {
        return currentShieldHealth / maxShieldHealth;
    }
}