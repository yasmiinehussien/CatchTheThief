using System.Collections.Generic;
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
    [Tooltip("Max search radius (tiles) used if player ends up outside valid tiles — player will snap to nearest valid tile within this radius.")]
    public int snapSearchRadius = 3;

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

        Invoke(nameof(SpawnAtStart), 0.01f);
    }

    void SpawnAtStart()
    {
        if (GridManager.Instance == null) return;

        // Keep the original start but ensure it is a valid tile
        Vector3 startPos = GridManager.Instance.GridToWorld(0, 0);
        startPos.y = 0.01f; 

        _controller.enabled = false;
        transform.position = startPos;
        _controller.enabled = true;

        // If the starting cell is invalid (edge cases), snap to nearest valid
        if (enforceGridBounds && !IsPositionOnMap(transform.position))
        {
            Vector2Int? nearest = FindNearestValidCell(transform.position, snapSearchRadius);
            if (nearest.HasValue)
            {
                Vector3 world = GridManager.Instance.GridToWorld(nearest.Value.x, nearest.Value.y);
                world.y = transform.position.y;
                _controller.enabled = false;
                transform.position = world;
                _controller.enabled = true;
            }
        }
    }

    void Update()
    {
        HandleMovement();
        ApplyGravity();
        // Safety: if for any reason player ends on invalid tile, snap back to nearest valid
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

        // If grid enforcement is enabled, validate the movement before applying it
        if (enforceGridBounds && gm != null && gm.mapLayout != null)
        {
            Vector3 currentPos = transform.position;
            Vector3 fullTarget = currentPos + intendedMovement;

            // If full-target is valid -> allow
            if (IsPositionOnMap(fullTarget))
            {
                // allowed
            }
            else
            {
                // Try axis-aligned moves (sliding)
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
                    // Prefer axis that aligns more with input
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
                    // No valid movement — stop and go idle (user can change direction next frame)
                    intendedMovement = Vector3.zero;
                    _currentSpeed = 0f;
                    if (_animator != null)
                        _animator.SetBool("isWalking", false);
                }
            }
        }

        // Rotate smoothly toward the actual movement direction if any
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
        if (gm == null || gm.mapLayout == null) return;

        if (IsPositionOnMap(transform.position))
            return;

        // Try to snap to nearest valid map tile within radius
        Vector2Int? nearest = FindNearestValidCell(transform.position, snapSearchRadius);
        if (nearest.HasValue)
        {
            Vector3 world = gm.GridToWorld(nearest.Value.x, nearest.Value.y);
            world.y = Mathf.Max(transform.position.y, 0.1f);
            _controller.enabled = false;
            transform.position = world;
            _controller.enabled = true;
            _verticalVelocity = 0f;
        }
        else
        {
            // As a fallback clamp inside grid bounds
            int rows = gm.mapLayout.GetLength(0);
            int cols = gm.mapLayout.GetLength(1);
            Vector3 p = transform.position;
            float r = _controller.radius;
            p.x = Mathf.Clamp(p.x, -0.5f + r, cols - 0.5f - r);
            p.z = Mathf.Clamp(p.z, -0.5f + r, rows - 0.5f - r);
            _controller.enabled = false;
            transform.position = p;
            _controller.enabled = true;
            _verticalVelocity = 0f;
        }
    }

    // Returns true when world position maps inside grid bounds and to a valid tile (road or grass)
    private bool IsPositionOnMap(Vector3 worldPos)
    {
        if (gm == null || gm.mapLayout == null) return true; // don't block when grid missing

        int rows = gm.mapLayout.GetLength(0);
        int cols = gm.mapLayout.GetLength(1);

        int gx = Mathf.RoundToInt(worldPos.x);
        int gz = Mathf.RoundToInt(worldPos.z);

        if (gx < 0 || gx >= cols || gz < 0 || gz >= rows)
            return false;

        // mapLayout uses 1 = road, 0 = grass — both allowed
        int val = gm.mapLayout[gz, gx];
        return (val == 0 || val == 1);
    }

    // BFS search for nearest valid cell (road or grass) within maxRadius tiles.
    private Vector2Int? FindNearestValidCell(Vector3 worldPos, int maxRadius)
    {
        if (gm == null || gm.mapLayout == null) return null;

        int rows = gm.mapLayout.GetLength(0);
        int cols = gm.mapLayout.GetLength(1);

        int startX = Mathf.RoundToInt(worldPos.x);
        int startZ = Mathf.RoundToInt(worldPos.z);

        Queue<Vector2Int> q = new Queue<Vector2Int>();
        HashSet<Vector2Int> seen = new HashSet<Vector2Int>();

        Vector2Int start = new Vector2Int(startX, startZ);
        q.Enqueue(start);
        seen.Add(start);

        int[] dx = { 0, 0, 1, -1 };
        int[] dz = { 1, -1, 0, 0 };

        while (q.Count > 0)
        {
            Vector2Int cur = q.Dequeue();
            int dist = Mathf.Abs(cur.x - startX) + Mathf.Abs(cur.y - startZ);
            if (dist > maxRadius) continue;

            if (cur.x >= 0 && cur.x < cols && cur.y >= 0 && cur.y < rows)
            {
                int v = gm.mapLayout[cur.y, cur.x];
                if (v == 0 || v == 1)
                    return cur;
            }

            for (int i = 0; i < 4; i++)
            {
                Vector2Int n = new Vector2Int(cur.x + dx[i], cur.y + dz[i]);
                if (seen.Add(n))
                    q.Enqueue(n);
            }
        }

        return null;
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hasWon) return;
        if (hit == null || hit.gameObject == null) return;

        if (hit.gameObject.CompareTag("EndTile"))
        {
            hasWon = true;
            _currentSpeed = 0f;
            if (_animator != null)
            {
                _animator.SetBool("isWalking", false);

                // Trigger the dance animation. Make sure Animator has a Trigger parameter named "Dance".
                _animator.SetTrigger("Dance");
            }

            // Show end-game UI after triggering animation.
            var timeUpAlert = FindAnyObjectByType<TimeUpAlert>();
            if (timeUpAlert != null)
            {
                // Optionally: delay showing the alert until the dance finishes.
                // If you want a delay, use StartCoroutine(ShowAlertDelayed(revealDelay));
                timeUpAlert.ShowAlert();
            }
            else
            {
                Debug.Log("PlayerMovement: Reached bank — win triggered.");
            }
        }
    }
}