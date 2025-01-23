using UnityEngine;
using System.Collections;
using _Game.Scripts.Bullet;

namespace _Game.Scripts.Managers
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager instance;

        [Header("Time Scale Settings")]
        public float normalTimeScale = 1f;   // Full speed
        public float bulletTimeScale = 0.1f; // Slow speed
        public float timeScaleStep = 0.05f;  // How much we add or subtract per step
        public float timeScaleStepInterval = 0.1f; // Delay between steps

        [Header("Bullet Settings")]
        [Tooltip("The bullet prefab to spawn.")]
        public GameObject bulletPrefab;
        [Tooltip("Where the bullet is spawned from (transform).")]
        public Transform bulletSpawnPoint;
        [Tooltip("How long after bullet is fired do we start the slow motion?")]
        public float bulletTimeDelay = 0.3f;

        private BulletController _bulletController;
        private bool _bulletSpawned;

        private void Awake()
        {
            if (instance == null) instance = this;
            else Destroy(gameObject);

            // Default to normal timescale
            SetTimeScale(normalTimeScale);
        }

        private void Start()
        {
            // Immediately spawn & fire bullet
            SpawnAndFireBullet();
        }

        private void SpawnAndFireBullet()
        {
            if (!bulletPrefab || !bulletSpawnPoint)
            {
                Debug.LogError("Bullet Prefab or Spawn Point missing!");
                return;
            }

            // Instantiate bullet
            GameObject bulletObj = Instantiate(bulletPrefab, bulletSpawnPoint.position, Quaternion.Euler(0f, 0f, 90f));
            _bulletController = bulletObj.GetComponent<BulletController>();

            if (_bulletController == null)
            {
                Debug.LogError("No BulletController on bullet prefab!");
                return;
            }

            // Fire the bullet at normal time
            _bulletController.FireBullet();
            _bulletSpawned = true;

            // Start slowdown after a short delay (0.3s default)
            if (bulletTimeDelay > 0f)
                Invoke(nameof(StartGradualBulletTime), bulletTimeDelay);
            else
                StartGradualBulletTime();
        }

        // Public so bullet can call it again after collisions
        public void StartGradualBulletTime()
        {
            if (!_bulletSpawned) return;
            StartCoroutine(GraduallyDecreaseTimeScale());
        }

        // Gradually slow from normalTimeScale => bulletTimeScale
        public IEnumerator GraduallyDecreaseTimeScale()
        {
            float currentScale = Time.timeScale;
            while (currentScale > bulletTimeScale)
            {
                currentScale -= timeScaleStep;
                SetTimeScale(Mathf.Max(currentScale, bulletTimeScale));
                yield return new WaitForSecondsRealtime(timeScaleStepInterval);
            }

            // Once at bullet time, let bullet steer
            if (_bulletController != null)
                _bulletController.EnterBulletTime();
        }

        // Gradually speed from bulletTimeScale => normalTimeScale
        public IEnumerator GraduallyIncreaseTimeScale()
        {
            float currentScale = Time.timeScale;
            while (currentScale < normalTimeScale)
            {
                currentScale += timeScaleStep;
                SetTimeScale(Mathf.Min(currentScale, normalTimeScale));
                yield return new WaitForSecondsRealtime(timeScaleStepInterval);
            }
            // Now fully back to normal time
        }

        private void SetTimeScale(float timeScale)
        {
            Time.timeScale = timeScale;
            Time.fixedDeltaTime = 0.02f * timeScale;
        }
    }
}
