using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private CharacterController _characterController;
    public float MovementSpeed;
    public float RotationSpeed;
    public float JumpForce;
    public float Gravity = -9.81f;

    //The platform radius the player will be moving around
    public float platformRadius = 5f;
    public Transform platformCenter;

    private float _currentAngle = 0f;
    private Vector3 _centerPosition;
    private float _rotationY; // For mouse rotation, not used in current implementation
    private float _verticalVelocity;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _characterController = GetComponent<CharacterController>();

        if(platformCenter != null)
        {
            _centerPosition = platformCenter.position;
        }
        else
        {
            _centerPosition = Vector3.zero;
        }

        Vector3 offset = transform.position - _centerPosition;
        offset.y = 0; // Ignore vertical offset

        _currentAngle = Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;
    }

    public void Move(Vector2 movementVector)
    {
        float horizontal = movementVector.x; //(W/A movement)
        _currentAngle += horizontal * MovementSpeed * 10f * Time.deltaTime;

        float radian = _currentAngle * Mathf.Deg2Rad;
        float targetX = _centerPosition.x + Mathf.Cos(radian) * platformRadius;
        float targetZ = _centerPosition.z + Mathf.Sin(radian) * platformRadius;

        float currentY = transform.position.y;

        if (_characterController.isGrounded && _verticalVelocity < 0)
        {
            _verticalVelocity = -2f; //allows to be on the ground
        }

        _verticalVelocity += Gravity * Time.deltaTime;
        currentY += _verticalVelocity * Time.deltaTime;

        Vector3 newPosition = new Vector3(targetX, currentY, targetZ);

        Vector3 movement = newPosition - transform.position;
        _characterController.Move(movement);

        //Allows the player to move towards where they are looking.
        if (horizontal != 0)
        {
            transform.rotation = Quaternion.Euler(0, _currentAngle, 0);
        } 

    }
    /*
    public void Rotate(Vector2 rotationVector)
    {
        _rotationY += rotationVector.x * RotationSpeed * Time.deltaTime;
        transform.localRotation = Quaternion.Euler(0, _rotationY, 0);
    }
    */

    public void Jump()
    {
        if(_characterController.isGrounded)
        {
            _verticalVelocity = JumpForce;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
