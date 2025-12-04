using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform player;
    public Transform platformCenter;

    public float cameraDistance; //distance from the player
    public float cameraHeight; //height offset
    public float cameraAngle; //angle offset
    public Vector3 offset;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    void LateUpdate()
    {
        if (player == null || platformCenter == null) return;

        // direction from player to platform center
        Vector3 directionToCenter = platformCenter.position - player.position;
        directionToCenter.y = 0; // 
        directionToCenter.Normalize(); //same direction , length 1
        //==> Getting the direction

        // Behind the player relative to the platform center
        Vector3 cameraPosition = player.position - directionToCenter * cameraDistance;
        cameraPosition.y = player.position.y + cameraHeight;

        transform.position = cameraPosition;

        Vector3 lookDirection = player.position - transform.position;
        float xRotation = Mathf.Atan2(lookDirection.x, lookDirection.z) * Mathf.Rad2Deg;

        // Camera will see the player
        transform.rotation = Quaternion.Euler(cameraAngle, xRotation, 0);
    }
}
