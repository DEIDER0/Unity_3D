using System;

namespace Dungeon.Model
{
    /// <summary>
    /// MODELO. Cofre: guarda un ítem y recuerda si ya fue abierto.
    /// Regla: solo se puede abrir una vez.
    /// </summary>
    public sealed class ChestModel
    {
        /// <summary>Se emite con el ítem entregado la única vez que se abre.</summary>
        public event Action<Item> Opened;

        public Item Content { get; }
        public bool IsOpened { get; private set; }

        public ChestModel(Item content)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
        }

        /// <summary>Intenta abrir el cofre. Devuelve false si ya estaba abierto.</summary>
        public bool TryOpen(out Item item)
        {
            if (IsOpened)
            {
                item = null;
                return false;
            }

            IsOpened = true;
            item = Content;
            Opened?.Invoke(item);
            return true;
        }
    }
}
