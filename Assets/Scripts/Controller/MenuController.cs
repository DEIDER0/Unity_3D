using Dungeon.Data;
using Dungeon.View;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dungeon.Controller
{
    /// <summary>
    /// CONTROLADOR. Conecta los botones de las vistas de menú (MainMenuView o GameOverView)
    /// con las acciones: empezar una partida nueva, volver al menú o salir.
    /// </summary>
    public class MenuController : MonoBehaviour
    {
        [SerializeField] private MainMenuView mainMenu;
        [SerializeField] private GameOverView gameOver;
        [SerializeField] private PlayerConfig playerConfig;

        private void OnEnable()
        {
            if (mainMenu != null)
            {
                mainMenu.PlayClicked += StartGame;
                mainMenu.QuitClicked += Quit;
            }
            if (gameOver != null)
            {
                gameOver.RetryClicked += StartGame;
                gameOver.MenuClicked += GoToMenu;
            }
        }

        private void OnDisable()
        {
            if (mainMenu != null)
            {
                mainMenu.PlayClicked -= StartGame;
                mainMenu.QuitClicked -= Quit;
            }
            if (gameOver != null)
            {
                gameOver.RetryClicked -= StartGame;
                gameOver.MenuClicked -= GoToMenu;
            }
        }

        private void Start()
        {
            if (gameOver != null) gameOver.ShowResult(GameController.LastGameWasVictory, GameController.LastLevelReached);
        }

        private void StartGame()
        {
            GameController.StartNewGame(playerConfig);
            SceneManager.LoadScene(SceneNames.Level(1));
        }

        private void GoToMenu() => SceneManager.LoadScene(SceneNames.MainMenu);

        private void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
