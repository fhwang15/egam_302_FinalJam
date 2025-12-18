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
    MeteorAttack
}

public class BossManager : MonoBehaviour
{
    public Transform playerCharacter;

    public List<Stitch> stitches = new List<Stitch>();
    public int totalStitchCount = 0;

    public Vector3 PlayerCharacter;

    [Header("Background Music")]
    public AudioClip phase1Music;
    public AudioClip phase2Music;
    private AudioSource musicSource;

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


    [Header("Enum: Smash Attack Settings")]
    public GameObject smashIndicatorPrefab; // Prefab for the smash indicator
    public float smashRadius = 3f; //Radius for now, maybe turn into square?
    public float smashDamage = 20f; // will turn into just # of hit
    public float smashNoticeTime = 2f; //2 seconds notice time before smash

    [Header("Enum: Swish Attack Settings")]
    public GameObject swishIndicatorPrefab; // Prefab for the swish indicator
    public GameObject swishVFXPrefab;
    public float swishLength; //Jangpan length (left to right)
    public float swishWidth; // Jangpan width (front to back)
    public float swishDistance; // How far the Jangpan is from the center of the platform

    [Header("Enum: Round Attack Settings")]
    public GameObject roundAttackVFXPrefab;
    public GameObject attackOrbPrefab; // attackOrbPrefab
    public int orbCount = 12; // 구체 개수 (원 둘레)
    public float orbSpeed = 5f; // 구체 속도
    public float orbDamage = 1f; // 데미지
    public float orbLifetime = 5f; // 구체 지속 시간
    public float roundAttackWarningTime = 1f;

    [Header("Meteor Attack")]
    public GameObject meteorPrefab; // 떨어지는 Orb
    public GameObject meteorIndicatorPrefab; // 경고 장판
    public int meteorCount = 5;
    public float meteorSpawnHeight = 15f; // 시작 높이
    public float meteorFallSpeed = 10f; // 떨어지는 속도
    public float meteorWarningTime = 1f; // 경고 시간
    public float stageRadius = 12f; // Stage 크기
    public float bossExclusionRadius = 5f; // 보스 주변 제외

    [Header("Stakes")]
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
    public PhaseOne currentPhase = PhaseOne.Detect;

    //Health
    public float health;

    public float detectionCooldown = 2f;

    //Boss Stuff
    private Renderer _bossRenderer;
    private Material _bossMaterial;
    private Color _bossOriginalColor;

    public BossHealthBar healthBar;
    private bool _isAttacking = false;

    public GameObject phaseTwoTransitionVFX;


    [Header("Sound Effects")]
    public AudioClip warningSound; // 경고음 (이명 소리)
    public AudioClip attackSound; // 공격음
    private AudioSource sfxSource;

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

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.volume = 0.5f;

        PlayPhase1Music();

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

    ////Music////
    void PlayPhase1Music()
{
    if (phase1Music != null && musicSource != null)
    {
        musicSource.clip = phase1Music;
        musicSource.Play();
        Debug.Log("Phase 1 음악 재생");
    }
}

    void PlayPhase2Music()
    {
        if (phase2Music != null && musicSource != null)
        {
            musicSource.clip = phase2Music;
            musicSource.Play();
            Debug.Log("Phase 2 음악 재생");
        }
    }

    IEnumerator FadeOutMusic(float duration)
    {
        if (musicSource == null) yield break;

        float startVolume = musicSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        musicSource.volume = 0f;
    }

    IEnumerator FadeInMusic(float duration)
    {
        if (musicSource == null) yield break;

        float targetVolume = 0.5f; // 원하는 볼륨
        float elapsed = 0f;

        while (elapsed < duration)
        {
            musicSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        musicSource.volume = targetVolume;
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
        Debug.Log("=== Phase 2 전환! ===");
        _isAttacking = true;

        StartCoroutine(FadeOutMusic(1f));

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
                shake.Shake(2f, 3f); // 0.5초, 강도 0.5

                StartCoroutine(ReenableCameraController(camController, 3f));
            }
        }

        // 보스 빨갛게
        StartCoroutine(FlashRed(2f));

        // VFX
        if (roundAttackVFXPrefab != null)
        {
            GameObject vfx = Instantiate(phaseTwoTransitionVFX, transform.position, Quaternion.identity);
            vfx.transform.localScale = Vector3.one * 3f;
            Destroy(vfx, 3f);
        }

        yield return new WaitForSeconds(2f);

        // Phase 2 시작
        currentBossPhase = BossState.phaseTwo;
        currentPhase = PhaseOne.Detect;
        _isAttacking = false;

        PlayPhase2Music();
        StartCoroutine(FadeInMusic(1f));

