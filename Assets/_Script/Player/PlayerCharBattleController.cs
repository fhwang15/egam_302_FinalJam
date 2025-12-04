using UnityEngine;

public class PlayerCharBattleController : MonoBehaviour
{
    public int health;

    public Transform bossLocation;


    public Transform firePoint;
    public GameObject BulletPrefab;
    public float bulletSpeed;

    //Whenever Player Clicks, they will instantiate bullet.

    private void Start()
    {
        health = 10;
    }


    public void Shooting()
    { 
        GameObject bullet = Instantiate(BulletPrefab, firePoint.position, firePoint.rotation);
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        
        if(bulletScript != null)
        {
            // let's shoot bullets left/right based on the camera.
            Camera cam = Camera.main;
            Vector3 camRight = cam.transform.right;

            float howSimilarIsFireForwardToCameraRight = Vector3.Dot(camRight, firePoint.forward);

            bulletScript.Initialize(camRight * Mathf.Sign(howSimilarIsFireForwardToCameraRight), bulletSpeed);
        }


    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButtonDown(0)) //Left Click
        {
            Shooting();
        }
    }


}
