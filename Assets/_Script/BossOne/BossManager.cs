using Mono.Cecil.Cil;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;


public enum BossState
{
    phaseOne,
    phaseTwo
}
public enum PhaseOne
{
    Detect,
    Smash,
    RoundAttack,
    CornShooting,
    Wind
}

public class BossManager : MonoBehaviour
{
    public Transform playerCharacter;

    public List<Stitch> stitches = new List<Stitch>();
    public int totalStitchCount = 0;

    public GameObject BossCharacter;
    private Vector3 BossForward;
    public Vector3 PlayerCharacter;

    //List of transforms that will record the position of the stitches
    ///public List<Transform> StitchLocation;

    //Stakes
    public GameObject stakePrefab;
    public Transform[] stakeSpawnPoints; // Stake 생성 위치 (5개)

    public List<Stake> stakes = new List<Stake>();
    public int activeStakes = 0;
    public float phase2Health = 300f;
    public CoreShootingManager coreShootingManager;

    public float[] coreExposureThresholds = { 0.75f, 0.60f, 0.45f, 0.30f };
    private int currentThresholdIndex = 0;

    private Dictionary<Stake, Coroutine> stakeRespawnCoroutines = new Dictionary<Stake, Coroutine>();

    //Enum (State of Phase One and Two)
    public BossState currentBossPhase = BossState.phaseOne;

    //Enum (States of Detect/AOE1/AOE2/AOE3)
    PhaseOne currentPhase = PhaseOne.Detect;

    //Health
    public float health;

    public float detectionCooldown = 2f;

    //Boss Stuff
    private Renderer _bossRenderer;
    private Material _bossMaterial;
    private Color _bossOriginalColor;

    public BossHealthBar healthBar;


    //Smash
    public GameObject smashIndicatorPrefab; // Prefab for the smash indicator
    public float smashRadius = 3f; //Radius for now, maybe turn into square?
    public float smashDamage = 20f; // will turn into just # of hit
    public float smashNoticeTime = 2f; //2 seconds notice time before smash

    private float _lastDetectionTime = 0f;
    private bool _isAttacking = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        BossForward = BossCharacter.transform.forward;


        totalStitchCount = stitches.Count;
        Debug.Log($"Phase 1 시작! Stitch 개수: {totalStitchCount}");

