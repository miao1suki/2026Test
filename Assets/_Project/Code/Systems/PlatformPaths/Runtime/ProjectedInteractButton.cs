using System;
using System.Collections.Generic;
using Project.CameraModes;
using Project.InputAbstraction;
using Project.RopePaths;
using UnityEngine;
using UnityEngine.Events;

namespace Project.PlatformPaths
{
    [AddComponentMenu("2026Test/Platform Paths/ProjectedInteractButton")]
    [DisallowMultipleComponent]
    public sealed class ProjectedInteractButton :
        MonoBehaviour,
        IInteractionTarget,
        IAchievementSignalProvider
    {
        private static readonly string[] AchievementSignals =
        {
            AchievementSignalIds.InteractButtonActivated
        };

        [Header("指挥平台")]
        [SerializeField]
        private PlatformMove[] commandedPlatforms =
            new PlatformMove[0];

        [Header("玩家交互")]
        [SerializeField]
        private string playerTag = "Player";

        [SerializeField, Min(0.05f)]
        private float interactionRange = 1.5f;

        [SerializeField]
        private CameraModeController cameraModeController;

        [SerializeField]
        private bool require2DFaceMatch = true;

        [SerializeField]
        private UnityEvent activatedEvent =
            new UnityEvent();

        public event Action Activated;
        public UnityEvent ActivatedEvent => activatedEvent;

        public IReadOnlyList<string> GetAchievementSignalIds()
        {
            return AchievementSignals;
        }

        private void Reset()
        {
            ResolveReferences();
        }

        private void Awake()
        {
            ResolveReferences();
        }

        public bool CanInteract(GameObject interactor)
        {
            if (interactor == null ||
                !IsPlayer(interactor.transform))
            {
                return false;
            }

            Vector3 interactorPoint =
                ResolveInteractorPoint(interactor.transform);
            if (Vector3.Distance(
                    transform.position,
                    interactorPoint) >
                interactionRange)
            {
                return false;
            }

            if (!require2DFaceMatch ||
                cameraModeController == null ||
                cameraModeController.TargetMode !=
                CameraViewMode.Side2D)
            {
                return true;
            }

            if (cameraModeController.IsTransitioning)
            {
                return false;
            }

            RopeProjectionDirection direction =
                RopeProjectionUtility.DirectionFromYaw(
                    cameraModeController.Side2DYaw);
            Vector2 buttonPosition =
                RopeProjectionUtility.Project(
                    transform.position,
                    direction);
            Vector2 interactorPosition =
                RopeProjectionUtility.Project(
                    interactorPoint,
                    direction);
            return Vector2.Distance(
                       buttonPosition,
                       interactorPosition) <=
                   interactionRange;
        }

        public bool TryInteract(GameObject interactor)
        {
            if (!CanInteract(interactor))
            {
                return false;
            }

            TriggerPlatforms();
            return true;
        }

        private void ResolveReferences()
        {
            if (cameraModeController == null)
            {
                cameraModeController =
                    FindFirstObjectByType<CameraModeController>();
            }
        }

        private bool IsPlayer(Transform candidate)
        {
            if (candidate == null ||
                string.IsNullOrWhiteSpace(playerTag))
            {
                return false;
            }

            return candidate.CompareTag(playerTag) ||
                   candidate.root.CompareTag(playerTag);
        }

        private static Vector3 ResolveInteractorPoint(
            Transform interactor)
        {
            Collider collider =
                interactor.GetComponentInChildren<Collider>();
            return collider != null
                ? collider.bounds.center
                : interactor.position;
        }

        private void TriggerPlatforms()
        {
            if (commandedPlatforms == null)
            {
                return;
            }

            for (int index = 0;
                 index < commandedPlatforms.Length;
                 index++)
            {
                PlatformMove platform =
                    commandedPlatforms[index];
                if (platform != null)
                {
                    platform.ReceiveButtonSignal(
                        transform.position);
                }
            }

            Activated?.Invoke();
            activatedEvent?.Invoke();
            GameplaySignalHub.Emit(
                AchievementSignalIds.InteractButtonActivated,
                gameObject);
        }
    }
}
