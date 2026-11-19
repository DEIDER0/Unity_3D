namespace Dungeon.Model
{
    /// <summary>MODELO. Tipos de recompensa que puede dar un cofre.</summary>
    public enum ItemType
    {
        /// <summary>Arma: su valor es el daño extra que suma al daño base.</summary>
        Weapon,
        /// <summary>Mejora: su valor es la vida máxima extra.</summary>
        Upgrade,
        /// <summary>Curación: su valor es la vida que recupera al instante.</summary>
        Heal
    }
}
