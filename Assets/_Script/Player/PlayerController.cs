using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private CharacterController _characterController;
    public float MovementSpeed; // A/D moving
    public float lateralMovementSpeed; // W/S moving
    public float RotationSpeed; //Character is facing speed 
    public float JumpForce; 
    public float Gravity = -9.81f;


    //The platform radius the player will be moving around
    public float platformRadius;
    public float minForward;
    public float maxBackward;
    public Transform platformCenter;
    public Transform mainCamera;

    private float _currentRadius;
    private Vector3 _centerPosition;
    private float _rotationY; // For mouse rotation, not used in current implementation
    private float _verticalVelocity;
    private Quaternion _lastMoveRotation;


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

        if(mainCamera == null)
        {
            mainCamera = Camera.main.transform; // in case I forget to assign camera lol
        }

        Vector3 offset = transform.position - _centerPosition;
        offset.y = 0;
        _currentRadius = offset.magnitude;

        _lastMoveRotation = transform.rotation;
    }

    public void Move(Vector2 movementVector)
    {
        //Current player position
        Vector3 currentOffset = transform.position - _centerPosition;
        currentOffset.y = 0;
        Vector3 tangentDirection = Vector3.Cross(Vector3.up, currentOffset).normalized;
        Vector3 radialDirection = -currentOffset.normalized; //Current direction from center to player

        //A/D to move around the platform
        Vector3 lateralMove = -tangentDirection * movementVector.x * MovementSpeed * Time.deltaTime;

        // W/S to move closer/farther from center
        _currentRadius -= movementVector.y * lateralMovementSpeed * Time.deltaTime;
        _currentRadius = Mathf.Clamp(_currentRadius, minForward, maxBackward); // Clamp radius to avoid going too close or too far

        _characterController.Move(lateralMove);

        Vector3 newOffset = transform.position - _centerPosition;
        newOffset.y = 0;


        if(newOffset.magnitude > 0.01f)
        {
            newOffset = newOffset.normalized * _currentRadius;
            Vector3 updatedPos = _centerPosition + newOffset;
            updatedPos.y = transform.position.y; // Preserve the y position

            Vector3 correction = updatedPos - transform.position;
            correction.y = 0;
            _characterController.Move(correction);
        }


        //gravity
        if (_characterController.isGrounded && _verticalVelocity < 0)
        {
            _verticalVelocity = -2f;
        }

        _verticalVelocity += Gravity * Time.deltaTime;
        _characterController.Move(new Vector3(0, _verticalVelocity, 0) * Time.deltaTime);

        if (mainCamera != null)
        {
            Vector3 lookDirection = mainCamera.position - transform.position;
            lookDirection.y = 0;
            if(lookDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection);
            }
        }

        if (movementVector.magnitude > 0.1f) // Only when there is movement input
        {
            // Calculating the desired move direction based on input
            Vector3 moveDirection = -tangentDirection * movementVector.x + radialDirection * (movementVector.y);
            moveDirection.y = 0;

           

            if (moveDirection.magnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation =targetRotation;
                _lastMoveRotation = targetRotation;
            }
        }
        else
        {
            transform.rotation = _lastMoveRotation;
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
