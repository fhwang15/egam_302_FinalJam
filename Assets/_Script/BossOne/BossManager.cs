using Mono.Cecil.Cil;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Unity.VisualScripting;


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
    Swish,
    Wind
}

public class BossManager : MonoBehaviour
{
    public Transform playerCharacter;

    public List<Stitch> stitches = new List<Stitch>();
    public int totalStitchCount = 0;

    public Vector3 PlayerCharacter;

    //List of transforms that will record the position of the stitches
    ///public List<Transform> StitchLocation;

    [Header("Boss Rotation Setting")]
    //Boss Rotation towards the player character.
    public GameObject BossCharacter;
    private Vector3 BossForward;
    public float rotationSpeed = 2f;
    public bool rotateTowardsPlayer = true;

    [Header("Enum: Detect Settings")]
    public float detectDuration = 5f;
    public float attackCooldown = 2f;
    private float detectTimer = 0f;
    private Vector3 targetAttackPosition;
    private bool isDetecting = false;

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

    //Enum of attacks (States of Detect/AOE1/AOE2/AOE3)
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

        currentPhase = PhaseOne.Detect;

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

        //if the boss should rotate towards the player, and is not attacking at the moment (enum = detect)
        if (currentPhase == PhaseOne.Detect && playerCharacter != null && rotateTowardsPlayer)
        {
            RotateTowardsPlayer();
        }

    }


    void RotateTowardsPlayer()
    {
        Vector3 directionToPlayer = playerCharacter.position - transform.position;
        directionToPlayer.y = 0; // Y축 회전만

        if (directionToPlayer.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
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

    ///////////The Attacks and Phases///////////

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

            case PhaseOne.Swish:
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
        if (!isDetecting)
        {
            isDetecting = true;
            detectTimer = 0f;
        }

        //Rotates towards player over time for 5 seconds
        detectTimer += Time.deltaTime;

        // After 5 seconds, store the player's position and choose an attack
        if (detectTimer >= detectDuration)
        {
            targetAttackPosition = playerCharacter.position; 
            isDetecting = false;
            ChooseAttack(); 
        }
    }

    void ChooseAttack()
    {
        Vector3 bossForward = transform.forward;
        Vector3 directionToTarget = (targetAttackPosition - transform.position).normalized;
        float dotProduct = Vector3.Dot(bossForward, directionToTarget);

        // Dot Product로 공격 결정
        if (dotProduct < -0.3f)
        {
            currentPhase = PhaseOne.RoundAttack;
        }
        else if (dotProduct < 0.3f)
        {
            currentPhase = PhaseOne.Smash;
        }
        else
        {
            currentPhase = PhaseOne.Swish;
        }

        if (Random.value < 0.1f) currentPhase = PhaseOne.Wind;
    }

    //Smash Attack Coroutine
    public IEnumerator Smash()
    {
        _isAttacking = true;

        Vector3 targetPosition = playerCharacter.position;
        targetPosition.y = 1.13f;

        GameObject indicator = Instantiate(smashIndicatorPrefab, targetPosition, Quaternion.Euler(0, 90, 0));
        indicator.transform.localScale = new Vector3(smashRadius * 2, 2.3f, smashRadius * 2);

        StartCoroutine(BlinkIndicator(indicator, smashNoticeTime));

        yield return new WaitForSeconds(smashNoticeTime);

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

        yield return new WaitForSeconds(attackCooldown);
        _isAttacking = false;
        currentPhase = PhaseOne.Detect;
    }
    
    //Round Attack Coroutine
    public IEnumerator RoundAttack()
    {
        yield return new WaitForSeconds(2f);
        currentPhase = PhaseOne.Detect;
    }

    //Swish Attack Coroutine
    public IEnumerator Swish()
    {
        _isAttacking = true;
        Debug.Log("Swish Attack 준비!");

        // 플랫폼 중심 (원의 중심)
        Vector3 platformCenter = Vector3.zero; // 또는 실제 플랫폼 중심

        // 타겟 위치 (플레이어가 있던 곳)
        Vector3 targetPosition = targetAttackPosition;

        // 중심에서 타겟으로의 방향 (반지름 방향)
        Vector3 radialDirection = (targetPosition - platformCenter).normalized;
        radialDirection.y = 0;

        // 접선 방향 (원을 따라 회전한 방향)
        Vector3 tangentDirection = Vector3.Cross(Vector3.up, radialDirection).normalized;

        // 랜덤으로 왼쪽→오른쪽 또는 오른쪽→왼쪽
        bool leftToRight = Random.value > 0.5f;
        if (!leftToRight)
        {
            tangentDirection = -tangentDirection; // 방향 반전
        }

        Debug.Log($"Swish 방향: {(leftToRight ? "왼쪽→오른쪽" : "오른쪽→왼쪽")}");

        // 원호 장판 생성 (반원 모양)
        float arcRadius = 15f; // 원의 반지름
        float arcWidth = 5f; // 장판 너비

        GameObject swishIndicator = CreateSwishIndicator(platformCenter, radialDirection, tangentDirection, arcRadius, arcWidth);

        StartCoroutine(BlinkIndicator(swishIndicator, smashNoticeTime));

        yield return new WaitForSeconds(smashNoticeTime);

        Debug.Log("Swish Attack 실행!");

        // 플레이어가 장판 범위 안에 있는지 체크
        bool playerHit = CheckPlayerInSwishZone(platformCenter, radialDirection, tangentDirection, arcRadius, arcWidth);

        if (playerHit)
        {
            Debug.Log("플레이어 Swish 맞음!");

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                CameraShake shake = mainCam.GetComponent<CameraShake>();
                if (shake != null)
                {
                    shake.Shake(0.5f, 0.5f);
                }
            }
        }
        else
        {
            Debug.Log("플레이어가 Swish 피함!");
        }

        Destroy(swishIndicator);

        yield return new WaitForSeconds(0.5f);

        Debug.Log($"공격 완료! {attackCooldown}초 쿨타임...");
        yield return new WaitForSeconds(attackCooldown);

        _isAttacking = false;
        currentPhase = PhaseOne.Detect;
        Debug.Log("쿨타임 종료, Detect 모드로 전환");
    }


    GameObject CreateSwishIndicator(Vector3 center, Vector3 radialDir, Vector3 tangentDir, float radius, float width)
    {
        // 반원 장판 만들기
        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(indicator.GetComponent<Collider>()); // Collider 제거

        // 반투명 빨간색 Material
        Renderer renderer = indicator.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = new Color(1f, 0f, 0f, 0.5f); // 빨간색 반투명
        renderer.material = mat;

        // 타겟 위치 (반지름 방향으로 이동)
        Vector3 targetPos = center + radialDir * radius;
        targetPos.y = 1.13f;

        // 위치 설정 (원의 반 정도 위치)
        indicator.transform.position = targetPos;

        // 회전: 접선 방향으로 늘어나도록
        indicator.transform.rotation = Quaternion.LookRotation(Vector3.up, tangentDir);

        // 크기: 반원처럼 넓게
        indicator.transform.localScale = new Vector3(radius * 1.5f, width, 1f);

        return indicator;
    }

    bool CheckPlayerInSwishZone(Vector3 center, Vector3 radialDir, Vector3 tangentDir, float radius, float width)
    {
        if (playerCharacter == null) return false;

        Vector3 playerPos = playerCharacter.position;
        playerPos.y = 0;

        Vector3 centerFlat = center;
        centerFlat.y = 0;

        // 플레이어가 타겟 쪽 반원 안에 있는지 체크
        Vector3 toPlayer = playerPos - centerFlat;

        // 1. 반지름 체크
        float distanceFromCenter = toPlayer.magnitude;
        if (distanceFromCenter > radius + width || distanceFromCenter < radius - width)
        {
            return false; // 너무 멀거나 가까움
        }

        // 2. 각도 체크 (타겟 방향 ±90도 범위)
        float angleToPlayer = Vector3.SignedAngle(radialDir, toPlayer.normalized, Vector3.up);

        // 반원 범위 (-90도 ~ +90도)
        if (Mathf.Abs(angleToPlayer) <= 90f)
        {
            return true;
        }

        return false;
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
            Application.Quit();
        
    }

}