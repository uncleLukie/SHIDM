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
            var coll = GetComponent<Collider>();
            if (coll) coll.isTrigger = true;
            
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
                var wander = other.GetComponent<Common_WanderScript>();
                if (wander != null) wander.Die();

                if (bloodFXPrefab != null)
                    Instantiate(bloodFXPrefab, transform.position, Quaternion.identity);
                
                StartCoroutine(SmoothlyEnterBulletTimeAfterEnemyHit());
            }
            else if (layerName == enemyLayerName)
            {
                var wander = other.GetComponent<Common_WanderScript>();
                if (wander != null) wander.Die();

                if (bloodFXPrefab != null)
                    Instantiate(bloodFXPrefab, transform.position, Quaternion.identity);
                
                GameManager.instance.EnterBulletTimeAfterEnemyHit();
            }
        }
        
        private System.Collections.IEnumerator SmoothlyEnterBulletTimeAfterEnemyHit()
        {
            yield return new WaitForEndOfFrame();
            
            GameManager.instance.EnterBulletTimeAfterEnemyHit();
        }
    }
}
