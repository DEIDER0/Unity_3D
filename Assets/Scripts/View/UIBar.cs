using UnityEngine;

namespace Dungeon.View
{
    /// <summary>
    /// VISTA (utilidad). Barra de relleno hecha con anclas: no necesita sprites.
    /// El relleno es un RectTransform hijo que se estira de 0 a 'value' en X.
    /// </summary>
    public static class UIBar
    {
        public static void SetFill(RectTransform fill, float value)
        {
            if (fill == null) return;
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
        }
    }
}
