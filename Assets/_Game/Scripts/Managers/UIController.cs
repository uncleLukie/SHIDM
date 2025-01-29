using UnityEngine;
using System.Collections; 
using _Game.Scripts.Managers;

namespace _Game.Scripts.Managers
{
    public class UIController : MonoBehaviour
    {
        public static UIController instance;

        [Header("Panels")]
        public GameObject titleStartPanel;      // TitleStart
        public GameObject playPausePanel;       // PlayPause
        public GameObject playGameOverPanel;    // PlayGameOver
        public GameObject playGameWinPanel;     // PlayGameWin

        [Header("Optional Fade Settings (TitleStart)")]
        [Tooltip("If you want the TitleStart panel to fade out, attach a CanvasGroup to that panel.")]
        public CanvasGroup titleStartCanvasGroup;
        public float fadeDuration = 0.5f;

        private void Awake()
        {
            // Basic singleton pattern (optional)
            if (instance == null) instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            // Ensure correct initial visibility:
            if (titleStartPanel)      titleStartPanel.SetActive(true);
            if (playPausePanel)       playPausePanel.SetActive(false);
            if (playGameOverPanel)    playGameOverPanel.SetActive(false);
            if (playGameWinPanel)     playGameWinPanel.SetActive(false);

            // We want the mouse to be visible on the TitleStart screen:
            GameManager.instance?.gameObject.SendMessage("LockAndHideCursor", false, SendMessageOptions.DontRequireReceiver);
        }

        // ---------------------------------------------------------------------
        //  TitleStart Panel Buttons
        // ---------------------------------------------------------------------

        /// <summary>
        /// Invoked by the "Click to Start" button on the TitleStart panel.
        /// Fades out the panel (if desired) then tells GameManager to start the game.
        /// </summary>
        public void OnClickTitleStart()
        {
            if (titleStartCanvasGroup)
            {
                // If we have a CanvasGroup, run a fade-out
                StartCoroutine(FadeOutTitleStartPanel());
            }
            else
            {
                // No fade: simply hide it and start game
                if (titleStartPanel) titleStartPanel.SetActive(false);
                GameManager.instance.StartTheGame();
            }
        }

        private IEnumerator FadeOutTitleStartPanel()
        {
            float elapsed = 0f;
            float startAlpha = titleStartCanvasGroup.alpha;
            float endAlpha = 0f;

            // Disable interaction so user doesn't spam-click
            titleStartCanvasGroup.interactable = false;
            titleStartCanvasGroup.blocksRaycasts = false;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                titleStartCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
                yield return null;
            }

            // After fade, hide it completely
            if (titleStartPanel) titleStartPanel.SetActive(false);

            // Now start the actual game
            GameManager.instance.StartTheGame();
        }

        /// <summary>
        /// Invoked by a "Quit Game" button on the TitleStart panel.
        /// </summary>
        public void OnClickTitleQuit()
        {
            GameManager.instance.QuitGame();
        }

        // ---------------------------------------------------------------------
        //  PlayPause Panel Buttons
        // ---------------------------------------------------------------------

        /// <summary>
        /// "Continue" button on the PlayPause panel.
        /// Resumes normal game from paused state.
        /// </summary>
        public void OnClickPauseContinue()
        {
            GameManager.instance.ResumeGame();
        }

        /// <summary>
        /// "Restart" button on the PlayPause panel.
        /// Reloads the current scene.
        /// </summary>
        public void OnClickPauseRestart()
        {
            GameManager.instance.RestartGame();
        }

        /// <summary>
        /// "Give Up" button on the PlayPause panel.
        /// Quits the application.
        /// </summary>
        public void OnClickPauseGiveUp()
        {
            GameManager.instance.QuitGame();
        }

        // ---------------------------------------------------------------------
        //  PlayGameOver Panel Buttons
        // ---------------------------------------------------------------------

        /// <summary>
        /// "Restart" button on the GameOver panel.
        /// Reloads the current scene.
        /// </summary>
        public void OnClickGameOverRestart()
        {
            GameManager.instance.RestartGame();
        }

        /// <summary>
        /// "Give Up" button on the GameOver panel.
        /// Quits the application.
        /// </summary>
        public void OnClickGameOverGiveUp()
        {
            GameManager.instance.QuitGame();
        }

        // ---------------------------------------------------------------------
        //  PlayGameWin Panel Buttons
        // ---------------------------------------------------------------------

        /// <summary>
        /// "Restart" button on the GameWin panel.
        /// </summary>
        public void OnClickGameWinRestart()
        {
            GameManager.instance.RestartGame();
        }

        /// <summary>
        /// "Give Up" button on the GameWin panel.
        /// </summary>
        public void OnClickGameWinGiveUp()
        {
            GameManager.instance.QuitGame();
        }
    }
}
