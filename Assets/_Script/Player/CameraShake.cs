using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool isShaking = false;

    void Start()
    {
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;
    }

    public void Shake(float duration = 0.5f, float magnitude = 0.3f)
    {
        if (!isShaking)
        {
            StartCoroutine(ShakeCoroutine(duration, magnitude));
        }
    }

    IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        isShaking = true;
        originalPosition = transform.position; // 현재 위치 저장
        float elapsed = 0f;

        Debug.Log($"카메라 흔들림 시작! 원래 위치: {originalPosition}");

        while (elapsed < duration)
        {
            // 랜덤 오프셋
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            float z = Random.Range(-1f, 1f) * magnitude;

            transform.position = originalPosition + new Vector3(x, y, z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPosition;
        isShaking = false;

        Debug.Log("카메라 흔들림 종료!");
    }

    // 위치 업데이트 (CameraController가 움직일 때마다)
    public void UpdateOriginalPosition()
    {
        if (!isShaking)
        {
            originalPosition = transform.localPosition;
            originalRotation = transform.localRotation;
        }
    }
}