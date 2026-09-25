using System.Collections.Generic;
using Project.CameraModes;
using Project.InputAbstraction;
using Project.LadderPaths;
using Project.PlatformPaths;
using Project.RopePaths;
using UnityEngine;

namespace Project.Player
{
    public enum PlayerCrouchBehavior
    {
        Hold = 0,
        Toggle = 1
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(PlayerActionRunner))]
    public sealed class PlayerController :
        MonoBehaviour,
        ILadderClimbStateReceiver
    {
        [Header("Players")]
        [SerializeField]
        private Rigidbody motor;

        [SerializeField]
        private CapsuleCollider bodyCollider;

        [SerializeField]
        private CameraFollowController cameraFollow;

        [SerializeField]
        private CameraModeController cameraMode;

        [SerializeField]
        private PlayerActionRunner actionRunner;

        [SerializeField]
        private PlatformRider platformRider;

        [SerializeField]
        private LadderClimbSensor ladderSensor;

        [SerializeField]
        private RopePathNetwork[] ropeNetworks;

        [Header("Actions")]
        [SerializeField]
        private PlayerActionBinding[] actionBindings =
            new PlayerActionBinding[0];

        [Header("Movement")]
        [SerializeField]
        private float moveSpeed = 5f;

        [SerializeField]
        private float jumpSpeed = 7f;

        [SerializeField]
        private float climbSpeed = 2f;

        [SerializeField]
        private float lookSensitivity = 0.12f;

        [SerializeField]
        private float sprintMultiplier = 1.5f;

        [SerializeField, Min(0.1f)]
        private float crouchHeight = 0.7f;

        [SerializeField, Range(0.1f, 1f)]
        private float crouchSpeedMultiplier = 0.5f;

        [SerializeField]
        private PlayerCrouchBehavior crouchBehavior =
            PlayerCrouchBehavior.Toggle;

        [SerializeField, Min(0.01f)]
        private float groundCheckDistance = 0.12f;

        [SerializeField, Min(0f)]
        private float coyoteTime = 0.12f;

        [SerializeField, Min(0f)]
        private float jumpBufferTime = 0.12f;

        [SerializeField]
        private LayerMask groundMask = ~0;

        private PlayerStateContext context;
        private PlayerStateMachine stateMachine;
        private readonly Dictionary<InputActionId, ActSO> actionMap =
            new Dictionary<InputActionId, ActSO>();
        private readonly RaycastHit[] groundHits =
            new RaycastHit[8];
        private LadderPathNetwork[] ladderNetworks;
        private LadderPathNetwork activeLadderNetwork;
        private RopeProjectionDirection projectionDirection =
            RopeProjectionDirection.Front;
        private float standingHeight;
        private float coyoteTimer;
        private float jumpBufferTimer;
        private bool isGrounded;
        private bool isCrouched;
        private bool crouchToggle;
        private bool isSprinting;
        private bool sprintToggle;
        private int controlLockDepth;
        private LadderClimbContact pendingLadderContact;
        private bool hasPendingLadderContact;

        public event System.Action<ActSO> ActionStarted;
        public event System.Action<ActSO> ActionCompleted;
        public event System.Action<bool> ControlLockChanged;
        public event System.Action TimelineSignalReceived;

        public Rigidbody Motor => motor;
        public CameraFollowController CameraFollow => cameraFollow;
        public CameraModeController CameraMode => cameraMode;
        public RopePathNetwork[] RopeNetworks => ropeNetworks;
        public float ClimbSpeed => climbSpeed;
        public bool IsGrounded => isGrounded;
        public bool IsCrouched => isCrouched;
        public bool IsSprinting => isSprinting;
        public PlayerCrouchBehavior CrouchBehavior =>
            crouchBehavior;
        public bool IsControlLocked => controlLockDepth > 0;
        public bool IsRidingPlatform =>
            platformRider != null &&
            platformRider.IsRiding;
        public PlayerStateId CurrentStateId =>
            stateMachine != null
                ? stateMachine.CurrentId
                : PlayerStateId.Normal;
        public RopeProjectionDirection ProjectionDirection =>
            projectionDirection;
        public bool IsActionState =>
            stateMachine != null &&
            stateMachine.CurrentId == PlayerStateId.Action;

        private void Awake()
        {
            ResolveReferences();
            ResolveLadderNetworks();
            BuildActionMap();
            context = new PlayerStateContext(this, actionRunner);
            stateMachine = new PlayerStateMachine();
            stateMachine.Register(
                PlayerStateId.Normal,
                new PlayerNormalState());
            stateMachine.Register(
                PlayerStateId.Action,
                new PlayerActionState());
            stateMachine.Register(
                PlayerStateId.Climbing,
                new PlayerClimbState());
            stateMachine.Register(
                PlayerStateId.Locked,
                new PlayerLockedState());
            stateMachine.Start(PlayerStateId.Normal, context);
            Apply2DDirection();
        }

        private void Update()
        {
            if (platformRider == null)
            {
                platformRider = GetComponent<PlatformRider>();
            }

            RefreshGroundedState();
            RefreshLadderNetwork();
            stateMachine.Tick(context);
        }

        public bool TryPlayAction(ActSO action)
        {
            if (action == null ||
                action.Timeline == null ||
                stateMachine.CurrentId == PlayerStateId.Locked)
            {
                return false;
            }

            ActSO current = context.CurrentAction;
            if (current != null &&
                action.Priority < current.Priority)
            {
                return false;
            }

            if (stateMachine.CurrentId == PlayerStateId.Action)
            {
                if (current != null &&
                    !current.Interruptible)
                {
                    return false;
                }

                ReturnToNormal();
            }

            if (stateMachine.CurrentId == PlayerStateId.Climbing)
            {
                return false;
            }

            ResetStatefulInputs();
            context.CurrentAction = action;
            bool started = stateMachine.Change(
                PlayerStateId.Action,
                context);
            if (started && stateMachine.CurrentId == PlayerStateId.Action)
            {
                ActionStarted?.Invoke(action);
            }

            return started &&
                   stateMachine.CurrentId == PlayerStateId.Action;
        }

        public bool TryPlayAction(InputActionId inputAction)
        {
            return actionMap.TryGetValue(
                       inputAction,
                       out ActSO action) &&
                   TryPlayAction(action);
        }

        public bool HasActionBinding(InputActionId inputAction)
        {
            return actionMap.ContainsKey(inputAction);
        }

        public void ReturnToNormal()
        {
            stateMachine.Change(
                controlLockDepth > 0
                    ? PlayerStateId.Locked
                    : PlayerStateId.Normal,
                context);
        }

        public void Configure(
            CameraFollowController follow,
            RopePathNetwork[] networks)
        {
            cameraFollow = follow;
            ropeNetworks = networks;
            ResolveReferences();
        }

        public void SetFallbackNormalState()
        {
            motor.useGravity = true;
        }

        public void PushControlLock()
        {
            controlLockDepth++;
            if (controlLockDepth == 1)
            {
                ResetStatefulInputs();
                bool changed = stateMachine.Change(
                    PlayerStateId.Locked,
                    context);
                if (changed)
                {
                    ControlLockChanged?.Invoke(true);
                }
            }
        }

        public void PopControlLock()
        {
            if (controlLockDepth <= 0)
            {
                return;
            }

            controlLockDepth--;
            if (controlLockDepth == 0 &&
                stateMachine.CurrentId == PlayerStateId.Locked)
            {
                stateMachine.Change(PlayerStateId.Normal, context);
                ControlLockChanged?.Invoke(false);
            }
        }

        public void SetControlLocked(bool locked)
        {
            if (locked)
            {
                PushControlLock();
            }
            else
            {
                PopControlLock();
            }
        }

        public bool HasActionInput()
        {
            if (GameInput.IsPressed(InputActionId.Move) ||
                GameInput.IsPressed(InputActionId.Look))
            {
                return true;
            }

            foreach (InputActionId actionId in actionMap.Keys)
            {
                if (GameInput.WasTriggeredThisFrame(actionId))
                {
                    return true;
                }
            }

            return false;
        }

        public void Update2DRotation()
        {
            if (GameInput.WasTriggeredThisFrame(
                    InputActionId.PointerPrimary))
            {
                Rotate2D(-1);
            }

            if (GameInput.WasTriggeredThisFrame(
                    InputActionId.PointerSecondary))
            {
                Rotate2D(1);
            }
        }

        public void Update3DLook()
        {
            if (cameraMode == null)
            {
                return;
            }

            Vector2 look =
                GameInput.ReadVector2(InputActionId.Look);
            if (look.sqrMagnitude <= 0.0001f)
            {
                look = GameInput.PointerDelta;
            }

            cameraMode.RotatePerspective(
                look.x * lookSensitivity,
                -look.y * lookSensitivity);
        }

        public void Move2D(float horizontalInput)
        {
            Vector3 direction =
                RopeProjectionUtility.ScreenRight(
                    projectionDirection) *
                horizontalInput;
            MoveHorizontal(direction);
        }

        public void Move3D(Vector2 input)
        {
            Camera camera = cameraMode != null
                ? cameraMode.ControlledCamera
                : null;
            if (camera == null)
            {
                StopHorizontalMovement();
                return;
            }

            Vector3 right = Vector3.ProjectOnPlane(
                camera.transform.right,
                Vector3.up).normalized;
            Vector3 forward = Vector3.ProjectOnPlane(
                camera.transform.forward,
                Vector3.up).normalized;
            MoveHorizontal(
                right * input.x +
                forward * input.y);
        }

        public void UpdateJump()
        {
            bool riding =
                platformRider != null &&
                platformRider.IsRiding;

            if (GameInput.WasTriggeredThisFrame(InputActionId.Jump))
            {
                ResetStatefulInputs();
            }

            if (isCrouched)
            {
                jumpBufferTimer = 0f;
                return;
            }

            if (GameInput.WasTriggeredThisFrame(InputActionId.Jump))
            {
                jumpBufferTimer = jumpBufferTime;
                if (riding)
                {
                    platformRider.Detach();
                    riding = false;
                }
            }
            else
            {
                jumpBufferTimer -= Time.deltaTime;
            }

            if (riding)
            {
                coyoteTimer = coyoteTime;
                return;
            }

            if (isGrounded)
            {
                coyoteTimer = coyoteTime;
                if (motor.linearVelocity.y < 0f)
                {
                    SetVerticalVelocity(-0.5f);
                }
            }
            else
            {
                coyoteTimer -= Time.deltaTime;
            }

            if (jumpBufferTimer <= 0f ||
                coyoteTimer <= 0f ||
                stateMachine.CurrentId != PlayerStateId.Normal)
            {
                return;
            }

            SetVerticalVelocity(jumpSpeed);
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }

        public void SetClimbing(bool climbing)
        {
            if (climbing &&
                platformRider != null &&
                platformRider.IsRiding)
            {
                platformRider.Detach();
            }

            motor.useGravity = !climbing;
            if (climbing && !motor.isKinematic)
            {
                motor.linearVelocity = Vector3.zero;
            }
            else if (!climbing)
            {
                StopHorizontalMovement();
            }
        }

        public void ToggleViewMode()
        {
            if (IsSide2D())
            {
                cameraFollow.SetRequestedMode(
                    CameraViewMode.Perspective3D);
                return;
            }

            Apply2DDirection();
        }

        public bool IsSide2D()
        {
            return cameraMode == null ||
                   (cameraMode.CurrentMode ==
                    CameraViewMode.Side2D &&
                    cameraMode.TargetMode ==
                    CameraViewMode.Side2D);
        }

        public void UpdateMovementFromCurrentView(Vector2 input)
        {
            if (IsSide2D())
            {
                Move2D(input.x);
                return;
            }

            Move3D(input);
        }

        public void StopHorizontalMovement()
        {
            if (motor == null || motor.isKinematic)
            {
                return;
            }

            Vector3 velocity = motor.linearVelocity;
            velocity.x = 0f;
            velocity.z = 0f;
            motor.linearVelocity = velocity;
        }

        public void UpdateCrouch()
        {
            if (crouchBehavior ==
                    PlayerCrouchBehavior.Hold ||
                GameInput.GetActionTrigger(
                    InputActionId.Crouch) ==
                InputActionTrigger.Hold)
            {
                SetCrouched(
                    GameInput.IsPressed(
                        InputActionId.Crouch));
                return;
            }

            if (GameInput.WasTriggeredThisFrame(
                    InputActionId.Crouch))
            {
                bool desired = !crouchToggle;
                if (SetCrouched(desired))
                {
                    crouchToggle = desired;
                }
            }
        }

        public void UpdateSprint()
        {
            InputActionTrigger trigger =
                GameInput.GetActionTrigger(InputActionId.Sprint);
            if (trigger == InputActionTrigger.Hold)
            {
                isSprinting = GameInput.IsPressed(
                    InputActionId.Sprint);
                sprintToggle = isSprinting;
                return;
            }

            if (GameInput.WasTriggeredThisFrame(
                    InputActionId.Sprint))
            {
                sprintToggle = !sprintToggle;
            }

            isSprinting = sprintToggle;
        }

        public bool CancelCrouch()
        {
            crouchToggle = false;
            return SetCrouched(false);
        }

        public void CancelSprint()
        {
            sprintToggle = false;
            isSprinting = false;
        }

        public bool SetCrouched(bool crouched)
        {
            if (bodyCollider == null || isCrouched == crouched)
            {
                return true;
            }

            if (!crouched && !CanStand())
            {
                return false;
            }

            isCrouched = crouched;
            float height = crouched
                ? Mathf.Min(crouchHeight, standingHeight)
                : standingHeight;
            Vector3 center = bodyCollider.center;
            center.y = -(standingHeight - height) * 0.5f;
            bodyCollider.height = height;
            bodyCollider.center = center;
            return true;
        }

        public void NotifyActionCompleted(ActSO action)
        {
            if (action != null)
            {
                ActionCompleted?.Invoke(action);
            }
        }

        public void ReceiveTimelineSignal()
        {
            TimelineSignalReceived?.Invoke();
        }

        public void OnLadderClimbEnter(LadderClimbContact contact)
        {
            pendingLadderContact = contact;
            hasPendingLadderContact = true;
            TryEnterClimb();
        }

        public void OnLadderClimbStay(LadderClimbContact contact)
        {
            pendingLadderContact = contact;
            hasPendingLadderContact = true;
            TryEnterClimb();
        }

        public void OnLadderClimbExit(
            LadderClimbExitReason reason)
        {
            hasPendingLadderContact = false;
            if (stateMachine.CurrentId != PlayerStateId.Climbing)
            {
                return;
            }

            context.CurrentLadder = null;
            ReturnToNormal();
        }

        private void TryEnterClimb()
        {
            if (!hasPendingLadderContact ||
                controlLockDepth > 0 ||
                stateMachine.CurrentId == PlayerStateId.Action ||
                stateMachine.CurrentId == PlayerStateId.Climbing)
            {
                return;
            }

            float vertical =
                GameInput.ReadVector2(InputActionId.Move).y;
            if (Mathf.Abs(vertical) <= 0.1f)
            {
                return;
            }

            if (platformRider != null &&
                platformRider.IsRiding)
            {
                platformRider.Detach();
            }

            ResetStatefulInputs();
            context.CurrentLadder =
                pendingLadderContact.Segment;
            if (stateMachine.Change(
                    PlayerStateId.Climbing,
                    context))
            {
                hasPendingLadderContact = false;
            }
        }

        private void MoveHorizontal(Vector3 direction)
        {
            if (direction.sqrMagnitude > 1f)
            {
                direction.Normalize();
            }

            if (platformRider != null &&
                platformRider.IsRiding)
            {
                transform.position +=
                    direction *
                    ResolveMoveSpeed() *
                    Time.deltaTime;
                return;
            }

            Vector3 velocity = motor.linearVelocity;
            velocity.x = direction.x * ResolveMoveSpeed();
            velocity.z = direction.z * ResolveMoveSpeed();
            motor.linearVelocity = velocity;
        }

        private void ResetStatefulInputs()
        {
            CancelCrouch();
            CancelSprint();
        }

        private float ResolveMoveSpeed()
        {
            float speed = isSprinting
                ? moveSpeed * sprintMultiplier
                : moveSpeed;
            return isCrouched
                ? speed * crouchSpeedMultiplier
                : speed;
        }

        private void BuildActionMap()
        {
            actionMap.Clear();
            if (actionBindings == null)
            {
                return;
            }

            for (int index = 0;
                 index < actionBindings.Length;
                 index++)
            {
                PlayerActionBinding binding = actionBindings[index];
                if (binding.action != null)
                {
                    actionMap[binding.inputAction] = binding.action;
                }
            }
        }

        private void Rotate2D(int quarterTurns)
        {
            int next = Mathf.RoundToInt(
                Mathf.Repeat(
                    (int)projectionDirection + quarterTurns,
                    4));
            projectionDirection =
                (RopeProjectionDirection)next;
            Apply2DDirection();
        }

        private void Apply2DDirection()
        {
            if (ropeNetworks != null)
            {
                for (int index = 0;
                     index < ropeNetworks.Length;
                     index++)
                {
                    if (ropeNetworks[index] != null)
                    {
                        ropeNetworks[index]
                            .CurrentProjectionDirection =
                            projectionDirection;
                    }
                }
            }

            if (ladderSensor != null)
            {
                ladderSensor.ProjectionDirection =
                    (LadderProjectionDirection)projectionDirection;
            }

            cameraFollow?.SetRequestedMode(
                CameraViewMode.Side2D,
                DirectionToYaw(projectionDirection));
        }

        private void ResolveReferences()
        {
            motor = motor != null
                ? motor
                : GetComponent<Rigidbody>();
            bodyCollider = bodyCollider != null
                ? bodyCollider
                : GetComponent<CapsuleCollider>();
            if (bodyCollider != null && standingHeight <= 0f)
            {
                standingHeight = bodyCollider.height;
            }
            actionRunner = actionRunner != null
                ? actionRunner
                : GetComponent<PlayerActionRunner>();
            platformRider = platformRider != null
                ? platformRider
                : GetComponent<PlatformRider>();
            if (cameraFollow == null)
            {
                cameraFollow =
                    FindFirstObjectByType<CameraFollowController>();
            }

            if (cameraMode == null)
            {
                cameraMode =
                    FindFirstObjectByType<CameraModeController>();
            }

            if (ladderSensor == null)
            {
                ladderSensor = GetComponent<LadderClimbSensor>();
            }

            if (ladderSensor != null)
            {
                ladderSensor.ProjectionDirection =
                    (LadderProjectionDirection)projectionDirection;
            }
        }

        private void ResolveLadderNetworks()
        {
            ladderNetworks =
                FindObjectsByType<LadderPathNetwork>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);
        }

        private void RefreshLadderNetwork()
        {
            if (ladderSensor == null ||
                stateMachine.CurrentId == PlayerStateId.Climbing)
            {
                return;
            }

            LadderPathNetwork nearest = null;
            float nearestDistance = float.MaxValue;
            for (int networkIndex = 0;
                 networkIndex < ladderNetworks.Length;
                 networkIndex++)
            {
                LadderPathNetwork network =
                    ladderNetworks[networkIndex];
                if (network == null)
                {
                    continue;
                }

                IReadOnlyList<LadderSegment> segments =
                    network.Segments;
                for (int segmentIndex = 0;
                     segmentIndex < segments.Count;
                     segmentIndex++)
                {
                    LadderSegment segment =
                        segments[segmentIndex];
                    if (segment == null ||
                        !segment.isActiveAndEnabled)
                    {
                        continue;
                    }

                    Vector3 point =
                        segment.GetClosestPointOnCenterLine(
                            transform.position);
                    float distance =
                        (point - transform.position).sqrMagnitude;
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearest = network;
                    }
                }
            }

            if (nearest == activeLadderNetwork)
            {
                return;
            }

            activeLadderNetwork = nearest;
            ladderSensor.SetNetwork(nearest);
        }

        private bool CanStand()
        {
            Vector3 scale = transform.lossyScale;
            float radius = bodyCollider.radius *
                Mathf.Max(
                    Mathf.Abs(scale.x),
                    Mathf.Abs(scale.z));
            float height = Mathf.Max(
                standingHeight * Mathf.Abs(scale.y),
                radius * 2f);
            Vector3 center = transform.TransformPoint(
                new Vector3(
                    bodyCollider.center.x,
                    0f,
                    bodyCollider.center.z));
            float half = Mathf.Max(0f, height * 0.5f - radius);
            Vector3 bottom = center - Vector3.up * half;
            Vector3 top = center + Vector3.up * half;
            Collider[] hits = Physics.OverlapCapsule(
                bottom,
                top,
                radius * 0.98f,
                groundMask,
                QueryTriggerInteraction.Ignore);
            for (int index = 0; index < hits.Length; index++)
            {
                Collider hit = hits[index];
                if (hit != null && !hit.transform.IsChildOf(transform))
                {
                    return false;
                }
            }

            return true;
        }

        private void RefreshGroundedState()
        {
            if (bodyCollider == null)
            {
                isGrounded = false;
                return;
            }

            Vector3 scale = transform.lossyScale;
            float radius = bodyCollider.radius *
                Mathf.Max(
                    Mathf.Abs(scale.x),
                    Mathf.Abs(scale.z));
            float height = Mathf.Max(
                bodyCollider.height * Mathf.Abs(scale.y),
                radius * 2f);
            Vector3 center =
                transform.TransformPoint(bodyCollider.center);
            float bottomOffset =
                Mathf.Max(0f, height * 0.5f - radius);
            Vector3 bottom = center - Vector3.up * bottomOffset;
            float distance = radius + groundCheckDistance;
            isGrounded =
                ProbeGround(bottom, distance) ||
                ProbeGround(
                    bottom + transform.right * radius * 0.7f,
                    distance) ||
                ProbeGround(
                    bottom - transform.right * radius * 0.7f,
                    distance) ||
                ProbeGround(
                    bottom + transform.forward * radius * 0.7f,
                    distance) ||
                ProbeGround(
                    bottom - transform.forward * radius * 0.7f,
                    distance);
        }

        private bool ProbeGround(
            Vector3 origin,
            float distance)
        {
            int hitCount = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                groundHits,
                distance,
                groundMask,
                QueryTriggerInteraction.Ignore);
            for (int index = 0; index < hitCount; index++)
            {
                Transform hit = groundHits[index].transform;
                if (hit != null && !hit.IsChildOf(transform))
                {
                    return true;
                }
            }

            return false;
        }

        private void SetVerticalVelocity(float value)
        {
            if (motor == null || motor.isKinematic)
            {
                return;
            }

            Vector3 velocity = motor.linearVelocity;
            velocity.y = value;
            motor.linearVelocity = velocity;
        }

        private static float DirectionToYaw(
            RopeProjectionDirection direction)
        {
            return Mathf.Repeat(
                (int)direction * 90f + 180f,
                360f) - 180f;
        }
    }
}
