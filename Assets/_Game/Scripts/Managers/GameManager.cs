using UnityEngine;
using System.Collections;
using _Game.Scripts.Bullet;

namespace _Game.Scripts.Managers
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager instance;

        [Header("Time Scale Settings")]
        public float normalTimeScale = 1f;
        public float bulletTimeScale = 0.1f;
        public float timeScaleStep = 0.05f;
        public float timeScaleStepInterval = 0.1f;

        [Header("Bullet Time Settings")]
        [Tooltip("Delay before we enter bullet time after the bullet is fired.")]
        public float bulletTimeDelay = 0.3f;

        [Header("Scene Bullet Reference")]
        [Tooltip("Reference to the bullet in the scene (disabled at start) with X=90 local rotation so tip is up.")]
        public SimpleBulletController bulletInScene;
        
        private bool _gameStarted;
        private bool _bulletSpawned;

        private void Awake()
        {
            if (instance == null) instance = this;
            else Destroy(gameObject);

            SetTimeScale(normalTimeScale);
        }

        private void Start()
        {
            // If bullet is active in scene, disable it
            if (bulletInScene && bulletInScene.gameObject.activeSelf)
            {
                bulletInScene.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            // example: user left-click => activate bullet
            if (!_gameStarted && Input.GetMouseButtonDown(0))
            {
                _gameStarted = true;
                ActivateAndFireBullet();
            }
        }

        /// <summary>
        /// Enable bulletInScene, call FireBullet(), schedule bullet time after bulletTimeDelay
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
            _bulletSpawned = true;

            // bullet time after delay
            if (bulletTimeDelay > 0f)
                Invoke(nameof(StartGradualBulletTime), bulletTimeDelay);
            else
                StartGradualBulletTime();
        }

        public void StartGradualBulletTime()
        {
            if (!_bulletSpawned) return;
            StartCoroutine(GraduallyDecreaseTimeScale());
        }

        private IEnumerator GraduallyDecreaseTimeScale()
        {
            float currentScale = Time.timeScale;
            while (currentScale > bulletTimeScale)
            {
                currentScale -= timeScaleStep;
                SetTimeScale(Mathf.Max(currentScale, bulletTimeScale));
                yield return new WaitForSecondsRealtime(timeScaleStepInterval);
            }

            // Let bullet steer
            if (bulletInScene)
                bulletInScene.EnterBulletTime();
        }

        public IEnumerator GraduallyIncreaseTimeScale()
        {
            float currentScale = Time.timeScale;
            while (currentScale < normalTimeScale)
            {
                currentScale += timeScaleStep;
                SetTimeScale(Mathf.Min(currentScale, normalTimeScale));
                yield return new WaitForSecondsRealtime(timeScaleStepInterval);
            }
        }

        private void SetTimeScale(float timeScale)
        {
            Time.timeScale = timeScale;
            Time.fixedDeltaTime = 0.02f * timeScale;
        }
    }
}
