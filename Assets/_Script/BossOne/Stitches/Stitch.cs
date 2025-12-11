using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class Stitch : MonoBehaviour
{
    //Stitch Health
    public float maxHealth = 30f;
    public float health;
    public BossManager bossManager; //Ref to Boss
    public StitchHealthBar healthBar; // Reference to the health bar UI

    private bool isDead = false;

    //When Hit, Flash White (Red?)
    private Renderer _renderer;
    private Material _material;
    private Color _originalColor;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        health = maxHealth;

        _renderer = GetComponent<Renderer>();
        if (_renderer != null)
        {
            _material = _renderer.material;
            _originalColor = _material.color;
        }

        if (bossManager == null)
        {
            bossManager = FindAnyObjectByType<BossManager>();
        }

        // 보스에게 자신을 등록
        if (bossManager != null)
        {
            bossManager.RegisterStitch(this);
        }

        if(healthBar != null)
        {
            healthBar.SetMaxHealth(maxHealth);
        }

    }
    public void TakeDamage(float damage)
    {
        if (isDead) return;

        health -= damage;
        Debug.Log($"{gameObject.name} 체력: {health}");

        if (healthBar != null)
        {
            healthBar.SetHealth(health);
        }

        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log($"{gameObject.name} 파괴됨!");

        // Hide it with Stitch
        if (healthBar != null)
        {
            healthBar.gameObject.SetActive(false);
        }

        // Let the boss know
        if (bossManager != null)
        {
            bossManager.OnStitchDestroyed(this);
        }

        // VFX on getting it destroyed

        Destroy(gameObject, 0.1f);
    }

    // Visual feedback when hit
    public void OnHit()
    {
        
        StartCoroutine(FlashRed());
    }

    IEnumerator FlashRed()
    {
        // White
        _material.color = Color.white;

        yield return new WaitForSeconds(0.1f);

        // Original
        _material.color = _originalColor;
    }
}
