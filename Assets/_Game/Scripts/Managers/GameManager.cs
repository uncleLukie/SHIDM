using UnityEngine;

namespace _Game.Scripts.Managers
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager instance;

        private int _score;
        private float _distanceTraveled;
        private Vector3 _startPosition;
        private Transform _bulletTransform;

        [SerializeField] private float normalTimeScale = 0.6f;
        [SerializeField] private float bulletTimeScale = 0.1f;

        [Header("Bullet Settings")]
        [Tooltip("Bullet prefab to instantiate when the game starts.")]
        public GameObject bulletPrefab;

        [Tooltip("Bullet fire effect to instantiate.")]
        public GameObject bulletFireFXPrefab;

        [Tooltip("Spawn point for the bullet.")]
        public Transform bulletSpawnPoint;

        private bool _gameStarted;
        private bool _isBulletTimeActive;

        private void Awake()
        {
            if (instance == null) instance = this;
            else Destroy(gameObject);

            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            SetTimeScale(normalTimeScale);
        }

        private void Update()
        {
            // Wait for player input to start the game
            if (!_gameStarted && (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)))
            {
                StartGame();
            }

            // Toggle bullet time when pressing Mouse 1
            if (_gameStarted && Input.GetMouseButtonDown(0))
            {
                if (_isBulletTimeActive)
                {
                    DisableBulletTime();
                }
                else
                {
                    EnableBulletTime();
                }
            }

            // Track distance traveled by the bullet
            if (_bulletTransform != null)
            {
                _distanceTraveled = Vector3.Distance(_startPosition, _bulletTransform.position);
            }
        }

        public void IncrementScore(int amount = 1)
        {
            _score += amount;
        }

        public void OnBossHit()
        {
            Debug.Log("Boss Hit! You win!");
            // e.g., SceneManager.LoadScene("WinScene");
        }

        public void OnBulletDestroyed()
        {
            Debug.Log("Bullet destroyed. Game Over.");
            // e.g., SceneManager.LoadScene("GameOverScene");
        }

        public void EnableBulletTime()
        {
            _isBulletTimeActive = true;
            SetTimeScale(bulletTimeScale);
        }

        public void DisableBulletTime()
        {
            _isBulletTimeActive = false;
            SetTimeScale(normalTimeScale);
        }

        private void SetTimeScale(float newTimeScale)
        {
            Time.timeScale = newTimeScale;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
        }

        private void StartGame()
        {
            _gameStarted = true;

            // Spawn the bullet
            GameObject bullet = Instantiate(bulletPrefab, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
            _bulletTransform = bullet.transform;
            _startPosition = bullet.transform.position;

            // Play bullet fire effect
            if (bulletFireFXPrefab != null)
            {
                Instantiate(bulletFireFXPrefab, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
            }

            // Start with normal time scale
            DisableBulletTime();
        }
    }
}
