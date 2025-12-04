using System.Collections;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    private Vector3 _direction;
    private float _speed;
    private float _maxLifetime = 0.5f;
    private bool _hasHit = false;


    // Update is called once per frame
    void Update()
    {
        if (_hasHit)
        {
            return; //Stop moving if we've hit something
        }

        transform.position += _direction * _speed * Time.deltaTime; //bulletMoving

        Camera cam = Camera.main;
        // this method would have worked for perspective projection
        //Vector3 fromCameraThroughBulletDirection = (transform.position - cam.transform.position).normalized;
       
        Vector3 camDir = cam.transform.forward;

        Debug.DrawRay(transform.position, camDir * 50.0f, Color.red, 5.0f);

        RaycastHit hit;
        if(Physics.Raycast(transform.position, camDir, out hit, _speed * Time.deltaTime))
        {
            //Coroutine to wait until the end of the stuff? 
            OnHit(hit);
        }

    }

    public void Initialize(Vector3 direction, float speed)
    {

        _direction = direction.normalized;
        _speed = speed;

        Destroy(gameObject, _maxLifetime);
    }

    void OnHit(RaycastHit hit)
    {
        _hasHit = true;

        Platform platform = hit.collider.GetComponent<Platform>();
        BossManager bossManager = hit.collider.gameObject.GetComponent<BossManager>();
        Stitch stitch = hit.collider.gameObject.GetComponent<Stitch>();

        if(platform != null)
        {
            //Something (Maybe add a particle effect/if it hits a breakable platform, it will do something)

        }

        if (bossManager != null)
        {
            //Hit the boss (Phase One = No hit)
        }

        if(stitch != null)
        {
            //if stitch, it will do something.

        }


        Destroy(gameObject, 0.5f);
    }

    //

}
