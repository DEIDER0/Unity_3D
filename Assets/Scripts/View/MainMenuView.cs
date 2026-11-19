using System;
using UnityEngine;
using UnityEngine.UI;

namespace Dungeon.View
{
    /// <summary>
    /// VISTA. Menú principal. Solo muestra los botones y avisa con eventos cuando se pulsan;
    /// cargar escenas le corresponde a MenuController.
    /// </summary>
    public class MainMenuView : MonoBehaviour
    {
        public event Action PlayClicked;
        public event Action QuitClicked;

        [SerializeField] private Button playButton;
        [SerializeField] private Button quitButton;

        private void OnEnable()
        {
            if (playButton != null) playButton.onClick.AddListener(OnPlay);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuit);
            if (playButton != null) playButton.Select();
        }

        private void OnDisable()
        {
            if (playButton != null) playButton.onClick.RemoveListener(OnPlay);
            if (quitButton != null) quitButton.onClick.RemoveListener(OnQuit);
        }

        private void OnPlay()
        {
            SfxPlayer.Play(Sfx.ChestOpen, 0.5f);
            PlayClicked?.Invoke();
        }

        private void OnQuit() => QuitClicked?.Invoke();
    }
}
