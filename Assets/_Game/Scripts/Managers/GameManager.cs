using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement; // For scene reload
using _Game.Scripts.Bullet;
using _Game.Scripts.Cameras;

namespace _Game.Scripts.Managers
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager instance;

        [Header("Time Scale Settings")]
        public float normalTimeScale = 0.4f; 
        public float bulletTimeScale = 0.1f;

        [Header("Ramp Settings (Normal)")]
        public float normalTimeScaleStep = 0.02f;
        public float normalTimeScaleInterval = 0.1f;

        [Header("Ramp Settings (Quick)")]
        [Tooltip("Used when we do a bullet-time entry after passing through an enemy.")]
        public float quickTimeScaleStep = 0.03f;
        public float quickTimeScaleInterval = 0.02f;

        [Header("References")]
        public SimpleBulletController bulletInScene;
        public BulletAimCameraRig bulletAimCameraRig;
        
        [Header("Game Over UI")]
        public GameObject gameOverScreen; 

        [Header("Pause/UI Menu")]
        [Tooltip("Assign the same panel you want to show when Esc is pressed.")]
        public GameObject pauseMenu;

        [Header("Game Win UI")]
        [Tooltip("Panel to show when the player has beaten the final boss")]
        public GameObject gameWinScreen;

        private bool _gameStarted;
        private bool _isGameOver;
        private bool _isMenuOpen;    // Whether our pause/UI menu is currently open

        public bool IsGameOver => _isGameOver;

        private void Awake()
        {
            if (instance == null) instance = this;
            else Destroy(gameObject);

            SetTimeScale(normalTimeScale);

            if (gameOverScreen) 
                gameOverScreen.SetActive(false);

            if (gameWinScreen)
                gameWinScreen.SetActive(false);

            // Hide the pause menu if assigned
            if (pauseMenu) 
                pauseMenu.SetActive(false);
        }

        private void Start()
        {
            // If bullet is already active, we disable it at start
            if (bulletInScene && bulletInScene.gameObject.activeSelf)
                bulletInScene.gameObject.SetActive(false);

            // Default: lock/hide cursor
            // (UIController overrides this to show the mouse for TitleStart)
            LockAndHideCursor(false);
        }

        private void Update()
        {
            // Block pause if game hasn't started yet, or if game is over/win
            if (!_gameStarted || _isGameOver) return;

            // Toggle pause menu with Esc
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePauseMenu();
            }
        }

        /// <summary>
        /// Public method for the TitleStart UI button to call:
        /// Sets the _gameStarted flag and fires the first bullet.
        /// </summary>
        public void StartTheGame()
        {
            if (_gameStarted) return;
            _gameStarted = true;
            ActivateAndFireBullet();
            LockAndHideCursor(true);
        }

        /// <summary>
        /// Show/hide the pause/UI menu, and lock/unlock the cursor accordingly.
        /// Called from Update() if the user presses Esc. 
        /// Toggling again or calling ResumeGame() will revert it.
        /// </summary>
        private void TogglePauseMenu()
        {
            _isMenuOpen = !_isMenuOpen;
            if (pauseMenu)
                pauseMenu.SetActive(_isMenuOpen);

            // If menu is open => unlock cursor, else lock it
            LockAndHideCursor(!_isMenuOpen);

            Time.timeScale = _isMenuOpen ? 0 : normalTimeScale;
        }

        /// <summary>
        /// Public method for the "Continue" button to call, or for the second Esc press.
        /// Resumes normal gameplay from pause.
        /// </summary>
        public void ResumeGame()
        {
            _isMenuOpen = false;

            if (pauseMenu)
                pauseMenu.SetActive(false);

            LockAndHideCursor(true);
            Time.timeScale = normalTimeScale;
        }

        /// <summary>
        /// Lock/hide or unlock/show the hardware mouse cursor.
        /// </summary>
        private void LockAndHideCursor(bool lockIt)
        {
            if (lockIt)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        /// <summary>
        /// Called once at game start: bullet becomes active, fires, then does a normal bullet-time ramp.
        /// </summary>
        private void ActivateAndFireBullet()
        {
            if (!bulletInScene)
            {
                Debug.LogError("No bulletInScene assigned in GameManager!");
                return;
            }

            bulletInScene.gameObject.SetActive(true);
            bulletInScene.FireBullet();

            // Enter bullet time with normal ramp
            EnterBulletTimeAndWaitForClick(quickRamp: false);
        }

        /// <summary>
        /// Called by BulletCollisionProxy (or bullet itself) after hitting an enemy. 
        /// We do a "quick" ramp into bullet time.
        /// </summary>
        public void EnterBulletTimeAfterEnemyHit()
        {
            EnterBulletTimeAndWaitForClick(quickRamp: true);
        }

        /// <summary>
        /// Unified bullet-time entry method that smoothly ramps time scale down (normal or quick),
        /// sets the bullet to bullet-time mode, picks aim camera, then waits for user click to revert.
        /// </summary>
        private void EnterBulletTimeAndWaitForClick(bool quickRamp)
        {
            // Stop any existing time-scale coroutines
            StopAllCoroutines();

            // Start the downward ramp
            float step = quickRamp ? quickTimeScaleStep : normalTimeScaleStep;
            float interval = quickRamp ? quickTimeScaleInterval : normalTimeScaleInterval;
            StartCoroutine(DoBulletTimeRampDownThenWait(step, interval));
        }

        private IEnumerator DoBulletTimeRampDownThenWait(float step, float interval)
        {
            float currentScale = Time.timeScale;
            while (currentScale > bulletTimeScale)
            {
                currentScale -= step;
                SetTimeScale(Mathf.Max(currentScale, bulletTimeScale));
                yield return new WaitForSecondsRealtime(interval);
            }
            
            if (bulletInScene) bulletInScene.EnterBulletTime();
            if (bulletAimCameraRig != null) 
                bulletAimCameraRig.BulletTime.Value = 1f;
            
            Debug.Log("BulletTime active: user can aim. Click to finalize and revert.");

            // Wait until user left-click to revert
            yield return new WaitUntil(() => Input.GetMouseButtonDown(0) && !_isMenuOpen);

            RevertTimeScaleToNormal();
        }

        /// <summary>
        /// Ramps time scale back up to normal, re-fires bullet in normal speed, 
        /// reselects free camera, etc.
        /// </summary>
        private void RevertTimeScaleToNormal()
        {
            if (bulletInScene) bulletInScene.ExitBulletTime();
            
            if (bulletAimCameraRig != null)
                bulletAimCameraRig.BulletTime.Value = 0f;
            
            if (bulletInScene) bulletInScene.ReFireInCurrentAimDirection();
            
            StartCoroutine(GraduallyIncreaseTimeScale());
        }

        private IEnumerator GraduallyIncreaseTimeScale()
        {
            float currentScale = Time.timeScale;
            while (currentScale < normalTimeScale)
            {
                currentScale += normalTimeScaleStep;
                SetTimeScale(Mathf.Min(currentScale, normalTimeScale));
                yield return new WaitForSecondsRealtime(normalTimeScaleInterval);
            }
            // done
            Debug.Log("Time back to normal, free camera is active.");
        }
        
        /// <summary>
        /// Called by bullet or other code to signal game over.
        /// Mouse unlocked, user sees GameOver panel. No more pause or bullet firing.
        /// </summary>
        public void GameOver(string reason)
        {
            if (_isGameOver) return;
            _isGameOver = true;
            Debug.Log($"Game Over: {reason}");

            if (gameOverScreen)
                gameOverScreen.SetActive(true);

            LockAndHideCursor(false);
        }

        /// <summary>
        /// Similar to GameOver, but for a Win scenario. 
        /// Time is not paused, and pressing Esc does nothing because _isGameOver is true.
        /// </summary>
        public void GameWin()
        {
            if (_isGameOver) return;
            _isGameOver = true;
            Debug.Log("Game Won!");

            if (gameWinScreen)
                gameWinScreen.SetActive(true);

            LockAndHideCursor(false);
        }

        /// <summary>
        /// Public method for UI buttons to reload the current scene, effectively "Restart".
        /// </summary>
        public void RestartGame()
        {
            // Restore timescale to normal for the new scene
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>
        /// Public method for UI buttons to quit the game.
        /// </summary>
        public void QuitGame()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private void SetTimeScale(float timeScale)
        {
            Time.timeScale = timeScale;
            Time.fixedDeltaTime = 0.02f * timeScale;
        }
    }
}
