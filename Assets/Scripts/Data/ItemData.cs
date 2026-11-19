using Dungeon.Model;
using UnityEngine;

namespace Dungeon.Data
{
    /// <summary>DATOS. Definición de un ítem (lo que contiene un cofre).</summary>
    [CreateAssetMenu(menuName = "Dungeon/Item", fileName = "Item")]
    public class ItemData : ScriptableObject
    {
        public string itemName = "Espada de acero";
        public ItemType type = ItemType.Weapon;
        [Min(1)] public int value = 5;

        public Item CreateItem() => new Item(itemName, type, value);
    }
}
