﻿using System.Collections;
using UnityEngine;

public class Health : MonoBehaviour
{
    private UIManager uiManager;
    private PlayerMovement movement;

    private Vector3 lastHit;

    [SerializeField] float maxHealth = 100f;
    [SerializeField] float regenPerSec = 2f;
    [SerializeField] float damageReduPercentage = 50f;
    private float health;

    private bool regenerating;
    private bool dead = false;

    #region  Getters and Setters

    public float GetMaxHealt() 
    {
        return maxHealth;
    }
    public float GetCurrentHealth()
    {
        return health;
    }

    public bool GetDead()
    {
        return dead;
    }

    #endregion

    void Awake()
    {
        movement = GetComponent<KnightPlayerMovement>();
        uiManager = UIManager.GetInstance();
    }

    void Start()
    {
        health = maxHealth;

        if(GetComponentInChildren<NinjaUI>() != null)
        {
            uiManager.SetMaxHealth(maxHealth, GetComponentInChildren<NinjaUI>());
        }
        else if(GetComponentInChildren<KnightUI>() != null)
        {
            uiManager.SetMaxHealth(maxHealth, null, GetComponentInChildren<KnightUI>());
        }
    }

    //Updates the Health Bar.
    void Update()
    {
        if(GetComponentInChildren<NinjaUI>() != null)
        {
            uiManager.SetHealth(health, GetComponentInChildren<NinjaUI>());
        }
        else if(GetComponentInChildren<KnightUI>() != null)
        {
            uiManager.SetHealth(health, null, GetComponentInChildren<KnightUI>());
        }
    }

    private void FixedUpdate()
    {
        health = Mathf.Clamp(health, 0f, maxHealth);

        if(!dead)
        {
            Regenerate();
        }
    }

    //The Players health will go down when he takes damage.
    public void TakeDamage(float damage)
    {
        if(movement != null && movement.GetScoping())
        {
            health -= damage * (damageReduPercentage / 100);
        }
        else
        {
            health -= damage;
        }

        regenerating = false;

        if(health <= 0 && !dead)
        {
            Die();
        }

        StopAllCoroutines();
        StartCoroutine(RegenCooldown());
    }

    IEnumerator RegenCooldown() 
    {
        yield return new WaitForSeconds(8f);

        regenerating = true;
    }

    public void Heal(float ammount) 
    {
        health += ammount;
    }

    void Die()
    {
        dead = true;

        NinjaUI ninjaUI = GetComponentInChildren<NinjaUI>();
        KnightUI knightUI = GetComponentInChildren<KnightUI>();

        uiManager.GameOver(ninjaUI, knightUI);
    }

    private void Regenerate() 
    {
        if(regenerating && health < maxHealth)
        {
            health += regenPerSec * Time.fixedDeltaTime;
        }
        else if(health == maxHealth)
        {
            regenerating = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Medkit medkit = other.GetComponent<Medkit>();
        
        if (medkit && health < maxHealth && !dead)
        {
            Heal(medkit.GetHealAmmount());
            GameObject.Destroy(other.gameObject);
        }
    }

    void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.tag.Equals("Ammo"))
        {
            lastHit = transform.localPosition - other.transform.localPosition;
        }
    }
}
