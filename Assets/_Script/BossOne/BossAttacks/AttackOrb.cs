using UnityEngine;

public class AttackOrb : MonoBehaviour
{
    private Vector3 direction;
    private float speed;
    private float damage;
    private float lifetime;
    private float elapsed = 0f;

    public void Initialize(Vector3 dir, float spd, float dmg, float life)
    {
        direction = dir;
        speed = spd;
        damage = dmg;
        lifetime = life;
    }

    void Update()
    {
        // 이동
        transform.position += direction * speed * Time.deltaTime;

        // 수명
        elapsed += Time.deltaTime;
        if (elapsed >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"AttackOrb 충돌: {other.gameObject.name}");

        PlayerCharBattleController player = other.GetComponent<PlayerCharBattleController>();
        if (player != null)
        {
            player.TakeDamage(1);
            Destroy(gameObject);
            return;
        }

        Platform platform = other.GetComponentInParent<Platform>();
        if (platform != null)
        {
            Destroy(gameObject);
            return;
        }

        Stake stake = other.GetComponent<Stake>();
        if (stake != null)
        {

            return;
        }

        Stitch stitch = other.GetComponent<Stitch>();
        if (stitch != null)
        {

            return;
        }
    }
}
