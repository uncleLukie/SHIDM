using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Game.Scripts.Managers
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager instance;

        private int _score;
        private float _distanceTraveled;
        private Vector3 _startPosition;
        private Transform _bulletTransform;

        [SerializeField] private float timeScale = 0.1f;

        private void Awake()
        {
            if (instance == null) instance = this;
            else Destroy(gameObject);

            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            Time.timeScale = timeScale;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            
            GameObject bullet = GameObject.FindGameObjectWithTag("Bullet");
            if (bullet != null)
            {
                _bulletTransform = bullet.transform;
                _startPosition = _bulletTransform.position;
            }
            else
            {
                Debug.LogError("Bullet object with tag 'Bullet' not found.");
            }
        }

        private void Update()
        {
            if (_bulletTransform != null)
            {
                _distanceTraveled = Vector3.Distance(_startPosition, _bulletTransform.position);
                Debug.Log($"Distance Traveled: {_distanceTraveled}");
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
    }
}