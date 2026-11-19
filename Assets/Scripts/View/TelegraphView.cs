using System.Collections;
using UnityEngine;

namespace Dungeon.View
{
    /// <summary>
    /// VISTA. Círculo rojo en el piso que anuncia el golpe de área del jefe.
    /// Crece durante la preparación para que el jugador sepa cuándo esquivar o alejarse.
    /// </summary>
    public class TelegraphView : MonoBehaviour
    {
        [SerializeField] private Transform indicator;

        private Coroutine _routine;

        private void Awake()
        {
            if (indicator != null) indicator.gameObject.SetActive(false);
        }

        public void Show(float radius, float duration)
        {
            if (indicator == null) return;
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(GrowRoutine(radius, duration));
        }

        /// <param name="impact">true si el golpe se ejecutó (sonido de impacto).</param>
        public void Hide(bool impact)
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = null;
            if (indicator != null) indicator.gameObject.SetActive(false);
            if (impact) SfxPlayer.Play(Sfx.Slam);
        }

        private IEnumerator GrowRoutine(float radius, float duration)
        {
            indicator.gameObject.SetActive(true);
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);
                float pulse = 1f + Mathf.Sin(t * 25f) * 0.03f;
                float diameter = radius * 2f * Mathf.Lerp(0.3f, 1f, k) * pulse;
                indicator.localScale = new Vector3(diameter, 0.02f, diameter);
                yield return null;
            }
            indicator.localScale = new Vector3(radius * 2f, 0.02f, radius * 2f);
        }
    }
}
