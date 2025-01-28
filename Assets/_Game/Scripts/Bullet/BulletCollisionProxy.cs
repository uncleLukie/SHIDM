using UnityEngine;
using _Game.Scripts.Managers; // For GameManager
using Polyperfect.Common;      // If you need Common_WanderScript references

namespace _Game.Scripts.Bullet
{
    [RequireComponent(typeof(Collider))]
    public class BulletCollisionProxy : MonoBehaviour
    {
        public string enemyLayerName = "Enemy";
        public string environmentLayerName = "Environment";
        public GameObject bloodFXPrefab;

        private SimpleBulletController _bulletController;

        private void Awake()
        {
            // Ensure the collider is a trigger
            var coll = GetComponent<Collider>();
            if (coll) coll.isTrigger = true;

            // Find parent bullet controller
            _bulletController = GetComponentInParent<SimpleBulletController>();
            if (!_bulletController)
            {
                Debug.LogError("BulletCollisionProxy: No SimpleBulletController in parent!", this);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_bulletController == null || !_bulletController.IsFired)
                return;

            string layerName = LayerMask.LayerToName(other.gameObject.layer);

            if (layerName == enemyLayerName)
            {
                // Possibly kill enemy, spawn blood, etc.
                var wander = other.GetComponent<Common_WanderScript>();
                if (wander != null) wander.Die();

                if (bloodFXPrefab != null)
                    Instantiate(bloodFXPrefab, transform.position, Quaternion.identity);

                // *** New: call a method that quickly transitions to bullet time
                StartCoroutine(SmoothlyEnterBulletTimeAfterEnemyHit());
            }
            else if (layerName == environmentLayerName)
            {
                Debug.Log("Hit environment: " + other.name);
                _bulletController.EndBullet("Environment collision");
            }
        }

        /// <summary>
        /// Wait a tiny bit (optional) then call the GameManager to do a quick bullet-time ramp.
        /// </summary>
        private System.Collections.IEnumerator SmoothlyEnterBulletTimeAfterEnemyHit()
        {
            // Optionally wait one frame so the bullet has fully "passed" the enemy
            yield return new WaitForEndOfFrame();

            // Now call a custom method on GameManager that does a fast bullet-time
            GameManager.instance.StartQuickBulletTime();
        }
    }
}
