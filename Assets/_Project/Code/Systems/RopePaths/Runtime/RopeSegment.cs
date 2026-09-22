using System;
using UnityEngine;

namespace Project.RopePaths
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class RopeSegment : MonoBehaviour
    {
        [SerializeField]
        private Vector3 endpointA = new Vector3(-1f, 0f, 0f);

        [SerializeField]
        private Vector3 endpointB = new Vector3(1f, 0f, 0f);

        [SerializeField, HideInInspector]
        private string segmentId;

        [SerializeField, HideInInspector]
        private GameObject visualRoot;

        [SerializeField, HideInInspector]
        private Transform ropeBodyVisual;

        [SerializeField, HideInInspector]
        private Transform endpointAVisual;

        [SerializeField, HideInInspector]
        private Transform endpointBVisual;

        [SerializeField, Min(0.01f)]
        private float ropeWidth = 0.08f;

        [SerializeField, Min(0.02f)]
        private float endpointRadius = 0.18f;

        public Vector3 LocalEndpointA => endpointA;
        public Vector3 LocalEndpointB => endpointB;
        public string SegmentId => segmentId;
        public bool HasVisuals => ropeBodyVisual != null &&
                                  endpointAVisual != null &&
                                  endpointBVisual != null;
        public float RopeWidth => ropeWidth;
        public float EndpointRadius => endpointRadius;

        public Vector3 GetWorldEndpoint(RopeEndpoint endpoint)
        {
            return transform.TransformPoint(
                endpoint == RopeEndpoint.A ? endpointA : endpointB);
        }

        public Vector3 GetWorldDirection(RopeEndpoint endpoint)
        {
            Vector3 delta = GetWorldEndpoint(RopeEndpoint.B) -
                            GetWorldEndpoint(RopeEndpoint.A);
            if (endpoint == RopeEndpoint.A)
            {
                delta = -delta;
            }

            return delta.sqrMagnitude > 0.000001f ? delta.normalized : Vector3.zero;
        }

        public Vector3 GetLocalEndpoint(RopeEndpoint endpoint)
        {
            return endpoint == RopeEndpoint.A ? endpointA : endpointB;
        }

        public void SetLocalEndpoint(RopeEndpoint endpoint, Vector3 localPosition)
        {
            if (endpoint == RopeEndpoint.A)
            {
                endpointA = localPosition;
            }
            else
            {
                endpointB = localPosition;
            }

            RefreshVisuals();
        }

        public void SetWorldEndpoint(RopeEndpoint endpoint, Vector3 worldPosition)
        {
            SetLocalEndpoint(endpoint, transform.InverseTransformPoint(worldPosition));
        }

        public bool EnsureVisuals(Material material = null)
        {
            bool changed = false;
            if (visualRoot == null)
            {
                visualRoot = FindOrCreateChild("__RopeVisual");
                changed = true;
            }

            ropeBodyVisual = EnsurePrimitiveVisual(
                ropeBodyVisual,
                "__RopeBody",
                PrimitiveType.Cylinder,
                ref changed);
            endpointAVisual = EnsurePrimitiveVisual(
                endpointAVisual,
                "__RopeEndpointA",
                PrimitiveType.Sphere,
                ref changed);
            endpointBVisual = EnsurePrimitiveVisual(
                endpointBVisual,
                "__RopeEndpointB",
                PrimitiveType.Sphere,
                ref changed);
            changed |= ApplyMaterial(ropeBodyVisual, material);
            changed |= ApplyMaterial(endpointAVisual, material);
            changed |= ApplyMaterial(endpointBVisual, material);
            RefreshVisuals();
            return changed;
        }

        public void RefreshVisuals()
        {
            if (ropeBodyVisual == null)
            {
                return;
            }

            Vector3 delta = endpointB - endpointA;
            float length = delta.magnitude;
            ropeBodyVisual.localPosition = Vector3.Lerp(endpointA, endpointB, 0.5f);
            ropeBodyVisual.localRotation = length > 0.000001f
                ? Quaternion.FromToRotation(Vector3.up, delta / length)
                : Quaternion.identity;
            ropeBodyVisual.localScale = new Vector3(
                ropeWidth,
                Mathf.Max(0.0001f, length * 0.5f),
                ropeWidth);
            if (endpointAVisual != null)
            {
                endpointAVisual.localPosition = endpointA;
                endpointAVisual.localScale = Vector3.one * endpointRadius * 2f;
            }

            if (endpointBVisual != null)
            {
                endpointBVisual.localPosition = endpointB;
                endpointBVisual.localScale = Vector3.one * endpointRadius * 2f;
            }
        }

        public static RopeEndpoint GetOpposite(RopeEndpoint endpoint)
        {
            return endpoint == RopeEndpoint.A ? RopeEndpoint.B : RopeEndpoint.A;
        }

        private void Reset()
        {
            EnsureId();
            EnsureVisuals();
        }

        private void OnValidate()
        {
            EnsureId();
            ropeWidth = Mathf.Max(0.01f, ropeWidth);
            endpointRadius = Mathf.Max(0.02f, endpointRadius);
            RefreshVisuals();
        }

        private void EnsureId()
        {
            if (string.IsNullOrWhiteSpace(segmentId))
            {
                segmentId = Guid.NewGuid().ToString("N");
            }
        }

        private GameObject FindOrCreateChild(string childName)
        {
            Transform child = transform.Find(childName);
            if (child != null)
            {
                return child.gameObject;
            }

            GameObject gameObject = new GameObject(childName);
            gameObject.transform.SetParent(transform, false);
            return gameObject;
        }

        private Transform EnsurePrimitiveVisual(
            Transform current,
            string childName,
            PrimitiveType primitiveType,
            ref bool changed)
        {
            if (current == null)
            {
                Transform existing = visualRoot.transform.Find(childName);
                if (existing != null)
                {
                    current = existing;
                }
                else
                {
                    GameObject primitive = GameObject.CreatePrimitive(primitiveType);
                    primitive.name = childName;
                    primitive.transform.SetParent(visualRoot.transform, false);
                    Collider collider = primitive.GetComponent<Collider>();
                    if (collider != null)
                    {
                        if (Application.isPlaying)
                        {
                            Destroy(collider);
                        }
                        else
                        {
                            DestroyImmediate(collider);
                        }
                    }

                    current = primitive.transform;
                }

                changed = true;
            }

            return current;
        }

        private static bool ApplyMaterial(Transform visual, Material material)
        {
            if (visual == null || material == null)
            {
                return false;
            }

            MeshRenderer renderer = visual.GetComponent<MeshRenderer>();
            if (renderer != null && renderer.sharedMaterial != material)
            {
                renderer.sharedMaterial = material;
                return true;
            }

            return false;
        }
    }
}


