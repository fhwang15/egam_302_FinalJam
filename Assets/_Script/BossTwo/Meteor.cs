using UnityEngine;
using System.Collections;

public class Meteor : MonoBehaviour
{
    private Vector3 targetPosition;
    private float fallSpeed;
    private GameObject indicator;

    public float explosionRadius = 3f;
    public float damage = 2f;

    public void Initialize(Vector3 target, float speed, float warningTime, GameObject warningIndicator)
    {
        targetPosition = target;
        fallSpeed = speed;
        indicator = warningIndicator;

        StartCoroutine(FallRoutine(warningTime));
    }

    IEnumerator FallRoutine(float warningTime)
    {
        // 경고 대기
        yield return new WaitForSeconds(warningTime);

        // 경고 장판 제거
        if (indicator != null) Destroy(indicator);

        // 떨어지기!
        while (transform.position.y > targetPosition.y)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                fallSpeed * Time.deltaTime
            );
            yield return null;
        }

        // 착지 폭발!
        Explode();
    }

    void Explode()
    {
        Debug.Log("메테오 착지!");

        // 범위 내 플레이어 체크
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (Collider hit in hits)
        {
            PlayerCharBattleController player = hit.GetComponentInParent<PlayerCharBattleController>();
            if (player != null)
            {
                player.TakeDamage((int)damage);
            }
        }

        Destroy(gameObject);
    }
}