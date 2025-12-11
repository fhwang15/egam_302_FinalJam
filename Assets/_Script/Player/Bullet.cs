using System.Collections;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    private Vector3 _direction;
    private float _speed;
    private float _travelTime = 0.1f; // 날아가는 시간
    private float _raycastDistance = 100f;

    private bool _hasHit = false;
    public Transform platformCenter;


    // Update is called once per frame
    void Update()
    {


    }

    IEnumerator FlyShoot()
    {
        float elapsed = 0f;

        // 1단계: 날아가기
        while (elapsed < _travelTime)
        {
            float frameDistance = _speed * Time.deltaTime;
            transform.position += _direction * frameDistance;

            elapsed += Time.deltaTime;
            yield return null;
        }

        Vector3 rayDirection;

        if (platformCenter != null)
        {
            rayDirection = (platformCenter.position - transform.position).normalized;
        }
        else
        {
            rayDirection = (Vector3.zero - transform.position).normalized;
        }

        //Get All Hits
        RaycastHit[] hits = Physics.RaycastAll(transform.position, rayDirection, _raycastDistance);

        foreach (RaycastHit hit in hits)
        {

            if (hit.collider.gameObject.name == "Stage")
            {
                continue;
            }

            // Boss/Stitch/Stake
            BossManager boss = hit.collider.GetComponentInParent<BossManager>();
            Stitch stitch = hit.collider.GetComponentInParent<Stitch>();
            Stake stake = hit.collider.GetComponentInParent<Stake>();

            if (boss != null || stitch != null || stake != null)
            {
                Debug.DrawRay(transform.position, rayDirection * hit.distance, Color.green, 2f);
                OnHit(hit);
                yield break;
            }
        }

        Debug.DrawRay(transform.position, rayDirection * _raycastDistance, Color.red, 5.0f);

        Destroy(gameObject, 0.5f);
    }


    public void Initialize(Vector3 direction, float speed)
    {

        _direction = direction.normalized;
        _speed = speed;

        transform.rotation = Quaternion.LookRotation(_direction);

        StartCoroutine(FlyShoot());
    }

    void OnHit(RaycastHit hit)
    {
        _hasHit = true;

        Platform platform = hit.collider.GetComponent<Platform>();
        BossManager bossManager = hit.collider.GetComponentInParent<BossManager>();
        Stitch stitch = hit.collider.GetComponent<Stitch>();
        Stake stake = hit.collider.GetComponent<Stake>();

        if (platform != null)
        {
            //Something (Maybe add a particle effect/if it hits a breakable platform, it will do something)

        }

        if (bossManager != null)
        {
            bossManager.TakeDamagePhaseTwo(5f);
        }

        if (stitch != null)
        {
            stitch.TakeDamage(10f);
            stitch.OnHit(); //

        }

        if (stake != null)
        {
            stake.TakeDamage(10f);
            stake.OnHit();
        }


        Destroy(gameObject, 0.5f);

    }

    //

}