        if (healthBar != null) healthBar.Show();
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
                if (!_isAttacking)
                {
                    StartCoroutine(Swish());
                    _isAttacking = true;
                }
                break;

            case PhaseOne.MeteorAttack:
                if (!_isAttacking)
                {
                    currentPhase = PhaseOne.Smash;
                    _isAttacking = true;
                }
                break;
            default:

                break;
        }
        // Handle Phase One specific logic here
    }

    public void UpdatePhaseTwo()
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
                    StartCoroutine(RoundAttack()); // 2파동!
                    _isAttacking = true;
                }
                break;
            case PhaseOne.Swish:
                if (!_isAttacking)
                {
                    StartCoroutine(Swish());
                    _isAttacking = true;
                }
                break;

                case PhaseOne.MeteorAttack:
                   if (!_isAttacking)
                    {
                        StartCoroutine(MeteorAttack());
                        _isAttacking = true;
                    }
                
                break;
            default:
                break;
        }
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
            currentPhase = PhaseOne.Swish;
        }
        else
        {
            currentPhase = PhaseOne.Smash;
        }

        if (Random.value < 0.1f)
        {
            if (currentBossPhase == BossState.phaseOne)
            {
                currentPhase = PhaseOne.RoundAttack;
            }
            else
            {
                currentPhase = PhaseOne.MeteorAttack;
            }
        }
    }

    //Smash Attack Coroutine
    public IEnumerator Smash()
    {
        _isAttacking = true;

        Vector3 targetPosition = playerCharacter.position;
        targetPosition.y = 1.13f;

        GameObject indicator = Instantiate(smashIndicatorPrefab, targetPosition, Quaternion.Euler(0, 90, 0));
        indicator.transform.localScale = new Vector3(smashRadius * 2, 2.3f, smashRadius * 2);

        yield return new WaitForSeconds(smashNoticeTime);

        Destroy(indicator);

        float distanceToPlayer = Vector3.Distance(
            new Vector3(playerCharacter.position.x, 0, playerCharacter.position.z),
            new Vector3(targetPosition.x, 0, targetPosition.z)
        );

        if (distanceToPlayer <= smashRadius)
        {
            //Player gets hit

            PlayerCharBattleController battleController = playerCharacter.GetComponent<PlayerCharBattleController>();
            battleController.TakeDamage(1);

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
        _isAttacking = true;
        Debug.Log("Round Attack 시작!");

        GameObject warningCircle = Instantiate(swishIndicatorPrefab);



        if (roundAttackVFXPrefab != null)
        {
            Vector3 vfxPos = transform.position;
            vfxPos.y = 1.5f;

            GameObject warningVFX = Instantiate(roundAttackVFXPrefab, vfxPos, Quaternion.identity);
            warningVFX.transform.localScale = Vector3.one * 30f;

            Destroy(warningVFX, roundAttackWarningTime + 0.5f);
        }

        yield return new WaitForSeconds(roundAttackWarningTime);

        Vector3 circlePos = transform.position;
        circlePos.y = 2.9f;
        warningCircle.transform.position = circlePos;
        warningCircle.transform.localScale = new Vector3(59f, 0.3f, 59f); // 넓은 원
        yield return new WaitForSeconds(1f);
        Destroy(warningCircle);

        // 구체 발사!
        SpawnAttackOrbs();

        yield return new WaitForSeconds(2f);
        yield return new WaitForSeconds(attackCooldown);

        _isAttacking = false;
        currentPhase = PhaseOne.Detect;
    }

    void SpawnAttackOrbs()
    {
        if (attackOrbPrefab == null)
        {
            Debug.LogError("attackOrbPrefab이 할당되지 않았습니다!");
            return;
        }

        Vector3 bossPos = transform.position;
        bossPos.y = 3f; // 구체 높이

        // 360도 원형으로 구체 생성
        float angleStep = 360f / orbCount;

        for (int i = 0; i < orbCount; i++)
        {
            float angle = i * angleStep;
            float radian = angle * Mathf.Deg2Rad;

            // 방향 벡터
            Vector3 direction = new Vector3(Mathf.Cos(radian), 0, Mathf.Sin(radian));

            // 구체 생성 위치 (보스 중심)
            Vector3 spawnPos = bossPos;

            // 구체 생성
            GameObject orb = Instantiate(attackOrbPrefab, spawnPos, Quaternion.identity);

            // 구체에 스크립트 추가
            AttackOrb orbScript = orb.GetComponent<AttackOrb>();
            if (orbScript != null)
            {
                orbScript.Initialize(direction, orbSpeed, orbDamage, orbLifetime);
            }
            else
            {
                // 스크립트 없으면 수동으로 이동
                StartCoroutine(MoveOrb(orb, direction, orbSpeed, orbLifetime));
            }
        }

        Debug.Log($"{orbCount}개의 공격 구체 발사!");
    }

    IEnumerator MoveOrb(GameObject orb, Vector3 direction, float speed, float lifetime)
    {
        float elapsed = 0f;

        while (elapsed < lifetime && orb != null)
        {
            orb.transform.position += direction * speed * Time.deltaTime;
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (orb != null)
        {
            Destroy(orb);
        }
    }


    //Swish Attack Coroutine
    public IEnumerator Swish()
    {
        _isAttacking = true;
        Debug.Log("=== Swish Attack 시작! ===");

        Vector3 platformCenter = Vector3.zero;
        Vector3 targetPosition = targetAttackPosition;

        Vector3 radialDirection = (targetPosition - platformCenter).normalized;
        radialDirection.y = 0;

        Vector3 tangentDirection = Vector3.Cross(Vector3.up, radialDirection).normalized;

        bool leftToRight = Random.value > 0.5f;
        if (!leftToRight)
        {
            tangentDirection = -tangentDirection;
        }

        Vector3 swishPos = platformCenter + radialDirection * swishDistance;
        swishPos.y = 2.20f;
        float angle = Mathf.Atan2(tangentDirection.x, tangentDirection.z) * Mathf.Rad2Deg;

        GameObject warningIndicator = Instantiate(swishIndicatorPrefab, swishPos, Quaternion.Euler(0, angle, 0));

        warningIndicator.transform.localScale = new Vector3(swishLength, 0.2f, swishWidth);

        Collider col = warningIndicator.GetComponent<Collider>();
        if (col != null)
        {
            Destroy(col);
        }

        StartCoroutine(HighlightSafePlatforms(smashNoticeTime));

        yield return new WaitForSeconds(smashNoticeTime);

        Debug.Log("Swish Attack 실행!");
        Destroy(warningIndicator);

        // VFX 생성
        if (swishVFXPrefab != null)
        {
            Vector3 vfxPos = swishPos;
            vfxPos.y = 1.0f;

            GameObject vfx = Instantiate(swishVFXPrefab, vfxPos, Quaternion.Euler(0, angle, 0));
            vfx.transform.localScale = new Vector3(swishLength, 5f, swishWidth);

            Destroy(vfx, 3f);
        }


        bool playerHit = CheckPlayerInSwishZone(platformCenter, radialDirection, swishLength, swishWidth);

        if (playerHit)
        {
            bool onPlatform = IsPlayerOnPlatform();

            if (onPlatform)
            {
                //yay!
            }
            else
            {

                PlayerCharBattleController battleController = playerCharacter.GetComponent<PlayerCharBattleController>();
                if (battleController != null)
                {
                    battleController.TakeDamage(1);
                }

                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    CameraController camController = mainCam.GetComponent<CameraController>();
                    CameraShake shake = mainCam.GetComponent<CameraShake>();

                    if (shake != null)
                    {
                        // CameraController 잠깐 끄기!
                        if (camController != null)
                        {
                            camController.enabled = false;
                        }

                        // 흔들기
                        shake.Shake(0.2f, 0.5f);
                        StartCoroutine(ReenableCameraController(camController, 0.5f));
                    }
                }
            }
        }
        else
        {
            Debug.Log("플레이어가 Swish 피함!");
        }

        yield return new WaitForSeconds(0.5f);
        yield return new WaitForSeconds(attackCooldown);

        _isAttacking = false;
        currentPhase = PhaseOne.Detect;
    }

    bool CheckPlayerInSwishZone(Vector3 center, Vector3 radialDir, float swishLength, float swishWidth)
    {
        if (playerCharacter == null) return false;

        Vector3 playerPos = playerCharacter.position;
        playerPos.y = 0;

        Vector3 centerFlat = center;
        centerFlat.y = 0;

        // Swish 중심 위치
        Vector3 swishCenter = centerFlat + radialDir * swishDistance;

        // 플레이어 → Swish 중심 벡터
        Vector3 toPlayer = playerPos - swishCenter;

        // 접선 방향
        Vector3 tangentDir = Vector3.Cross(Vector3.up, radialDir).normalized;

        // 플레이어 거리 (좌우, 앞뒤)
        float lateralDistance = Mathf.Abs(Vector3.Dot(toPlayer, tangentDir));
        float radialDistance = Mathf.Abs(Vector3.Dot(toPlayer, radialDir));

        // 범위 체크
        if (lateralDistance <= swishLength / 2f && radialDistance <= swishWidth / 2f)
        {

            return true;
        }

        Debug.Log(">>> 충돌 안 함");
        return false;

    }

    IEnumerator HighlightSafePlatforms(float duration)
    {
        // 모든 Platform 찾기
        Platform[] platforms = FindObjectsOfType<Platform>();

        // 원래 색 저장
        System.Collections.Generic.Dictionary<Platform, Color> originalColors =
            new System.Collections.Generic.Dictionary<Platform, Color>();

        foreach (Platform platform in platforms)
        {
            Renderer renderer = platform.GetComponent<Renderer>();
            if (renderer != null)
            {
                originalColors[platform] = renderer.material.color;
                renderer.material.color = Color.green; // 초록색!
            }
        }

        yield return new WaitForSeconds(duration);

        // 원래 색으로 복구
        foreach (Platform platform in platforms)
        {
            if (platform != null && originalColors.ContainsKey(platform))
            {
                Renderer renderer = platform.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = originalColors[platform];
                }
            }
        }
    }

    // 플레이어가 Platform 위에 있는지 체크
    bool IsPlayerOnPlatform()
    {
        if (playerCharacter == null) return false;

        // 플레이어 발 아래로 여러 Raycast
        Vector3 rayStart = playerCharacter.position + Vector3.up * 0.2f;
        RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.down, 2f);

        foreach (RaycastHit hit in hits)
        {
            // Platform 컴포넌트 확인
            Platform platform = hit.collider.GetComponent<Platform>();
            if (platform != null)
            {
                Debug.Log($"플레이어가 Platform 위에 있음: {platform.gameObject.name}");
                return true;
            }

            // 또는 이름으로 확인
            if (hit.collider.gameObject.name.Contains("Platform"))
            {
                Debug.Log($"플레이어가 Platform 위에 있음: {hit.collider.gameObject.name}");
                return true;
            }
        }

        return false;
    }

    public IEnumerator MeteorAttack()
    {
        _isAttacking = true;
        Debug.Log("Meteor Attack 시작!");

        for (int i = 0; i < meteorCount; i++)
        {
            // 1. 랜덤 위치 (보스 피하기)
            Vector3 targetPos = GetRandomPositionAwayFromBoss();

            // 2. 경고 장판
            GameObject indicator = Instantiate(meteorIndicatorPrefab, targetPos, Quaternion.Euler(0, 0, 0));
            indicator.transform.localScale = new Vector3(5f, 0.2f, 5f);

            // 3. 메테오 Orb (위에서 떨어짐)
            Vector3 meteorStartPos = targetPos;
            meteorStartPos.y = meteorSpawnHeight; // 15m 위

            GameObject meteor = Instantiate(meteorPrefab, meteorStartPos, Quaternion.identity);

            // 4. 떨어뜨리기
            Meteor meteorScript = meteor.GetComponent<Meteor>();
            if (meteorScript != null)
            {
                meteorScript.Initialize(targetPos, meteorFallSpeed, meteorWarningTime, indicator);
            }

            yield return new WaitForSeconds(0.3f); // 다음 메테오까지 간격
        }

        yield return new WaitForSeconds(3f); // 다 떨어질 때까지

        Debug.Log($"공격 완료! {attackCooldown}초 쿨타임...");
        yield return new WaitForSeconds(attackCooldown);

        _isAttacking = false;
        currentPhase = PhaseOne.Detect;
    }

    Vector3 GetRandomPositionAwayFromBoss()
    {
        Vector3 randomPos;
        int attempts = 0;

        do
        {
            // Stage 안 랜덤 위치
            Vector2 random = Random.insideUnitCircle * stageRadius;
            randomPos = new Vector3(random.x, 2.22f, random.y);

            // 보스와 거리
            float distance = Vector3.Distance(
                new Vector3(transform.position.x, 0, transform.position.z),
                new Vector3(randomPos.x, 0, randomPos.z)
            );

            // 보스에서 충분히 멀면 OK!
            if (distance > bossExclusionRadius)
            {
                return randomPos;
            }

            attempts++;
        }
        while (attempts < 20);

        // 실패하면 Stage 가장자리
        Vector2 edge = Random.insideUnitCircle.normalized * stageRadius * 0.8f;
        return new Vector3(edge.x, 0.5f, edge.y);
    }

    IEnumerator ReenableCameraController(CameraController controller, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (controller != null)
        {
            controller.enabled = true;
        }
    }

    public IEnumerator intenseDoubleShake()
    {
       yield return new WaitForSeconds(0.5f);
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

    
    IEnumerator FlashRed(float duration)
    {
        float elapsed = 0f;
        Color redColor = new Color(1f, 0.3f, 0.3f);

        while (elapsed < duration)
        {
            float t = Mathf.PingPong(elapsed * 3f, 1f);
            _bossMaterial.color = Color.Lerp(_bossOriginalColor, redColor, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

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