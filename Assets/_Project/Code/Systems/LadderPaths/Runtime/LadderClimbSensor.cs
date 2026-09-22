using System.Collections.Generic;
using UnityEngine;

namespace Project.LadderPaths
{
    [DisallowMultipleComponent]
    public sealed class LadderClimbSensor : MonoBehaviour
    {
        [SerializeField]
        private LadderPathNetwork network;

        [SerializeField]
        private MonoBehaviour stateReceiver;

        [SerializeField]
        private LadderProjectionDirection projectionDirection =
            LadderProjectionDirection.Front;

        [SerializeField]
        private bool useProjectionFallback = true;

        [SerializeField, Min(0.02f)]
        private float projectionQueryInterval = 0.05f;

        [SerializeField, Min(0f)]
        private float horizontalPadding = 0.2f;

        [SerializeField, Min(0f)]
        private float verticalPadding = 0.15f;

        [SerializeField]
        private LayerMask obstructionMask = ~0;

        [SerializeField, Min(0f)]
        private float topExitGraceSeconds = 0.18f;

        private readonly HashSet<LadderSegment> triggerContacts =
            new HashSet<LadderSegment>();

        private ILadderClimbStateReceiver receiver;
        private LadderClimbContact activeContact;
        private LadderClimbContact projectedContact;
        private bool hasActiveContact;
        private bool hasProjectedContact;
        private float nextProjectionQueryTime;
        private float graceExpiresAt;
        private bool usingGrace;

        public LadderProjectionDirection ProjectionDirection
        {
            get => projectionDirection;
            set
            {
                projectionDirection = value;
                hasProjectedContact = false;
                nextProjectionQueryTime = 0f;
            }
        }

        public bool IsClimbing => hasActiveContact;
        public LadderSegment ActiveSegment => hasActiveContact
            ? activeContact.Segment
            : null;

        public void SetNetwork(LadderPathNetwork value)
        {
            network = value;
            hasProjectedContact = false;
        }

        public bool SetStateReceiver(MonoBehaviour value)
        {
            if (value != null && !(value is ILadderClimbStateReceiver))
            {
                return false;
            }

            stateReceiver = value;
            receiver = value as ILadderClimbStateReceiver;
            return true;
        }

        public void RefreshNow()
        {
            nextProjectionQueryTime = 0f;
            EvaluateClimbState();
        }

        private void Awake()
        {
            ResolveReceiver();
        }

        private void FixedUpdate()
        {
            EvaluateClimbState();
        }

        private void OnDisable()
        {
            if (hasActiveContact)
            {
                ResolveReceiver();
                receiver?.OnLadderClimbExit(LadderClimbExitReason.SensorDisabled);
            }

            triggerContacts.Clear();
            hasActiveContact = false;
            hasProjectedContact = false;
            usingGrace = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            AddTriggerContact(other);
        }

        private void OnTriggerStay(Collider other)
        {
            AddTriggerContact(other);
        }

        private void OnTriggerExit(Collider other)
        {
            LadderSegment ladder = other.GetComponentInParent<LadderSegment>();
            if (ladder != null)
            {
                triggerContacts.Remove(ladder);
            }
        }

        private void AddTriggerContact(Collider other)
        {
            LadderSegment ladder = other.GetComponentInParent<LadderSegment>();
            if (ladder != null)
            {
                triggerContacts.Add(ladder);
            }
        }

