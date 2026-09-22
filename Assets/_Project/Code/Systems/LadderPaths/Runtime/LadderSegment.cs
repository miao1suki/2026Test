using System;
using UnityEngine;

namespace Project.LadderPaths
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class LadderSegment : MonoBehaviour
    {
        [SerializeField, Min(0.1f)]
        private float width = 2f;

        [SerializeField, Min(0.1f)]
        private float height = 2f;

        [SerializeField, Min(0.05f)]
        private float depth = 0.25f;

        [SerializeField, Range(2, 12)]
        private int rungCount = 5;

        [SerializeField, HideInInspector]
        private string segmentId;

        [SerializeField, HideInInspector]
        private Transform visualRoot;

        public float Width => width;
        public float Height => height;
        public float Depth => depth;
        public string SegmentId => segmentId;
        public bool HasVisuals => visualRoot != null;

        public Vector3 GetWorldEndpoint(LadderEndpoint endpoint)
        {
            float y = endpoint == LadderEndpoint.Bottom
                ? -height * 0.5f
                : height * 0.5f;
            return transform.TransformPoint(0f, y, 0f);
        }

        public LadderFaceFamily GetVisibleFaceFamily(
            LadderProjectionDirection direction)
        {
            Vector3 viewDepth = LadderProjectionUtility.ViewDepth(direction);
            float frontAmount = Mathf.Abs(Vector3.Dot(viewDepth, transform.forward));
            float sideAmount = Mathf.Abs(Vector3.Dot(viewDepth, transform.right));
            return frontAmount >= sideAmount
                ? LadderFaceFamily.FrontBack
                : LadderFaceFamily.LeftRight;
        }

        public float GetProjectedHalfWidth(LadderProjectionDirection direction)
        {
            return GetVisibleFaceFamily(direction) == LadderFaceFamily.FrontBack
                ? width * 0.5f
                : depth * 0.5f;
        }

        public Vector3 GetClosestPointOnCenterLine(Vector3 worldPosition)
        {
            Vector3 local = transform.InverseTransformPoint(worldPosition);
            local.x = 0f;
            local.y = Mathf.Clamp(local.y, -height * 0.5f, height * 0.5f);
            local.z = 0f;
            return transform.TransformPoint(local);
        }

        public bool EnsureVisuals(Material material = null)
        {
            bool changed = false;
            if (visualRoot == null)
            {
                Transform existing = transform.Find("__LadderVisual");
                if (existing == null)
                {
                    GameObject root = new GameObject("__LadderVisual");
                    root.transform.SetParent(transform, false);
                    existing = root.transform;
                }

                visualRoot = existing;
                changed = true;
            }

            changed |= EnsurePart("Rail_Left", material);
            changed |= EnsurePart("Rail_Right", material);
            for (int index = 0; index < rungCount; index++)
            {
                changed |= EnsurePart($"Rung_{index + 1:00}", material);
            }

            RemoveExtraRungs();
            RefreshVisuals();
            ConfigureCollider();
            return changed;
        }

        public void RefreshVisuals()
        {
            if (visualRoot == null)
            {
                return;
            }

            float railThickness = Mathf.Max(0.08f, width * 0.07f);
            float railX = Mathf.Max(0f, width * 0.5f - railThickness * 0.5f);
            SetPart(
                "Rail_Left",
                new Vector3(-railX, 0f, 0f),
                new Vector3(railThickness, height, depth));
            SetPart(
                "Rail_Right",
                new Vector3(railX, 0f, 0f),
                new Vector3(railThickness, height, depth));

            float rungThickness = Mathf.Max(0.06f, height * 0.045f);
            for (int index = 0; index < rungCount; index++)
            {
                float normalized = (index + 1f) / (rungCount + 1f);
                float y = Mathf.Lerp(-height * 0.5f, height * 0.5f, normalized);
                SetPart(
                    $"Rung_{index + 1:00}",
                    new Vector3(0f, y, 0f),
                    new Vector3(width, rungThickness, depth * 1.15f));
            }
        }

        private void Reset()
        {
            EnsureId();
            EnsureVisuals();
        }

        private void OnValidate()
        {
            EnsureId();
            width = Mathf.Max(0.1f, width);
            height = Mathf.Max(0.1f, height);
            depth = Mathf.Max(0.05f, depth);
            rungCount = Mathf.Clamp(rungCount, 2, 12);
            RefreshVisuals();
            ConfigureCollider();
            GetComponentInParent<LadderPathNetwork>()?.InvalidateCache();
        }

        private void EnsureId()
        {
            if (string.IsNullOrWhiteSpace(segmentId))
            {
                segmentId = Guid.NewGuid().ToString("N");
            }
        }

        private bool EnsurePart(string partName, Material material)
        {
            Transform part = visualRoot.Find(partName);
            bool changed = false;
            if (part == null)
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = partName;
                cube.transform.SetParent(visualRoot, false);
                Collider collider = cube.GetComponent<Collider>();
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

                part = cube.transform;
                changed = true;
            }

            MeshRenderer renderer = part.GetComponent<MeshRenderer>();
            if (material != null && renderer != null && renderer.sharedMaterial != material)
            {
                renderer.sharedMaterial = material;
                changed = true;
            }

            return changed;
        }

        private void SetPart(string partName, Vector3 position, Vector3 scale)
        {
            Transform part = visualRoot.Find(partName);
            if (part == null)
            {
                return;
            }

            part.localPosition = position;
            part.localRotation = Quaternion.identity;
            part.localScale = scale;
        }

        private void RemoveExtraRungs()
        {
            for (int index = visualRoot.childCount - 1; index >= 0; index--)
            {
                Transform child = visualRoot.GetChild(index);
                if (!child.name.StartsWith("Rung_", StringComparison.Ordinal) ||
                    !int.TryParse(child.name.Substring(5), out int rungIndex) ||
                    rungIndex <= rungCount)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private void ConfigureCollider()
        {
            BoxCollider box = GetComponent<BoxCollider>();
            if (box == null)
            {
                return;
            }

            box.isTrigger = true;
            box.center = Vector3.zero;
            box.size = new Vector3(width, height, depth);
        }
    }
}
