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

        private bool _gameStarted;
        private bool _isGameOver;
        
        public bool IsGameOver => _isGameOver;

        private void Awake()
        {
            if (instance == null) instance = this;
            else Destroy(gameObject);

            SetTimeScale(normalTimeScale);

            if (gameOverScreen) 
                gameOverScreen.SetActive(false);
        }

        private void Start()
        {
            if (bulletInScene && bulletInScene.gameObject.activeSelf)
                bulletInScene.gameObject.SetActive(false);
        }

        private void Update()
        {
            // if we want to block inputs after game over
            if (_isGameOver) return;

            // same logic for bullet activation
            if (!_gameStarted && Input.GetMouseButtonDown(0))
            {
                _gameStarted = true;
                ActivateAndFireBullet();
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
        /// Called by BulletCollisionProxy after hitting an enemy. 
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
            // Stop any existing time-scale coroutines (optional)
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
            yield return new WaitUntil(() => Input.GetMouseButtonDown(0));
            
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
        }

        private void SetTimeScale(float timeScale)
        {
            Time.timeScale = timeScale;
            Time.fixedDeltaTime = 0.02f * timeScale;
        }
    }
}
