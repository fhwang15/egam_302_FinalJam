using System.Collections;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

public class CoreShootingManager : MonoBehaviour
{

    public CinemachineCamera coreShootingCamera;

    public BossManager bossManager;
    public Camera mainCamera;
    public GameObject coreTarget; // 조준할 코어 오브젝트
    public Canvas uiCanvas; // UI Canvas
    public TextMeshProUGUI timerText; // 타이머 UI
    public Image crosshair; // 조준점

    // 코어 이동 설정
    public float coreSpeed = 3f;
    public float minX = -5f;
    public float maxX = 5f;
    public float minY = -3f;
    public float maxY = 3f;
    public float coreDistance = 10f; // 카메라에서 코어까지 거리

    // 타이밍
    public float shootingTime = 8f; // 쏠 수 있는 시간
    public int requiredHits = 3; // 성공 조건 (3번 맞춰야 함)

    private bool isActive = false;
    private int currentHits = 0;
    private float timeRemaining;
    private Vector3 coreVelocity;
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private bool playerControlEnabled = true;

    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (bossManager == null)
        {
            bossManager = FindFirstObjectByType<BossManager>();
        }

        // 시작할 때는 비활성화
        DeactivateCoreMode();
    }

    void Update()
    {
        if (!isActive) return;

        // 타이머 감소
        timeRemaining -= Time.deltaTime;

        if (timerText != null)
        {
            timerText.text = $"Time: {timeRemaining:F1}s | Hits: {currentHits}/{requiredHits}";
        }

        // 시간 종료
        if (timeRemaining <= 0)
        {
            FailCoreSequence();
            return;
        }

        // 코어 이동
        MoveCoreRandomly();

        // 클릭 감지
        if (Input.GetMouseButtonDown(0))
        {
            ShootAtCore();
        }
    }

    public void ActivateCoreMode()
    {
        isActive = true;
        currentHits = 0;
        timeRemaining = shootingTime;

        DisablePlayerControl();

        CameraController cameraController = mainCamera.GetComponent<CameraController>();
        if (cameraController != null)
        {
            cameraController.enabled = false;
        }

        CinemachineBrain brain = mainCamera.GetComponent<CinemachineBrain>();
        if (brain != null)
        {
            brain.enabled = true;
        }

        if (bossManager != null)
        {
            Collider bossCollider = bossManager.GetComponent<Collider>();
            if (bossCollider != null)
            {
                bossCollider.enabled = false;
            }
        }

        if (coreShootingCamera != null)
        {
            coreShootingCamera.Priority = 100;
        }

        // 코어 활성화
        if (coreTarget != null)
        {
            coreTarget.SetActive(true);
            coreVelocity = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0).normalized * coreSpeed;
        }

        // UI 활성화
        if (uiCanvas != null) uiCanvas.gameObject.SetActive(true);
        if (crosshair != null) crosshair.gameObject.SetActive(true);
        if (timerText != null) timerText.gameObject.SetActive(true);
    }


    void DeactivateCoreMode()
    {
        isActive = false;

        if (coreShootingCamera != null)
        {
            coreShootingCamera.Priority = 5;
        }

        // 기존 카메라 스크립트 다시 활성화!
        CameraController cameraController = mainCamera.GetComponent<CameraController>();
        if (cameraController != null)
        {
            cameraController.enabled = true;
        }


        CinemachineBrain brain = mainCamera.GetComponent<CinemachineBrain>();
        if (brain != null)
        {
            brain.enabled = false;
        }

        if (bossManager != null)
        {
            Collider bossCollider = bossManager.GetComponent<Collider>();
            if (bossCollider != null)
            {
                bossCollider.enabled = true;
                Debug.Log("Boss Collider 활성화!");
            }
        }

        EnablePlayerControl();

        if (coreTarget != null)
        {
            coreTarget.SetActive(false);
        }

        if (uiCanvas != null) uiCanvas.gameObject.SetActive(false);
        if (crosshair != null) crosshair.gameObject.SetActive(false);
        if (timerText != null) timerText.gameObject.SetActive(false);
    }

    void MoveCoreRandomly()
    {
        if (coreTarget == null) return;

        // 카메라 기준 로컬 좌표로 이동
        Vector3 localPos = mainCamera.transform.InverseTransformPoint(coreTarget.transform.position);

        // 이동
        localPos.x += coreVelocity.x * Time.deltaTime;
        localPos.y += coreVelocity.y * Time.deltaTime;

        // 경계 체크 및 반사
        if (localPos.x < minX || localPos.x > maxX)
        {
            coreVelocity.x *= -1;
            localPos.x = Mathf.Clamp(localPos.x, minX, maxX);
        }

        if (localPos.y < minY || localPos.y > maxY)
        {
            coreVelocity.y *= -1;
            localPos.y = Mathf.Clamp(localPos.y, minY, maxY);
        }

        // 랜덤 방향 변경 (가끔)
        if (Random.value < 0.02f)
        {
            coreVelocity = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0).normalized * coreSpeed;
        }

        // 월드 좌표로 변환
        coreTarget.transform.position = mainCamera.transform.TransformPoint(localPos);
    }

    void ShootAtCore()
    {
        // 마우스 위치에서 Raycast 발사! (화면 중앙 X)
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.yellow, 1f);

        if (Physics.Raycast(ray, out hit, 100f))
        {
            Debug.Log($"Raycast 맞음! {hit.collider.gameObject.name}");

            if (hit.collider.gameObject == coreTarget)
            {
                currentHits++;
                Debug.Log($"코어 맞춤! ({currentHits}/{requiredHits})");

                StartCoroutine(FlashCore());

                if (currentHits >= requiredHits)
                {
                    SucceedCoreSequence();
                }
            }
            else
            {
                Debug.Log($"빗나감! ({hit.collider.gameObject.name} 맞음)");
            }
        }
        else
        {
            Debug.Log("아무것도 안 맞음!");
        }
    }

    IEnumerator FlashCore()
    {
        Renderer renderer = coreTarget.GetComponent<Renderer>();
        if (renderer != null)
        {
            Color original = renderer.material.color;
            renderer.material.color = Color.green;
            yield return new WaitForSeconds(0.1f);
            renderer.material.color = original;
        }
    }

    void SucceedCoreSequence()
    {
        Debug.Log("코어 공격 성공! 큰 데미지!");

        DeactivateCoreMode();

        if (bossManager != null)
        {
            bossManager.ShowBoss();
            bossManager.OnCoreShootingSuccess();
        }
    }

    void FailCoreSequence()
    {
        Debug.Log("코어 공격 실패! 보스 체력 회복!");

        DeactivateCoreMode();

        if (bossManager != null)
        {
            bossManager.ShowBoss();
            bossManager.OnCoreShootingFail();
        }
    }

    void DisablePlayerControl()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            player.enabled = false;
        }

        PlayerCharBattleController battleController = FindFirstObjectByType<PlayerCharBattleController>();
        if (battleController != null)
        {
            battleController.enabled = false;
        }
    }

    void EnablePlayerControl()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            player.enabled = true;
        }

        PlayerCharBattleController battleController = FindFirstObjectByType<PlayerCharBattleController>();
        if (battleController != null)
        {
            battleController.enabled = true;
        }
    }
}