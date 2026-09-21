using System.Collections.Generic;
using UnityEngine;

namespace Project.CubeMapEditing
{
    [CreateAssetMenu(
        fileName = "GridMapPalette",
        menuName = "2026Test/网格地图物品仓库")]
    public sealed class GridMapPalette : ScriptableObject
    {
        [SerializeField] private List<GridMapItemDefinition> items =
            new List<GridMapItemDefinition>();

        public IReadOnlyList<GridMapItemDefinition> Items => items;

        public bool Add(GridMapItemDefinition item)
        {
            if (item == null || items.Contains(item))
            {
                return false;
            }

            items.Add(item);
            return true;
        }

        public bool Remove(GridMapItemDefinition item)
        {
            return item != null && items.Remove(item);
        }

        private void OnValidate()
        {
            items.RemoveAll(item => item == null);
        }
    }
}
