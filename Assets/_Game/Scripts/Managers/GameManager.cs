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

        [Header("Time Scale Settings")]
        public float normalTimeScale = 0.4f; 
        public float bulletTimeScale = 0.1f;

        [Header("Ramp Settings (Normal)")]
        public float normalTimeScaleStep = 0.02f;
        public float normalTimeScaleInterval = 0.1f;

        [Header("Ramp Settings (Quick)")]
        public float quickTimeScaleStep = 0.03f;
        public float quickTimeScaleInterval = 0.02f;

        [Header("References")]
        public SimpleBulletController bulletInScene;
        public BulletAimCameraRig bulletAimCameraRig;
        
        [Header("Game Over UI")]
        public GameObject gameOverScreen; 

        [Header("Pause/UI Menu")]
        public GameObject pauseMenu;

        [Header("Game Win UI")]
        public GameObject gameWinScreen;

        bool _gameStarted;
        bool _isGameOver;
        bool _isMenuOpen;

        public bool IsGameOver => _isGameOver;

        void Awake()
        {
            if (instance == null) instance = this;
            else Destroy(gameObject);
            SetTimeScale(normalTimeScale);

            if (gameOverScreen) gameOverScreen.SetActive(false);
            if (gameWinScreen) gameWinScreen.SetActive(false);
            if (pauseMenu) pauseMenu.SetActive(false);
        }

        void Start()
        {
            if (bulletInScene && bulletInScene.gameObject.activeSelf)
                bulletInScene.gameObject.SetActive(false);
            LockAndHideCursor(false);
        }

        void Update()
        {
            if (!_gameStarted || _isGameOver) return;
            if (Input.GetKeyDown(KeyCode.Escape)) TogglePauseMenu();
        }

        public void StartTheGame()
        {
            if (_gameStarted) return;
            _gameStarted = true;
            ActivateAndFireBullet();
            LockAndHideCursor(true);
        }

        void TogglePauseMenu()
        {
            _isMenuOpen = !_isMenuOpen;
            if (pauseMenu) pauseMenu.SetActive(_isMenuOpen);
            LockAndHideCursor(!_isMenuOpen);
            Time.timeScale = _isMenuOpen ? 0 : normalTimeScale;
        }

        public void ResumeGame()
        {
            _isMenuOpen = false;
            if (pauseMenu) pauseMenu.SetActive(false);
            LockAndHideCursor(true);
            Time.timeScale = normalTimeScale;
        }

        void LockAndHideCursor(bool lockIt)
        {
            Cursor.lockState = lockIt ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !lockIt;
        }

        void ActivateAndFireBullet()
        {
            if (!bulletInScene)
            {
                Debug.LogError("No bulletInScene assigned in GameManager!");
                return;
            }
            bulletInScene.gameObject.SetActive(true);
            bulletInScene.FireBullet();
            EnterBulletTimeAndWaitForClick(false);
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
            float currentScale = Time.timeScale;
            while (currentScale > bulletTimeScale)
            {
                currentScale -= step;
                SetTimeScale(Mathf.Max(currentScale, bulletTimeScale));
                yield return new WaitForSecondsRealtime(interval);
            }
            if (bulletInScene) bulletInScene.EnterBulletTime();
            if (bulletAimCameraRig) bulletAimCameraRig.BulletTime.Value = 1f;

            // Play bullet-time enter SFX
            AudioManager.instance.PlayBulletTimeEnter();

            Debug.Log("BulletTime active. Click to revert.");
            yield return new WaitUntil(() => Input.GetMouseButtonDown(0) && !_isMenuOpen);
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
            float currentScale = Time.timeScale;
            while (currentScale < normalTimeScale)
            {
                currentScale += normalTimeScaleStep;
                SetTimeScale(Mathf.Min(currentScale, normalTimeScale));
                yield return new WaitForSecondsRealtime(normalTimeScaleInterval);
            }

            // Play bullet-time exit SFX
            AudioManager.instance.PlayBulletTimeExit();

            Debug.Log("Time back to normal, free camera is active.");
        }
        
        public void GameOver(string reason)
        {
            if (_isGameOver) return;
            _isGameOver = true;
            Debug.Log($"Game Over: {reason}");
            if (gameOverScreen) gameOverScreen.SetActive(true);
            LockAndHideCursor(false);
        }

        public void GameWin()
        {
            if (_isGameOver) return;
            _isGameOver = true;
            Debug.Log("Game Won!");
            if (gameWinScreen) gameWinScreen.SetActive(true);
            LockAndHideCursor(false);
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
