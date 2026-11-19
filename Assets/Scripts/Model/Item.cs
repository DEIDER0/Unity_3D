using System;

namespace Dungeon.Model
{
    /// <summary>
    /// MODELO. Ítem inmutable (nombre, tipo y valor). No sabe nada de cómo se dibuja.
    /// El efecto del ítem lo aplica PlayerState.AddItem().
    /// </summary>
    public sealed class Item
    {
        public string Name { get; }
        public ItemType Type { get; }
        public int Value { get; }

        public Item(string name, ItemType type, int value)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("El ítem necesita un nombre.", nameof(name));
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), "El valor del ítem debe ser mayor que 0.");
            Name = name;
            Type = type;
            Value = value;
        }

        public override string ToString() => $"{Name} ({Type} +{Value})";
    }
}
