using UnityEngine;
using System.Collections;
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
        [Tooltip("Assign a UI panel or menu canvas here that you want to show when Esc is pressed.")]
        public GameObject pauseMenu;

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

            // Hide the pause menu if assigned
            if (pauseMenu) 
                pauseMenu.SetActive(false);
        }

        private void Start()
        {
            // If bullet is already active, we disable it at start
            if (bulletInScene && bulletInScene.gameObject.activeSelf)
                bulletInScene.gameObject.SetActive(false);

            // Lock/hide mouse by default
            LockAndHideCursor(true);
        }

        private void Update()
        {
            // If game is over, skip further input checks
            if (_isGameOver) return;

            // 1) Toggle pause menu with Esc
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePauseMenu();
            }

            // 2) If not started, left-click => first bullet spawn
            if (!_gameStarted && Input.GetMouseButtonDown(0) && !_isMenuOpen)
            {
                _gameStarted = true;
                ActivateAndFireBullet();
            }
        }

        /// <summary>
        /// Show/hide the pause/UI menu, and lock/unlock the cursor accordingly.
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
        /// We do a "quick" ramp into bullet time, but otherwise the same flow.
        /// </summary>
        public void EnterBulletTimeAfterEnemyHit()
        {
            EnterBulletTimeAndWaitForClick(quickRamp: true);
        }

        /// <summary>
        /// Unified bullet-time entry method that smoothly ramps time scale down (normal or quick),
        /// sets the bullet to bullet-time mode, picks aim camera, then waits for user click to revert.
        /// </summary>
        public void EnterBulletTimeAndWaitForClick(bool quickRamp)
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
        
        public void GameOver(string reason)
        {
            if (_isGameOver) return;
            _isGameOver = true;
            Debug.Log($"Game Over: {reason}");

            // Show UI
            if (gameOverScreen)
                gameOverScreen.SetActive(true);

            // Also unlock cursor so player can click UI
            LockAndHideCursor(false);
        }

        private void SetTimeScale(float timeScale)
        {
            Time.timeScale = timeScale;
            Time.fixedDeltaTime = 0.02f * timeScale;
        }
    }
}
