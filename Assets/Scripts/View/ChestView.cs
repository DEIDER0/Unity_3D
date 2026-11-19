using System.Collections;
using Dungeon.Model;
using UnityEngine;

namespace Dungeon.View
{
    /// <summary>
    /// VISTA. Muestra el cofre: anima la tapa al abrirse, enciende un brillo dorado
    /// y muestra el aviso "[E] Abrir" cuando el controlador se lo indica.
    /// Se suscribe a ChestModel.Opened.
    /// </summary>
    public class ChestView : MonoBehaviour
    {
        [SerializeField] private Transform lid;
        [Tooltip("Ángulo de apertura sobre el eje X (negativo = la tapa sube hacia atrás).")]
        [SerializeField] private float openAngle = -110f;
        [SerializeField] private float openDuration = 0.6f;
        [SerializeField] private GameObject prompt;
        [SerializeField] private Light glow;

        private ChestModel _model;
        private bool _subscribed;
        private Quaternion _lidRest;
        private Camera _camera;

        private void Awake()
        {
            if (lid != null) _lidRest = lid.localRotation;
            SetPromptVisible(false);
            if (glow != null) glow.enabled = false;
        }

        public void Bind(ChestModel model)
        {
            Unsubscribe();
            _model = model;
            if (isActiveAndEnabled) Subscribe();
            if (_model != null && _model.IsOpened) SetLidAngle(openAngle);
        }

        public void SetPromptVisible(bool visible)
        {
            if (prompt != null) prompt.SetActive(visible && (_model == null || !_model.IsOpened));
        }

        private void OnEnable() => Subscribe();
        private void OnDisable() => Unsubscribe();

        private void LateUpdate()
        {
            if (prompt == null || !prompt.activeSelf) return;
            if (_camera == null) _camera = Camera.main;
            if (_camera != null) prompt.transform.rotation = _camera.transform.rotation;
        }

        private void Subscribe()
        {
            if (_model == null || _subscribed) return;
            _model.Opened += OnOpened;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (_model == null || !_subscribed) return;
            _model.Opened -= OnOpened;
            _subscribed = false;
        }

        private void OnOpened(Item item)
        {
            SetPromptVisible(false);
            StartCoroutine(OpenRoutine());
            SfxPlayer.Play(Sfx.ChestOpen);
        }

        private IEnumerator OpenRoutine()
        {
            if (glow != null) glow.enabled = true;
            float t = 0f;
            while (t < openDuration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / openDuration);
                // pequeño rebote al final
                float eased = 1f - Mathf.Pow(1f - k, 3f) + Mathf.Sin(k * Mathf.PI) * 0.08f;
                SetLidAngle(openAngle * eased);
                yield return null;
            }
            SetLidAngle(openAngle);

            if (glow == null) yield break;
            float start = glow.intensity;
            t = 0f;
            while (t < 2f)
            {
                t += Time.deltaTime;
                glow.intensity = Mathf.Lerp(start, start * 0.25f, t / 2f);
                yield return null;
            }
        }

        private void SetLidAngle(float angle)
        {
            if (lid != null) lid.localRotation = Quaternion.AngleAxis(angle, Vector3.right) * _lidRest;
        }
    }
}
