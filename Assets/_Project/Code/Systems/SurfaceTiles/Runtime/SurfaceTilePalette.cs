using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.SurfaceTiles
{
    [CreateAssetMenu(
        fileName = "SurfaceTilePalette",
        menuName = "2026Test/Surface Tiles/Tile Palette")]
    public sealed class SurfaceTilePalette : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [SerializeField, HideInInspector] private string id;
            [SerializeField] private string displayName;
            [SerializeField] private Sprite sprite;

            public string Id => id;
            public string DisplayName => displayName;
            public Sprite Sprite => sprite;

            public Entry(Sprite value)
            {
                id = Guid.NewGuid().ToString("N");
                displayName = value != null ? value.name : "Tile";
                sprite = value;
            }

            internal void EnsureValid()
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    id = Guid.NewGuid().ToString("N");
                }

                if (string.IsNullOrWhiteSpace(displayName) && sprite != null)
                {
                    displayName = sprite.name;
                }
            }
        }

        [SerializeField] private List<Entry> tiles = new List<Entry>();
        [SerializeField] private Material previewMaterial;

        public IReadOnlyList<Entry> Tiles => tiles;
        public Material PreviewMaterial => previewMaterial;

        public bool TryGet(string id, out Entry entry)
        {
            entry = null;
            if (string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            for (int index = 0; index < tiles.Count; index++)
            {
                Entry candidate = tiles[index];
                if (candidate != null && candidate.Id == id)
                {
                    entry = candidate;
                    return true;
                }
            }

            return false;
        }

        public Texture2D GetSourceTexture()
        {
            for (int index = 0; index < tiles.Count; index++)
            {
                Sprite sprite = tiles[index]?.Sprite;
                if (sprite != null)
                {
                    return sprite.texture;
                }
            }

            return null;
        }

        public bool UsesSingleTexture()
        {
            Texture2D texture = null;
            for (int index = 0; index < tiles.Count; index++)
            {
                Sprite sprite = tiles[index]?.Sprite;
                if (sprite == null)
                {
                    continue;
                }

                texture ??= sprite.texture;
                if (sprite.texture != texture)
                {
                    return false;
                }
            }

            return true;
        }

        public void ReplaceTiles(IEnumerable<Sprite> sprites)
        {
            tiles.Clear();
            if (sprites == null)
            {
                return;
            }

            foreach (Sprite sprite in sprites)
            {
                if (sprite != null)
                {
                    tiles.Add(new Entry(sprite));
                }
            }
        }

        public void SetPreviewMaterial(Material material)
        {
            previewMaterial = material;
        }

        private void OnValidate()
        {
            for (int index = 0; index < tiles.Count; index++)
            {
                tiles[index]?.EnsureValid();
            }
        }
    }
}
