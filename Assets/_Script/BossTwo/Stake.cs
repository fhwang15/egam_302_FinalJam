using UnityEngine;

public class Stake : MonoBehaviour
{
    public float maxHealth = 50f;
    public float health;
    public BossManager bossManager;
    public StitchHealthBar healthBar;
    public float respawnTime = 15f;

    private bool isDead = false;
    private Renderer _renderer;
    private Material _material;
    private Color _originalColor;
    private Vector3 _spawnPosition;

    void Start()
    {
        health = maxHealth;
        _spawnPosition = transform.position;

        _renderer = GetComponent<Renderer>();
        if (_renderer != null)
        {
            _material = _renderer.material;
            _originalColor = _material.color;
        }

        if (bossManager == null)
        {
            bossManager = FindFirstObjectByType<BossManager>();
        }

        if (bossManager != null)
        {
            bossManager.RegisterStake(this);
        }

        if (healthBar != null)
        {
            healthBar.SetMaxHealth(maxHealth);
            healthBar.SetHealth(health);
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        health -= damage;

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
        Debug.Log($"{gameObject.name} 파괴! {respawnTime}초 후 재생성");

        if (bossManager != null)
        {
            bossManager.OnStakeDestroyed(this);
        }

        gameObject.SetActive(false);

        if (bossManager != null)
        {
            bossManager.ScheduleStakeRespawn(this, respawnTime);
        }
    }

    public void Respawn()
    {
        Debug.Log($"{gameObject.name} 재생성!");

        isDead = false;
        health = maxHealth;

        transform.position = _spawnPosition;
        gameObject.SetActive(true);

        if (healthBar != null)
        {
            healthBar.SetHealth(health);
        }

        if (bossManager != null)
        {
            bossManager.OnStakeRespawned(this);
        }
    }

    public void OnHit()
    {
        if (_renderer != null && !isDead)
        {
            StartCoroutine(FlashWhite());
        }
    }

    System.Collections.IEnumerator FlashWhite()
    {
        _material.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        _material.color = _originalColor;
    }
}