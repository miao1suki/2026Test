using Project.CameraModes;
using Project.PlatformPaths;
using Project.RopePaths;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public sealed class RopeDemoPlayer : MonoBehaviour
{
    [SerializeField]
    private float moveSpeed = 5f;

    [SerializeField]
    private float jumpSpeed = 7f;

    [SerializeField]
    private float gravity = -22f;

    [SerializeField]
    private float mouseLookSensitivity = 0.12f;

    [SerializeField]
    private CameraFollowController cameraFollow;

    [SerializeField]
    private CameraModeController cameraModeController;

    [SerializeField]
    private RopePathNetwork[] projectionNetworks;

    private CharacterController controller;
    private float verticalVelocity;
    private RopeProjectionDirection projectionDirection =
        RopeProjectionDirection.Front;

    public RopeProjectionDirection ProjectionDirection =>
        projectionDirection;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        EnsureDetectionCollider();
        ResolveReferences();
        ApplyProjectionDirection();
    }

    private void Update()
    {
        HandleViewModeToggle();
        HandleViewRotation();
        HandlePerspectiveLook();
        HandleMovement();
    }

    public void Configure(
        CameraFollowController follow,
        RopePathNetwork[] networks)
    {
        cameraFollow = follow;
        projectionNetworks = networks;
        ApplyProjectionDirection();
    }

    private void ResolveReferences()
    {
        if (cameraFollow == null)
        {
            cameraFollow = FindFirstObjectByType<CameraFollowController>();
        }

        if (cameraModeController == null)
        {
            cameraModeController =
                FindFirstObjectByType<CameraModeController>();
        }
    }

    private void EnsureDetectionCollider()
    {
        BoxCollider collider = GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<BoxCollider>();
        }

        collider.isTrigger = true;
        collider.center = Vector3.zero;
        collider.size = new Vector3(1f, 2.2f, 1f);
    }

    private void HandleViewModeToggle()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null ||
            !keyboard.fKey.wasPressedThisFrame)
        {
            return;
        }

        if (IsSide2D())
        {
            cameraFollow?.SetRequestedMode(
                CameraViewMode.Perspective3D);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            return;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        ApplyProjectionDirection();
    }

    private void HandleViewRotation()
    {
        if (!IsSide2D())
        {
            return;
        }

        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        if (mouse.leftButton.wasPressedThisFrame)
        {
            RotateView(-1);
        }

        if (mouse.rightButton.wasPressedThisFrame)
        {
            RotateView(1);
        }
    }

    private void HandlePerspectiveLook()
    {
        if (!IsPerspective3D() ||
            cameraModeController == null)
        {
            return;
        }

        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        Vector2 delta = mouse.delta.ReadValue();
        if (delta.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        cameraModeController.RotatePerspective(
            delta.x * mouseLookSensitivity,
            -delta.y * mouseLookSensitivity);
    }

    private void RotateView(int quarterTurns)
    {
        int directionIndex = Mathf.RoundToInt(
            Mathf.Repeat(
                (int)projectionDirection + quarterTurns,
                4));
        projectionDirection =
            (RopeProjectionDirection)directionIndex;
        ApplyProjectionDirection();
    }

    private void ApplyProjectionDirection()
    {
        if (projectionNetworks != null)
        {
            for (int index = 0;
                 index < projectionNetworks.Length;
                 index++)
            {
                RopePathNetwork network =
                    projectionNetworks[index];
                if (network != null)
                {
                    network.CurrentProjectionDirection =
                        projectionDirection;
                }
            }
        }

        cameraFollow?.SetRequestedMode(
            CameraViewMode.Side2D,
            DirectionToYaw(projectionDirection));
    }

    private void HandleMovement()
    {
        if (controller == null)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        float horizontalInput = 0f;
        float forwardInput = 0f;
        if (keyboard.aKey.isPressed)
        {
            horizontalInput -= 1f;
        }

        if (keyboard.dKey.isPressed)
        {
            horizontalInput += 1f;
        }

        if (!IsSide2D())
        {
            if (keyboard.wKey.isPressed)
            {
                forwardInput += 1f;
            }

            if (keyboard.sKey.isPressed)
            {
                forwardInput -= 1f;
            }
        }

        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -1f;
        }

        if (controller.isGrounded &&
            keyboard.spaceKey.wasPressedThisFrame)
        {
            verticalVelocity = jumpSpeed;
        }

        verticalVelocity += gravity * Time.deltaTime;
        Vector3 movement = ResolveHorizontalMovement(
            horizontalInput,
            forwardInput) * moveSpeed +
            Vector3.up * verticalVelocity;
        controller.Move(movement * Time.deltaTime);
    }

    private Vector3 ResolveHorizontalMovement(
        float horizontalInput,
        float forwardInput)
    {
        if (cameraModeController == null ||
            cameraModeController.CurrentMode ==
            CameraViewMode.Side2D)
        {
            return RopeProjectionUtility.ScreenRight(
                projectionDirection) * horizontalInput;
        }

        Camera camera =
            cameraModeController.ControlledCamera;
        if (camera == null)
        {
            return RopeProjectionUtility.ScreenRight(
                projectionDirection) * horizontalInput;
        }

        Vector3 right = Vector3.ProjectOnPlane(
            camera.transform.right,
            Vector3.up);
        Vector3 forward = Vector3.ProjectOnPlane(
            camera.transform.forward,
            Vector3.up);
        if (right.sqrMagnitude <= 0.0001f)
        {
            right = RopeProjectionUtility.ScreenRight(
                projectionDirection);
        }

        if (forward.sqrMagnitude <= 0.0001f)
        {
            forward = Vector3.forward;
        }

        return right.normalized * horizontalInput +
               forward.normalized * forwardInput;
    }

    private bool IsSide2D()
    {
        return cameraModeController == null ||
               (cameraModeController.CurrentMode ==
                CameraViewMode.Side2D &&
                cameraModeController.TargetMode ==
                CameraViewMode.Side2D);
    }

    private bool IsPerspective3D()
    {
        return cameraModeController != null &&
               (cameraModeController.CurrentMode ==
                CameraViewMode.Perspective3D ||
                cameraModeController.TargetMode ==
                CameraViewMode.Perspective3D);
    }

    private static float DirectionToYaw(
        RopeProjectionDirection direction)
    {
        return Mathf.Repeat(
            (int)direction * 90f + 180f,
            360f) - 180f;
    }
}
