using UnityEngine;
using Polyperfect.Common;
using _Game.Scripts.Managers;

namespace _Game.Scripts.Bullet
{
    [RequireComponent(typeof(Collider))]
    public class BulletCollisionProxy : MonoBehaviour
    {
        public string enemyLayerName = "Enemy";
        public string environmentLayerName = "Environment";

        public GameObject bloodFXPrefab;

        private SimpleBulletController bulletController;

        // Optionally store the bullet's last position each frame so we can raycast
        private Vector3 _prevPosition;

        private void Awake()
        {
            var coll = GetComponent<Collider>();
            if (coll) coll.isTrigger = true;
            
            bulletController = GetComponentInParent<SimpleBulletController>();
            if (!bulletController)
            {
                Debug.LogError("BulletCollisionProxy: No SimpleBulletController in parent!", this);
            }
        }

        private void OnEnable()
        {
            _prevPosition = transform.position;
        }

        private void Update()
        {
            // We store the bullet model's previous position so we can raycast
            // if we collide with environment.
            _prevPosition = transform.position;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (bulletController == null || !bulletController.IsFired)
                return;

            string layerName = LayerMask.LayerToName(other.gameObject.layer);

            if (layerName == enemyLayerName)
            {
                // Possibly kill enemy
                var wander = other.GetComponent<Common_WanderScript>();
                if (wander != null) wander.Die();

                // Blood
                if (bloodFXPrefab != null)
                    Instantiate(bloodFXPrefab, transform.position, Quaternion.identity);
                
                // Re-enter bullet time quickly
                GameManager.instance.EnterBulletTimeAfterEnemyHit();
            }
            else if (layerName == environmentLayerName)
            {
                Debug.Log("Hit environment");
                // We attempt a ricochet
                // To find an approximate normal, we do a raycast from _prevPosition to current
                // position. If that fails, we just do a simple "up" normal or something.

                Vector3 currentPos = transform.position;
                Vector3 dir = (currentPos - _prevPosition).normalized;
                float dist = Vector3.Distance(_prevPosition, currentPos);

                // We'll cast from the parent bullet's previous position to new position
                // in case the child is offset. 
                var parentPos = bulletController.transform.position;
                var castDir = (parentPos - (parentPos - dir * dist)).normalized;
                float castDist = dist + 0.2f;

                RaycastHit hitInfo;
                Vector3 hitNormal = Vector3.up; // fallback
                if (Physics.Raycast(parentPos - dir * dist, castDir, out hitInfo, castDist,
                    1 << other.gameObject.layer, QueryTriggerInteraction.Ignore))
                {
                    hitNormal = hitInfo.normal;
                }

                bool success = bulletController.TryRicochet(hitNormal);
                if (!success)
                {
                    // If no ricochet left -> game over
                    GameManager.instance.GameOver("No ricochets left!");
                }
            }
        }
    }
}
