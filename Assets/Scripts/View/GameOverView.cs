using System;
using UnityEngine;
using UnityEngine.UI;

namespace Dungeon.View
{
    /// <summary>
    /// VISTA. Pantalla final: muestra "¡Victoria!" o "Derrota" y los botones para
    /// reintentar o volver al menú (avisa con eventos; no carga escenas).
    /// </summary>
    public class GameOverView : MonoBehaviour
    {
        public event Action RetryClicked;
        public event Action MenuClicked;

        [SerializeField] private Text titleText;
        [SerializeField] private Text messageText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button menuButton;
        [SerializeField] private Color victoryColor = new Color(1f, 0.82f, 0.25f);
        [SerializeField] private Color defeatColor = new Color(0.9f, 0.2f, 0.15f);

        public void ShowResult(bool victory, int levelReached)
        {
            if (titleText != null)
            {
                titleText.text = victory ? "¡VICTORIA!" : "DERROTA";
                titleText.color = victory ? victoryColor : defeatColor;
            }
            if (messageText != null)
            {
                messageText.text = victory
                    ? "Derrotaste a los tres jefes y escapaste de la mazmorra."
                    : $"Caíste en el nivel {levelReached}. ¡Inténtalo de nuevo!";
            }
            SfxPlayer.Play(victory ? Sfx.Victory : Sfx.Defeat);
        }

        private void OnEnable()
        {
            if (retryButton != null) retryButton.onClick.AddListener(OnRetry);
            if (menuButton != null) menuButton.onClick.AddListener(OnMenu);
            if (retryButton != null) retryButton.Select();
        }

        private void OnDisable()
        {
            if (retryButton != null) retryButton.onClick.RemoveListener(OnRetry);
            if (menuButton != null) menuButton.onClick.RemoveListener(OnMenu);
        }

        private void OnRetry() => RetryClicked?.Invoke();
        private void OnMenu() => MenuClicked?.Invoke();
    }
}
