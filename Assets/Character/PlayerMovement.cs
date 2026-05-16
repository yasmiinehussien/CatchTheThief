using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Speeds")]
    public float walkSpeed = 3f;

    [Header("Smooth Settings")]
    public float speedSmoothTime = 0.1f;
    public float rotationSmoothTime = 0.1f;

    [Header("Grid Constraints")]
    [Tooltip("If true, movement is restricted to tiles inside GridManager.mapLayout (road or grass).")]
    public bool enforceGridBounds = true;
    private CharacterController _controller;
    private Animator _animator;

    private float _currentSpeed;
    private float _speedVelocity;
    private float _rotationVelocity;
    private float _verticalVelocity;

    private GridManager gm;
    private bool hasWon = false;
    

    void Start()
    {
        _controller = GetComponent<CharacterController>();
        _animator = GetComponentInChildren<Animator>();

        gm = GridManager.Instance;

        Invoke(nameof(SpawnAtStart), 0f);
    }

    void SpawnAtStart()
    {
        if (GridManager.Instance == null) return;
        Vector3 startPos = GridManager.Instance.GridToWorld(0, 0);
        startPos.y = 0f;
        _controller.enabled = false;
        transform.position = startPos;
        _controller.enabled = true;
        _verticalVelocity = 0f;
    }

    void Update()
    {
        HandleMovement();
        ApplyGravity();
        if (enforceGridBounds)
         EnsureOnValidTile();
    }

    void HandleMovement()
    {
        if (hasWon)
            return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        Vector2 input = Vector2.zero;

        if (keyboard.wKey.isPressed) input.y += 1f;
        if (keyboard.sKey.isPressed) input.y -= 1f;
        if (keyboard.aKey.isPressed) input.x -= 1f;
        if (keyboard.dKey.isPressed) input.x += 1f;

     
        if (keyboard.upArrowKey.isPressed) input.y += 1f;
        if (keyboard.downArrowKey.isPressed) input.y -= 1f;
        if (keyboard.leftArrowKey.isPressed) input.x -= 1f;
        if (keyboard.rightArrowKey.isPressed) input.x += 1f;

        bool isMoving = input.sqrMagnitude > 0.01f;

        if (_animator != null)
            _animator.SetBool("isWalking", isMoving);

        float targetSpeed = isMoving ? walkSpeed : 0f;

        _currentSpeed = Mathf.SmoothDamp(
            _currentSpeed,
            targetSpeed,
            ref _speedVelocity,
            speedSmoothTime
        );

        if (!isMoving) return;

        Vector3 inputDir = new Vector3(input.x, 0f, input.y).normalized;
        Vector3 intendedMovement = inputDir * _currentSpeed * Time.deltaTime;

        if (enforceGridBounds && gm != null && gm.mapLayout != null)
        {
            Vector3 currentPos = transform.position;
            Vector3 fullTarget = currentPos + intendedMovement;

            if (IsPositionOnMap(fullTarget))
            {
              
            }
            else
            {
             
                Vector3 targetX = currentPos + new Vector3(intendedMovement.x, 0f, 0f);
                Vector3 targetZ = currentPos + new Vector3(0f, 0f, intendedMovement.z);

                bool canX = IsPositionOnMap(targetX);
                bool canZ = IsPositionOnMap(targetZ);

                if (canX && !canZ)
                {
                    intendedMovement = new Vector3(intendedMovement.x, 0f, 0f);
                    inputDir = new Vector3(Mathf.Sign(intendedMovement.x), 0f, 0f);
                }
                else if (canZ && !canX)
                {
                    intendedMovement = new Vector3(0f, 0f, intendedMovement.z);
                    inputDir = new Vector3(0f, 0f, Mathf.Sign(intendedMovement.z));
                }
                else if (canX && canZ)
                {
                   
                    if (Mathf.Abs(intendedMovement.x) > Mathf.Abs(intendedMovement.z))
                    {
                        intendedMovement = new Vector3(intendedMovement.x, 0f, 0f);
                        inputDir = new Vector3(Mathf.Sign(intendedMovement.x), 0f, 0f);
                    }
                    else
                    {
                        intendedMovement = new Vector3(0f, 0f, intendedMovement.z);
                        inputDir = new Vector3(0f, 0f, Mathf.Sign(intendedMovement.z));
                    }
                }
                else
                {
                    
                    intendedMovement = Vector3.zero;
                    _currentSpeed = 0f;
                    if (_animator != null)
                        _animator.SetBool("isWalking", false);
                }
            }
        }

        
        if (intendedMovement.sqrMagnitude > 0.0001f)
        {
            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg;
            float smoothAngle = Mathf.SmoothDampAngle(
                transform.eulerAngles.y,
                targetAngle,
                ref _rotationVelocity,
                rotationSmoothTime
            );
            transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);

            _controller.Move(intendedMovement);
        }
    }

    void ApplyGravity()
    {
        if (_controller.isGrounded && _verticalVelocity < 0f)
            _verticalVelocity = -2f;

        _verticalVelocity += Physics.gravity.y * Time.deltaTime;

        _controller.Move(Vector3.up * _verticalVelocity * Time.deltaTime);
    }

    void EnsureOnValidTile()
    {
        if (gm == null || gm.mapLayout == null)
            return;

        int rows = gm.mapLayout.GetLength(0);
        int cols = gm.mapLayout.GetLength(1);

        Vector3 p = transform.position;

        p.x = Mathf.Clamp(p.x, 0f, cols - 1);
        p.z = Mathf.Clamp(p.z, 0f, rows - 1);

        _controller.enabled = false;
        transform.position = p;
        _controller.enabled = true;
    }

    private bool IsPositionOnMap(Vector3 worldPos)
    {
        if (gm == null || gm.mapLayout == null) return true;

        int rows = gm.mapLayout.GetLength(0);
        int cols = gm.mapLayout.GetLength(1);

        int gx = Mathf.RoundToInt(worldPos.x);
        int gz = Mathf.RoundToInt(worldPos.z);

        if (gx < 0 || gx >= cols || gz < 0 || gz >= rows)
            return false;

        // 1 = road, 
        int val = gm.mapLayout[gz, gx];
        return (val == 1);
    }

    public void StopPlayer()
    {
        _currentSpeed = 0f;
        _speedVelocity = 0f;
        _verticalVelocity = 0f;

        if (_animator != null)
            _animator.SetBool("isWalking", false);
    }
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hasWon) return;
        if (hit == null || hit.gameObject == null) return;

        if (hit.gameObject.CompareTag("door"))
        {
            hasWon = true;
            _currentSpeed = 0f;

            if (_animator != null)
            {
                _animator.SetBool("isWalking", false);
                _animator.SetTrigger("Dance");
            }

            var timeUpAlert = FindAnyObjectByType<TimeUpAlert>();
            if (timeUpAlert != null)
                timeUpAlert.ShowSuccess();
            else
                Debug.Log("WIN!");
        }
    }
}