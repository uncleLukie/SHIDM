using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using _Game.Scripts.Bullet;
using _Game.Scripts.Cameras;

namespace _Game.Scripts.Managers
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager instance;

        public float normalTimeScale = 0.4f; 
        public float bulletTimeScale = 0.1f;
        public float normalTimeScaleStep = 0.02f;
        public float normalTimeScaleInterval = 0.1f;
        public float quickTimeScaleStep = 0.03f;
        public float quickTimeScaleInterval = 0.02f;

        public SimpleBulletController bulletInScene;
        public BulletAimCameraRig bulletAimCameraRig;
        
        public GameObject gameOverScreen; 
        public GameObject pauseMenu;
        public GameObject gameWinScreen;

        bool gameStarted;
        bool isGameOver;
        bool isMenuOpen;

        bool wasInBulletTime;
        float prePauseTimeScale;

        public bool IsGameOver => isGameOver;

        void Awake()
        {
            if (instance == null) instance = this;
            else Destroy(gameObject);

            SetTimeScale(normalTimeScale);

            if (gameOverScreen) gameOverScreen.SetActive(false);
            if (gameWinScreen)  gameWinScreen.SetActive(false);
            if (pauseMenu)      pauseMenu.SetActive(false);
        }

        void Start()
        {
            // Deactivate bullet or set it inactive
            if (bulletInScene && bulletInScene.gameObject.activeSelf)
                bulletInScene.gameObject.SetActive(false);

            // Title screen => show mouse
            LockAndHideCursor(false);
        }

        void Update()
        {
            if (!gameStarted || isGameOver) return;

            // Pause menu
            if (Input.GetKeyDown(KeyCode.Escape)) TogglePauseMenu();

            // Wait for user to press Mouse1 to actually fire bullet
            if (!isMenuOpen && bulletInScene && !bulletInScene.IsFired && Input.GetMouseButtonDown(0))
            {
                // We only proceed if bullet is not fired yet
                bulletInScene.gameObject.SetActive(true);
                bulletInScene.FireBullet();
                EnterBulletTimeAndWaitForClick(false);
            }
        }

        public void StartTheGame()
        {
            if (gameStarted) return;
            gameStarted = true;

            // We'll just enable the bullet object but NOT fire. 
            // The bullet will remain floating until the user presses mouse 1 to fire it.
            if (bulletInScene && !bulletInScene.gameObject.activeSelf)
                bulletInScene.gameObject.SetActive(true);

            LockAndHideCursor(true);
        }

        void TogglePauseMenu()
        {
            isMenuOpen = !isMenuOpen;
            if (pauseMenu) pauseMenu.SetActive(isMenuOpen);

            if (isMenuOpen)
            {
                // Save previous time scale
                prePauseTimeScale = Time.timeScale;

                // Check if we were in bullet time
                wasInBulletTime = (bulletAimCameraRig && bulletAimCameraRig.BulletTime.Value > 0.5f);

                // Force free camera
                if (bulletAimCameraRig) bulletAimCameraRig.BulletTime.Value = 0f;
                if (bulletInScene) bulletInScene.ExitBulletTime();

                // Pause
                Time.timeScale = 0f;
                LockAndHideCursor(false);
            }
            else
            {
                // Unpause
                if (bulletAimCameraRig && wasInBulletTime)
                {
                    bulletAimCameraRig.BulletTime.Value = 1f;
                    if (bulletInScene) bulletInScene.EnterBulletTime();
                }
                else
                {
                    if (bulletAimCameraRig) bulletAimCameraRig.BulletTime.Value = 0f;
                    if (bulletInScene) bulletInScene.ExitBulletTime();
                }

                Time.timeScale = prePauseTimeScale > 0 ? prePauseTimeScale : normalTimeScale;
                LockAndHideCursor(true);
            }
        }

        public void ResumeGame()
        {
            if (!isMenuOpen) return;
            isMenuOpen = false;
            if (pauseMenu) pauseMenu.SetActive(false);

            if (bulletAimCameraRig && wasInBulletTime)
            {
                bulletAimCameraRig.BulletTime.Value = 1f;
                if (bulletInScene) bulletInScene.EnterBulletTime();
            }
            else
            {
                if (bulletAimCameraRig) bulletAimCameraRig.BulletTime.Value = 0f;
                if (bulletInScene) bulletInScene.ExitBulletTime();
            }

            Time.timeScale = prePauseTimeScale > 0 ? prePauseTimeScale : normalTimeScale;
            LockAndHideCursor(true);
        }

        public void LockAndHideCursor(bool lockIt)
        {
            Cursor.lockState = lockIt ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !lockIt;
        }

        public void EnterBulletTimeAfterEnemyHit()
        {
            EnterBulletTimeAndWaitForClick(true);
        }

        void EnterBulletTimeAndWaitForClick(bool quickRamp)
        {
            StopAllCoroutines();
            float step = quickRamp ? quickTimeScaleStep : normalTimeScaleStep;
            float interval = quickRamp ? quickTimeScaleInterval : normalTimeScaleInterval;
            StartCoroutine(DoBulletTimeRampDownThenWait(step, interval));
        }

        IEnumerator DoBulletTimeRampDownThenWait(float step, float interval)
        {
            AudioManager.instance.PlayBulletTimeEnter();
            float currentScale = Time.timeScale;
            while (currentScale > bulletTimeScale)
            {
                currentScale -= step;
                SetTimeScale(Mathf.Max(currentScale, bulletTimeScale));
                yield return new WaitForSecondsRealtime(interval);
            }
            if (bulletInScene) bulletInScene.EnterBulletTime();
            if (bulletAimCameraRig) bulletAimCameraRig.BulletTime.Value = 1f;

            yield return new WaitUntil(() => Input.GetMouseButtonDown(0) && !isMenuOpen);
            RevertTimeScaleToNormal();
        }

        void RevertTimeScaleToNormal()
        {
            if (bulletInScene) bulletInScene.ExitBulletTime();
            if (bulletAimCameraRig) bulletAimCameraRig.BulletTime.Value = 0f;
            if (bulletInScene) bulletInScene.ReFireInCurrentAimDirection();
            StartCoroutine(GraduallyIncreaseTimeScale());
        }

        IEnumerator GraduallyIncreaseTimeScale()
        {
            AudioManager.instance.PlayBulletTimeExit();
            float currentScale = Time.timeScale;
            while (currentScale < normalTimeScale)
            {
                currentScale += normalTimeScaleStep;
                SetTimeScale(Mathf.Min(currentScale, normalTimeScale));
                yield return new WaitForSecondsRealtime(normalTimeScaleInterval);
            }
        }

        public void GameOver(string reason)
        {
            if (isGameOver) return;
            isGameOver = true;
            Debug.Log($"Game Over: {reason}");

            if (bulletAimCameraRig) bulletAimCameraRig.BulletTime.Value = 0f;
            if (bulletInScene)
            {
                bulletInScene.ExitBulletTime();
                bulletInScene.ForceEndNow();
            }

            if (gameOverScreen) gameOverScreen.SetActive(true);
            LockAndHideCursor(false);
            AudioManager.instance.PlayGameOverMusic();
        }

        public void GameWin()
        {
            if (isGameOver) return;
            isGameOver = true;
            Debug.Log("Game Won!");

            if (bulletAimCameraRig) bulletAimCameraRig.BulletTime.Value = 0f;
            if (bulletInScene)
            {
                bulletInScene.ExitBulletTime();
                bulletInScene.ForceEndNow();
            }

            if (gameWinScreen) gameWinScreen.SetActive(true);
            LockAndHideCursor(false);
            AudioManager.instance.PlayGameWinMusic();
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void QuitGame()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        void SetTimeScale(float timeScale)
        {
            Time.timeScale = timeScale;
            Time.fixedDeltaTime = 0.02f * timeScale;
        }
    }
}