        private void EvaluateClimbState()
        {
            ResolveReceiver();
            if (receiver == null)
            {
                return;
            }

            if (TryGetTriggerContact(out LadderClimbContact triggerContact))
            {
                ApplyContact(triggerContact);
                return;
            }

            if (useProjectionFallback && TryGetProjectedContact(out LadderClimbContact contact))
            {
                ApplyContact(contact);
                return;
            }

            if (!hasActiveContact)
            {
                return;
            }

            LadderPathNetwork activeNetwork = activeContact.Network;
            if (!usingGrace &&
                activeNetwork != null &&
                activeNetwork.CanUseTopExitGrace(
                    activeContact.Segment,
                    transform.position,
                    projectionDirection,
                    horizontalPadding,
                    verticalPadding))
            {
                usingGrace = true;
                graceExpiresAt = Time.time + topExitGraceSeconds;
            }

            if (usingGrace && Time.time <= graceExpiresAt)
            {
                activeContact = new LadderClimbContact(
                    activeNetwork,
                    activeContact.Segment,
                    projectionDirection,
                    activeContact.Segment.GetWorldEndpoint(LadderEndpoint.Top),
                    LadderContactSource.TopExitGrace);
                receiver.OnLadderClimbStay(activeContact);
                return;
            }

            LadderClimbExitReason reason = usingGrace
                ? LadderClimbExitReason.GraceExpired
                : LadderClimbExitReason.LostContact;
            receiver.OnLadderClimbExit(reason);
            hasActiveContact = false;
            usingGrace = false;
        }

        private void ApplyContact(LadderClimbContact contact)
        {
            bool changedSegment = !hasActiveContact ||
                                  activeContact.Segment != contact.Segment;
            if (hasActiveContact && changedSegment)
            {
                receiver.OnLadderClimbExit(LadderClimbExitReason.LostContact);
                hasActiveContact = false;
            }

            activeContact = contact;
            usingGrace = false;
            if (!hasActiveContact)
            {
                hasActiveContact = true;
                receiver.OnLadderClimbEnter(contact);
            }
            else
            {
                receiver.OnLadderClimbStay(contact);
            }
        }

        private bool TryGetTriggerContact(out LadderClimbContact contact)
        {
            LadderSegment best = null;
            float bestDistance = float.MaxValue;
            triggerContacts.RemoveWhere(item => item == null || !item.isActiveAndEnabled);
            foreach (LadderSegment ladder in triggerContacts)
            {
                Vector3 point = ladder.GetClosestPointOnCenterLine(transform.position);
                float squaredDistance = (point - transform.position).sqrMagnitude;
                if (squaredDistance < bestDistance)
                {
                    bestDistance = squaredDistance;
                    best = ladder;
                }
            }

            if (best == null)
            {
                contact = default;
                return false;
            }

            LadderPathNetwork owner = best.GetComponentInParent<LadderPathNetwork>();
            contact = new LadderClimbContact(
                owner,
                best,
                projectionDirection,
                best.GetClosestPointOnCenterLine(transform.position),
                LadderContactSource.Trigger);
            return true;
        }

        private bool TryGetProjectedContact(out LadderClimbContact contact)
        {
            if (network == null)
            {
                contact = default;
                return false;
            }

            if (Time.time >= nextProjectionQueryTime)
            {
                nextProjectionQueryTime = Time.time + projectionQueryInterval;
                hasProjectedContact = network.TryFindClimbableLadder(
                    transform.position,
                    projectionDirection,
                    horizontalPadding,
                    verticalPadding,
                    obstructionMask,
                    out projectedContact);
            }

            contact = projectedContact;
            return hasProjectedContact;
        }

        private void ResolveReceiver()
        {
            if (receiver != null && stateReceiver != null)
            {
                return;
            }

            receiver = null;

            if (stateReceiver != null)
            {
                receiver = stateReceiver as ILadderClimbStateReceiver;
                return;
            }

            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            for (int index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is ILadderClimbStateReceiver candidate)
                {
                    stateReceiver = behaviours[index];
                    receiver = candidate;
                    return;
                }
            }
        }

        private void OnValidate()
        {
            projectionQueryInterval = Mathf.Max(0.02f, projectionQueryInterval);
            horizontalPadding = Mathf.Max(0f, horizontalPadding);
            verticalPadding = Mathf.Max(0f, verticalPadding);
            topExitGraceSeconds = Mathf.Max(0f, topExitGraceSeconds);
            if (stateReceiver != null && !(stateReceiver is ILadderClimbStateReceiver))
            {
                stateReceiver = null;
            }

            receiver = stateReceiver as ILadderClimbStateReceiver;
        }
    }
}
