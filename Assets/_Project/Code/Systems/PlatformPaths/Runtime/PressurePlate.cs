using UnityEngine;

namespace Project.PlatformPaths
{
    [DisallowMultipleComponent]
    public sealed class PressurePlate : MonoBehaviour
    {
        [Header("指挥平台")]
        [SerializeField]
        private PlatformMove[] commandedPlatforms =
            new PlatformMove[0];

        [Header("玩家检测")]
        [SerializeField]
        private string playerTag = "Player";

        [SerializeField, Min(0.05f)]
        private float checkHeight = 0.5f;

        [SerializeField, Range(0.1f, 1f)]
        private float checkWidth = 0.9f;

        private readonly Collider[] overlapBuffer = new Collider[16];
        private bool pressed;

        public bool IsPressed => pressed;

        private void Update()
        {
            bool occupied = HasPlayerOnPlate();
            if (occupied && !pressed)
            {
                pressed = true;
                TriggerPlatforms();
                return;
            }

            if (!occupied && pressed)
            {
                pressed = false;
            }
        }

        public void TriggerPlatforms()
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
                    platform.ReceiveButtonSignal(transform.position);
                }
            }
        }

        private bool HasPlayerOnPlate()
        {
            Collider plateCollider = GetComponent<Collider>();
            if (plateCollider == null)
            {
                return false;
            }

            Bounds bounds = plateCollider.bounds;
            float halfHeight = Mathf.Max(0.05f, checkHeight) * 0.5f;
            Vector3 center = new Vector3(
                bounds.center.x,
                bounds.max.y + halfHeight * 0.5f,
                bounds.center.z);
            Vector3 halfExtents = new Vector3(
                Mathf.Max(0.05f, bounds.extents.x * checkWidth),
                halfHeight,
                Mathf.Max(0.05f, bounds.extents.z * checkWidth));
            int hitCount = Physics.OverlapBoxNonAlloc(
                center,
                halfExtents,
                overlapBuffer,
                transform.rotation,
                ~0,
                QueryTriggerInteraction.Collide);

            for (int index = 0; index < hitCount; index++)
            {
                Collider collider = overlapBuffer[index];
                if (collider == null ||
                    collider.transform == transform ||
                    collider.transform.IsChildOf(transform) ||
                    string.IsNullOrWhiteSpace(playerTag))
                {
                    continue;
                }

                if (collider.CompareTag(playerTag) ||
                    collider.transform.root.CompareTag(playerTag))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
