using System.Collections;
using Dungeon.Model;
using UnityEngine;
using UnityEngine.UI;

namespace Dungeon.View
{
    /// <summary>
    /// VISTA. Barra de vida grande del jefe, en la parte superior de la pantalla.
    /// Aparece cuando el jugador entra a la sala del jefe y desaparece cuando el jefe muere.
    /// </summary>
    public class BossHealthBarView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Text nameText;
        [SerializeField] private RectTransform fill;
        [SerializeField] private Image fillImage;
        [SerializeField] private Color normalColor = new Color(0.8f, 0.15f, 0.15f);
        [SerializeField] private Color enragedColor = new Color(1f, 0.5f, 0.05f);

        private BossState _boss;
        private bool _subscribed;

        public void Bind(BossState boss)
        {
            Unsubscribe();
            _boss = boss;
            if (isActiveAndEnabled) Subscribe();
            if (_boss != null)
            {
                if (nameText != null) nameText.text = _boss.Name;
                OnHealthChanged(_boss.CurrentHealth, _boss.MaxHealth);
                if (fillImage != null) fillImage.color = _boss.IsEnraged ? enragedColor : normalColor;
            }
        }

        public void Show()
        {
            if (panel != null && _boss != null && !_boss.IsDead) panel.SetActive(true);
            SfxPlayer.Play(Sfx.BossRoar);
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }

        private void Awake() => Hide();
        private void OnEnable() => Subscribe();
        private void OnDisable() => Unsubscribe();

        private void Subscribe()
        {
            if (_boss == null || _subscribed) return;
            _boss.HealthChanged += OnHealthChanged;
            _boss.Enraged += OnEnraged;
            _boss.Died += OnDied;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (_boss == null || !_subscribed) return;
            _boss.HealthChanged -= OnHealthChanged;
            _boss.Enraged -= OnEnraged;
            _boss.Died -= OnDied;
            _subscribed = false;
        }

        private void OnHealthChanged(int current, int max) => UIBar.SetFill(fill, (float)current / max);

        private void OnEnraged()
        {
            if (fillImage != null) fillImage.color = enragedColor;
            if (nameText != null) nameText.text = _boss.Name + " (¡FURIOSO!)";
            SfxPlayer.Play(Sfx.BossRoar);
        }

        private void OnDied()
        {
            if (isActiveAndEnabled) StartCoroutine(HideLater());
        }

        private IEnumerator HideLater()
        {
            yield return new WaitForSeconds(1.5f);
            Hide();
        }
    }
}
