using Dungeon.Model;
using UnityEngine;

namespace Dungeon.View
{
    /// <summary>
    /// VISTA. Barra de vida pequeña sobre la cabeza de un enemigo (Canvas en el mundo).
    /// Se suscribe a EnemyState.HealthChanged y Died; solo se muestra cuando el enemigo está herido.
    /// </summary>
    public class EnemyHealthBarView : MonoBehaviour
    {
        [SerializeField] private GameObject barRoot;
        [SerializeField] private RectTransform fill;

        private EnemyState _state;
        private bool _subscribed;
        private Camera _camera;

        public void Bind(EnemyState state)
        {
            Unsubscribe();
            _state = state;
            if (isActiveAndEnabled) Subscribe();
            Refresh();
        }

        private void OnEnable() => Subscribe();
        private void OnDisable() => Unsubscribe();

        private void LateUpdate()
        {
            if (_camera == null) _camera = Camera.main;
            if (_camera != null) transform.rotation = _camera.transform.rotation; // siempre mirando a la cámara
        }

        private void Subscribe()
        {
            if (_state == null || _subscribed) return;
            _state.HealthChanged += OnHealthChanged;
            _state.Died += OnDied;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (_state == null || !_subscribed) return;
            _state.HealthChanged -= OnHealthChanged;
            _state.Died -= OnDied;
            _subscribed = false;
        }

        private void Refresh()
        {
            if (_state == null) { SetVisible(false); return; }
            OnHealthChanged(_state.CurrentHealth, _state.MaxHealth);
        }

        private void OnHealthChanged(int current, int max)
        {
            UIBar.SetFill(fill, (float)current / max);
            SetVisible(current > 0 && current < max);
        }

        private void OnDied() => SetVisible(false);

        private void SetVisible(bool visible)
        {
            if (barRoot != null) barRoot.SetActive(visible);
        }
    }
}
