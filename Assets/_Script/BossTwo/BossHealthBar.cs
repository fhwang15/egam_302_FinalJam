using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class BossHealthBar : MonoBehaviour
{
    public Slider healthSlider;
    public TextMeshProUGUI healthText; // 선택사항
    public BossManager bossManager;

    public float maxHealth = 300f;

    void Start()
    {
        if (bossManager == null)
        {
            bossManager = FindAnyObjectByType<BossManager>();
        }

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = maxHealth;
        }

        // Phase 1에서는 숨기기
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (bossManager == null)
        {
            return; // 보스 없으면 아무것도 안 함
        }

        if (bossManager.currentBossPhase == BossState.phaseTwo)
        {
            UpdateHealthBar(bossManager.phase2Health);
        }
    }

    public void UpdateHealthBar(float currentHealth)
    {
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }

        if (healthText != null)
        {
            healthText.text = $"Boss: {currentHealth:F0} / {maxHealth}";
        }
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}