        _bossRenderer = BossCharacter.GetComponent<Renderer>();
        if (_bossRenderer != null)
        {
            _bossMaterial = _bossRenderer.material;
            _bossOriginalColor = _bossMaterial.color;
        }
        else
        {
            //   Debug.LogWarning("BossCharacter에 Renderer가 없습니다!");
        }

    }

    // Update is called once per frame
    void Update()
    {

        if (currentBossPhase == BossState.phaseOne)
        {
            UpdatePhaseOne();
        }
        else if (currentBossPhase == BossState.phaseTwo)
        {
            UpdatePhaseTwo();
        }

    }


    public void RegisterStitch(Stitch stitch)
    {
        if (!stitches.Contains(stitch))
        {
            stitches.Add(stitch);
            Debug.Log($"Stitch 등록됨: {stitch.gameObject.name}");
        }
    }

    public void RegisterStake(Stake stake)
    {
        if (!stakes.Contains(stake))
        {
            stakes.Add(stake);
            activeStakes++;
            Debug.Log($"Stake 등록: {stake.gameObject.name}");
        }
    }



    public void OnStitchDestroyed(Stitch stitch)
    {
        if (stitches.Contains(stitch))
        {
            stitches.Remove(stitch);
            Debug.Log($"남은 Stitch: {stitches.Count}/{totalStitchCount}");

            // 모든 Stitch 파괴됨 → Phase 2
            if (stitches.Count == 0)
            {
                StartCoroutine(TransitionToPhaseTwo());
            }
        }
    }
    public void OnStakeDestroyed(Stake stake)
    {
        activeStakes--;
        Debug.Log($"활성 Stake: {activeStakes}/5");
    }

    public void OnStakeRespawned(Stake stake)
    {
        activeStakes++;
        Debug.Log($"Stake 재생성! 활성: {activeStakes}/5");
    }

    public void ScheduleStakeRespawn(Stake stake, float delay)
    {
        Coroutine coroutine = StartCoroutine(RespawnStakeAfterDelay(stake, delay));
        stakeRespawnCoroutines[stake] = coroutine;
    }

    IEnumerator RespawnStakeAfterDelay(Stake stake, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (stake != null)
        {
            stake.Respawn();
        }

        stakeRespawnCoroutines.Remove(stake);
    }


    public void TakeDamagePhaseTwo(float baseDamage)
    {
        if (currentBossPhase != BossState.phaseTwo) return;

        float damageReduction = activeStakes * 0.2f;
        float actualDamage = baseDamage * (1f - damageReduction);

        phase2Health -= actualDamage;
        Debug.Log($"보스 데미지: {actualDamage} (감소: {damageReduction * 100}%) | 체력: {phase2Health}");

        // 하얀색 깜빡이기 추가!
        OnHit();

        CheckCoreExposure();

        if (phase2Health <= 0)
        {
            Die();
        }
    }

    void CheckCoreExposure()
    {
        if (currentThresholdIndex >= coreExposureThresholds.Length) return;

        float healthPercent = phase2Health / 300f; // 최대 체력 기준

        if (healthPercent <= coreExposureThresholds[currentThresholdIndex])
        {
            Debug.Log($"코어 노출 구간 도달! ({healthPercent * 100}%)");
            currentThresholdIndex++;

            // 코어 쏘기 모드 시작
            if (coreShootingManager != null)
            {
                HideBoss();
                coreShootingManager.ActivateCoreMode();
            }
        }
    }

    public void OnCoreShootingSuccess()
    {
        Debug.Log("코어 공격 성공! 100 데미지!");
        phase2Health -= 100f; // 큰 데미지

        if (phase2Health <= 0)
        {
            Die();
        }
    }

    public void OnCoreShootingFail()
    {
        phase2Health += 50f;
        phase2Health = Mathf.Min(phase2Health, 300f); // 최대 체력 제한
    }

    IEnumerator TransitionToPhaseTwo()
    {
        Debug.Log("모든 Stitch 파괴! Phase 2로 전환!");
        _isAttacking = true;

        yield return new WaitForSeconds(2f);

        currentBossPhase = BossState.phaseTwo;
        currentPhase = PhaseOne.Detect;
        _isAttacking = false;

        Debug.Log("Phase 2 시작!");
        if (healthBar != null)
        {
            healthBar.Show();
        }

        SpawnStakes();
    }

    void SpawnStakes()
    {
        if (stakePrefab == null || stakeSpawnPoints == null || stakeSpawnPoints.Length == 0)
        {
            Debug.LogError("Stake Prefab 또는 Spawn Points가 설정되지 않았습니다!");
            return;
        }

        Debug.Log($"{stakeSpawnPoints.Length}개의 Stake 생성!");

        foreach (Transform spawnPoint in stakeSpawnPoints)
        {
            if (spawnPoint != null)
            {
                GameObject stakeObj = Instantiate(stakePrefab, spawnPoint.position, spawnPoint.rotation);
                Stake stake = stakeObj.GetComponent<Stake>();

                if (stake != null)
                {
                    // BossManager 자동 할당
                    stake.bossManager = this;
                }
            }
        }
    }

    public void UpdatePhaseOne()
    {
        switch (currentPhase)
        {
            case PhaseOne.Detect:
                Detect();
                break;
            case PhaseOne.Smash:
                if (!_isAttacking)
                {
                    StartCoroutine(Smash());
                    _isAttacking = true;
                }
                break;
            case PhaseOne.RoundAttack:
                if (!_isAttacking)
                {
                    StartCoroutine(RoundAttack());
                    _isAttacking = true;
                }
                break;

            case PhaseOne.CornShooting:
                break;
            case PhaseOne.Wind:

                break;
            default:

                break;
        }
        // Handle Phase One specific logic here
    }

    public void UpdatePhaseTwo()
    {

    }

    public void Detect()
    {
        if (playerCharacter == null) return;
        if (Time.time - _lastDetectionTime < detectionCooldown) return;

        Vector3 bossForward = transform.forward;
        Vector3 directionToPlayer = (playerCharacter.position - transform.position).normalized;
        directionToPlayer.y = 0;

        currentPhase = PhaseOne.Smash;

        /*
        //Dot Product for Where boss is looking / Character position
        float dotProduct = Vector3.Dot(bossForward, directionToPlayer);


        if (dotProduct < -0.3f)
        {
            //if the dot product is negative == Round Attack
            currentPhase = PhaseOne.RoundAttack;
        }
        else if (dotProduct < 0.3f)
        {
            //If the Dot Product is less than some amount, then it will be smach,
            currentPhase = PhaseOne.Smash;
        }
        else if (dotProduct >= 0.3f)
        {
            //if the dot product is large, CornShooting
            currentPhase = PhaseOne.CornShooting;
        }

        if (Random.value < 0.1f)
        {
            currentPhase = PhaseOne.Wind;
        }

        _lastDetectionTime = Time.time;
        */
    }

    public IEnumerator Smash()
    {
        _isAttacking = true;
        Debug.Log("Smash Attack 준비!");

        Vector3 targetPosition = playerCharacter.position;
        targetPosition.y = 1.13f;

        GameObject indicator = Instantiate(smashIndicatorPrefab, targetPosition, Quaternion.Euler(0, 90, 0));
        indicator.transform.localScale = new Vector3(smashRadius * 2, 2.3f, smashRadius * 2);

        StartCoroutine(BlinkIndicator(indicator, smashNoticeTime));

        yield return new WaitForSeconds(smashNoticeTime);

        Debug.Log("Smash Attack 실행!");
        Destroy(indicator);

        float distanceToPlayer = Vector3.Distance(
            new Vector3(playerCharacter.position.x, 0, playerCharacter.position.z),
            new Vector3(targetPosition.x, 0, targetPosition.z)
        );

        if (distanceToPlayer <= smashRadius)
        {
            Debug.Log($"플레이어 맞음! 데미지: {smashDamage}");

            // 카메라 흔들기!
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                CameraController camController = mainCam.GetComponent<CameraController>();
                CameraShake shake = mainCam.GetComponent<CameraShake>();

                if (shake != null)
                {
                    Debug.Log("카메라 흔들기 시작!");

                    // CameraController 잠깐 끄기!
                    if (camController != null)
                    {
                        camController.enabled = false;
                    }

                    // 흔들기
                    shake.Shake(0.2f, 0.5f); // 0.5초, 강도 0.5

                    // 0.5초 후 다시 켜기
                    StartCoroutine(ReenableCameraController(camController, 0.5f));
                }
            }
        }
        else
        {
            Debug.Log("플레이어가 범위 밖으로 피함!");
        }

        yield return new WaitForSeconds(0.5f);

        _isAttacking = false;
        currentPhase = PhaseOne.Detect;
    }

    // 새 함수 추가!
    IEnumerator ReenableCameraController(CameraController controller, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (controller != null)
        {
            controller.enabled = true;
            Debug.Log("CameraController 다시 활성화!");
        }
    }

    public IEnumerator RoundAttack()
    {
        yield return new WaitForSeconds(2f);
    }

    public IEnumerator BlinkIndicator(GameObject indicator, float duration)
    {
        float elapsed = 0f;
        Renderer renderer = indicator.GetComponent<Renderer>();

        while (elapsed < duration)
        {
            // 깜빡이는 효과
            float alpha = Mathf.PingPong(Time.time * 3f, 1f) * 0.7f + 0.3f;
            Color color = renderer.material.color;
            color.a = alpha;
            renderer.material.color = color;

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    public IEnumerator CoolDown()
    {
        yield return new WaitForSeconds(2f);
    }


    public void HideBoss()
    {
        if (_bossRenderer != null)
        {
            _bossRenderer.enabled = false;
        }
    }
    public void ShowBoss()
    {
        if (_bossRenderer != null)
        {
            _bossRenderer.enabled = true;
        }
    }

    public void OnHit()
    {
        if (_bossRenderer != null)
        {
            StartCoroutine(FlashWhite());
        }
    }
    IEnumerator FlashWhite()
    {
        _bossMaterial.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        _bossMaterial.color = _bossOriginalColor;
    }



    void Die()
    {
        Debug.Log("보스 사망!");

        // 체력바 숨기기
        if (healthBar != null)
        {
            healthBar.Hide();
        }

        Destroy(gameObject, 2f);
    }

}