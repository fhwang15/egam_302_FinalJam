using UnityEngine;

public class PlayerCharBattleController : MonoBehaviour
{
    public int health;

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
            bulletScript.Initialize(firePoint.forward, bulletSpeed);
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
