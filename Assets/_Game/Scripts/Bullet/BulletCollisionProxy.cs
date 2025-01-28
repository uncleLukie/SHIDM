using UnityEngine;
using _Game.Scripts.Managers;
using Polyperfect.Common; // If you need Common_WanderScript references

namespace _Game.Scripts.Bullet
{
    /// <summary>
    /// This script sits on the pistol-bullet model child, which has a MeshCollider (isTrigger).
    /// It detects collisions with enemies/environment, and then calls the parent
    /// SimpleBulletController to handle the logic (killing enemy, bullet time, bullet end, etc.).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class BulletCollisionProxy : MonoBehaviour
    {
        [Tooltip("Layer name for 'Enemy'.  We'll detect hits and forward to BulletController.")]
        public string enemyLayerName = "Enemy";

        [Tooltip("Layer name for 'Environment'. We'll detect hits and forward to BulletController.")]
        public string environmentLayerName = "Environment";

        [Tooltip("Optional blood prefab spawned when we hit an enemy.")]
        public GameObject bloodFXPrefab;

        private SimpleBulletController _bulletController; // Reference to parent bullet logic

        private void Awake()
        {
            // Ensure the collider is a trigger
            Collider coll = GetComponent<Collider>();
            if (coll) coll.isTrigger = true;

            // Find the parent bullet controller
            _bulletController = GetComponentInParent<SimpleBulletController>();
            if (!_bulletController)
            {
                Debug.LogError("PistolBulletCollisionProxy: No SimpleBulletController found in parent!", this);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Only do something if the bullet is currently flying
            if (_bulletController == null || !_bulletController.IsFired)
                return;

            // Check the object's layer
            int layer = other.gameObject.layer;
            string layerName = LayerMask.LayerToName(layer);

            if (layerName == enemyLayerName)
            {
                Debug.Log("Bullet hit Enemy: " + other.name);

                // Optionally kill the enemy
                var wander = other.GetComponent<Common_WanderScript>();
                if (wander != null)
                    wander.Die();

                // Spawn blood
                if (bloodFXPrefab != null)
                {
                    Instantiate(bloodFXPrefab, transform.position, Quaternion.identity);
                }

                // Trigger bullet time again so user can re-aim
                // or partial bullet time logic
                GameManager.instance.StartGradualBulletTime();

                // If you want the bullet to end on hitting enemy, call:
                // _bulletController.EndBullet("Hit enemy");
                // Otherwise let it keep flying
            }
            else if (layerName == environmentLayerName)
            {
                Debug.Log("Bullet hit environment: " + other.name);

                // End bullet
                _bulletController.EndBullet("Hit environment");
            }
        }
    }
}
