using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class PlayerCharBattleController : MonoBehaviour
{
    [Header("Health")]
    public int currentHealth;
    public int maxPlayerHealth;

    [Header("Health Sprites")]
    public Sprite emptyHeart;
    public Sprite halfHeart;
    public Sprite fullHeart;
    public Image[] Hearts;

    [Header("Visuals")]
    private MeshRenderer playerRenderer;
    private Material playerMaterial;
    private Color originalColor;


    [Header("Invincible after hit")]
    public float invincibleTime;
    private bool isInvincible;

    [Header("Shooting")]
    public Transform bossLocation;
    public Transform firePoint;
    public GameObject BulletPrefab;
    public float bulletSpeed;

    //Whenever Player Clicks, they will instantiate bullet.

    private void Start()
    {
        currentHealth = maxPlayerHealth * 2;
    }


    public void Shooting()
    { 
        GameObject bullet = Instantiate(BulletPrefab, firePoint.position, firePoint.rotation);
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        
        if(bulletScript != null)
        {
            // let's shoot bullets left/right based on the camera.
            Camera cam = Camera.main;
            Vector3 camRight = cam.transform.right;

            float howSimilarIsFireForwardToCameraRight = Vector3.Dot(camRight, firePoint.forward);

            bulletScript.Initialize(camRight * Mathf.Sign(howSimilarIsFireForwardToCameraRight), bulletSpeed);
        }


    }

    // Update is called once per frame
    void Update()
    {

        UpdateHealthUI();
        for (int i = 0; i < Hearts.Length; i++)
        {
            if (i < maxPlayerHealth)
            {
                Hearts[i].enabled = true;
            }else
            {
                Hearts[i].enabled = false;
            }
        }

        if(Input.GetMouseButtonDown(0)) //Left Click
        {
            Shooting();
        }
    }

    void UpdateHealthUI()
    {
        int fullHearts = currentHealth / 2;
        bool hasHalfHeart = (currentHealth % 2) == 1;

        for (int i = 0; i < Hearts.Length; i++)
        {
            if (i < maxPlayerHealth)
            {
                Hearts[i].enabled = true;

                if (i < fullHearts)
                {
                    // 꽉 찬 하트
                    Hearts[i].sprite = fullHeart;
                }
                else if (i == fullHearts && hasHalfHeart)
                {
                    // 반 하트
                    Hearts[i].sprite = halfHeart;
                }
                else
                {
                    // 빈 하트
                    Hearts[i].sprite = emptyHeart;
                }
            }
            else
            {
                Hearts[i].enabled = false;
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (isInvincible) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        Debug.Log($"플레이어 피격! 남은 체력: {currentHealth}/{maxPlayerHealth * 2}");

        UpdateHealthUI();

        // 무적 시간 + 깜빡임
        StartCoroutine(InvincibilityFlash());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public IEnumerator InvincibilityFlash()
    {
        isInvincible = true;
        float elapsed = 0f;

        while (elapsed < invincibleTime)
        {
            // 깜빡임 (빨간색)
            if (playerRenderer != null)
            {
                float t = Mathf.PingPong(elapsed * 10f, 1f);
                playerMaterial.color = Color.Lerp(originalColor, Color.red, t);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 원래 색으로
        if (playerRenderer != null)
        {
            playerMaterial.color = originalColor;
        }

        isInvincible = false;
    }

    void Die()
    {
        Debug.Log("플레이어 사망!");

        // 게임 오버 처리
        // Time.timeScale = 0f; // 일시정지
        // GameOverUI.SetActive(true);
    }

}
