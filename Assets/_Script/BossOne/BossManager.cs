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

    public GameObject BossCharacter;
    private Vector3 BossForward;
    public Vector3 PlayerCharacter;

    //List of transforms that will record the position of the stitches
    public List<Transform> StitchLocation;

    //Enum (State of Phase One and Two)
    BossState currentBossPhase = BossState.phaseOne;

    //Enum (States of Detect/AOE1/AOE2/AOE3)
    PhaseOne currentPhase = PhaseOne.Detect;

    //Health
    public float health;

    public float detectionCooldown = 2f;


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
    }

    // Update is called once per frame
    void Update()
    {

        if (currentBossPhase == BossState.phaseOne)
        {
            UpdatePhaseOne();
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
                if(!_isAttacking)
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
        targetPosition.y = 1.13f; // 바닥 높이

        // Rotation을 X축 90도로 설정 (바닥에 평평하게)
        GameObject indicator = Instantiate(smashIndicatorPrefab, targetPosition, Quaternion.Euler(0, 90, 0));

        // Scale은 Cylinder가 누워있는 상태 기준
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
        }
        else
        {
            Debug.Log("플레이어가 범위 밖으로 피함!");
        }

        yield return new WaitForSeconds(0.5f);

        _isAttacking = false;
        currentPhase = PhaseOne.Detect;
    }


    public IEnumerator RoundAttack()
    {
        yield return new WaitForSeconds(2f);
    }

    IEnumerator BlinkIndicator(GameObject indicator, float duration)
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

}