using UnityEngine;
using System.Collections;
using _Game.Scripts.Managers;

namespace _Game.Scripts.Managers
{
    public class UIController : MonoBehaviour
    {
        public static UIController instance;

        [Header("Panels")]
        public GameObject titleStartPanel;
        public GameObject playPausePanel;
        public GameObject playGameOverPanel;
        public GameObject playGameWinPanel;

        [Header("Optional Fade Settings (TitleStart)")]
        public CanvasGroup titleStartCanvasGroup;
        public float fadeDuration = 0.5f;

        void Awake()
        {
            if (instance == null) instance = this;
            else Destroy(gameObject);
        }

        void Start()
        {
            if (titleStartPanel)      titleStartPanel.SetActive(true);
            if (playPausePanel)       playPausePanel.SetActive(false);
            if (playGameOverPanel)    playGameOverPanel.SetActive(false);
            if (playGameWinPanel)     playGameWinPanel.SetActive(false);

            // Let the user see the mouse. 
            GameManager.instance?.LockAndHideCursor(false);

            // Title music
            AudioManager.instance.PlayTitleMusic();
        }

        public void OnClickTitleStart()
        {
            AudioManager.instance.PlayUIClick();

            // Request pointer lock in WebGL, if possible
            GameManager.instance?.LockAndHideCursor(true);

            if (titleStartCanvasGroup)
            {
                StartCoroutine(FadeOutTitleStartPanel());
            }
            else
            {
                if (titleStartPanel) titleStartPanel.SetActive(false);
                HandleActualGameStart();
            }
        }

        IEnumerator FadeOutTitleStartPanel()
        {
            // Start-Game One-Shot right away
            AudioManager.instance.PlayStartGameOneShot();

            float elapsed = 0f;
            float startAlpha = titleStartCanvasGroup.alpha;
            float endAlpha = 0f;

            titleStartCanvasGroup.interactable = false;
            titleStartCanvasGroup.blocksRaycasts = false;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                titleStartCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
                yield return null;
            }

            if (titleStartPanel) titleStartPanel.SetActive(false);
            HandleActualGameStart();
        }

        void HandleActualGameStart()
        {
            // Switch from title to in-game music
            AudioManager.instance.PlayInGameMusic();

            // Actually start the game logic
            GameManager.instance.StartTheGame();
        }

        public void OnClickTitleQuit()
        {
            AudioManager.instance.PlayUIClick();
            GameManager.instance.QuitGame();
        }

        public void OnClickPauseContinue()
        {
            AudioManager.instance.PlayUIClick();
            GameManager.instance.ResumeGame();
        }

        public void OnClickPauseRestart()
        {
            AudioManager.instance.PlayUIClick();
            GameManager.instance.RestartGame();
        }

        public void OnClickPauseGiveUp()
        {
            AudioManager.instance.PlayUIClick();
            GameManager.instance.QuitGame();
        }

        public void OnClickGameOverRestart()
        {
            AudioManager.instance.PlayUIClick();
            GameManager.instance.RestartGame();
        }

        public void OnClickGameOverGiveUp()
        {
            AudioManager.instance.PlayUIClick();
            GameManager.instance.QuitGame();
        }

        public void OnClickGameWinRestart()
        {
            AudioManager.instance.PlayUIClick();
            GameManager.instance.RestartGame();
        }

        public void OnClickGameWinGiveUp()
        {
            AudioManager.instance.PlayUIClick();
            GameManager.instance.QuitGame();
        }
    }
}
