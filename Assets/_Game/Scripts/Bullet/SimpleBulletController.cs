using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using Polyperfect.Common;
using _Game.Scripts.Managers;
using Unity.Cinemachine;

namespace _Game.Scripts.Bullet
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public class SimpleBulletController : MonoBehaviour, IInputAxisOwner
    {
        int enemyLayer;
        int environmentLayer;
        int bossLayer;

        public float normalSpeed = 40f;
        public float bulletTimeSpeed = 2f;
        public float gravity = 0.4f;
        public bool reduceGravityInBulletTime = true;
        public float maxDistance = 150f;
        public int ricochetCount = 1;
        public float maxFlightSeconds = 20f;

        public UnityEvent onBulletFired;
        public UnityEvent onBulletEnd;

        public GameObject bloodFXPrefab;
        public GameObject muzzleFlashFX;

        public Action PreUpdate;
        public Action<Vector3, float> PostUpdate;

        bool fired;
        bool isBulletTime;
        Vector3 lastPosition;
        float distanceTraveled;
        float verticalVelocity;
        float flightTimer;
        bool didBounceThisFrame;

        public bool IsFired => fired;

        void Awake()
        {
            enemyLayer = LayerMask.NameToLayer("Enemy");
            environmentLayer = LayerMask.NameToLayer("Environment");
            bossLayer = LayerMask.NameToLayer("Boss");

            var rb = GetComponent<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
            var coll = GetComponent<Collider>();
            if (coll && !coll.isTrigger)
            {
                coll.isTrigger = true;
                Debug.LogWarning("SimpleBulletController: Setting collider isTrigger=true at runtime!");
            }
        }

        void OnEnable()
        {
            fired = false;
            isBulletTime = false;
            lastPosition = transform.position;
            distanceTraveled = 0f;
            verticalVelocity = 0f;
            flightTimer = 0f;
            didBounceThisFrame = false;
        }

        void Update()
        {
            PreUpdate?.Invoke();

            if (!fired)
            {
                PostUpdate?.Invoke(Vector3.zero, 1f);
                return;
            }

            flightTimer += Time.deltaTime;
            if (maxFlightSeconds > 0 && flightTimer >= maxFlightSeconds)
            {
                GameManager.instance.GameOver("Flight time exceeded");
                EndBullet("Flight time exceeded");
                return;
            }

            float speed = isBulletTime ? bulletTimeSpeed : normalSpeed;
            float g = (reduceGravityInBulletTime && isBulletTime) ? 0f : gravity;
            verticalVelocity -= g * Time.deltaTime;

            Vector3 moveFrame = (transform.forward * speed + Vector3.up * verticalVelocity) * Time.deltaTime;
            transform.position += moveFrame;

            float distFrame = (transform.position - lastPosition).magnitude;
            distanceTraveled += distFrame;
            lastPosition = transform.position;

            if (maxDistance > 0 && distanceTraveled >= maxDistance)
            {
                GameManager.instance.GameOver("Distance limit reached");
                EndBullet("Distance limit reached");
                return;
            }

            Vector3 localVel = Quaternion.Inverse(transform.rotation)
                               * (transform.forward * speed + Vector3.up * verticalVelocity);
            PostUpdate?.Invoke(localVel, 1f);

            didBounceThisFrame = false;
        }

        public void FireBullet()
        {
            if (fired) return;
            fired = true;
            distanceTraveled = 0f;
            lastPosition = transform.position;
            verticalVelocity = 0f;
            flightTimer = 0f;
            
            if (muzzleFlashFX)
            {
                GameObject flash = Instantiate(muzzleFlashFX, transform.position, Quaternion.identity);
                Destroy(flash, 0.3f);
            }

            onBulletFired?.Invoke();
            AudioManager.instance.PlayBulletFire();
        }

        public void EnterBulletTime() => isBulletTime = true;
        public void ExitBulletTime() => isBulletTime = false;

        void EndBullet(string reason)
        {
            Debug.Log($"Bullet ended: {reason}");
            onBulletEnd?.Invoke();
            fired = false;
            gameObject.SetActive(false);
        }

        public void ForceEndNow()
        {
            if (!fired) return;
            EndBullet("ForceEnd");
        }

        public void ReFireInCurrentAimDirection()
        {
            verticalVelocity = 0f;
            var aimCore = transform.Find("Player Aiming Core");
            if (aimCore) transform.rotation = aimCore.rotation;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!fired) return;
            if (didBounceThisFrame) return;

            int layer = other.gameObject.layer;

            if (layer == enemyLayer)
            {
                DoBloodSpurt(other);
                DoKillWander(other);
                distanceTraveled = 0f;
                AudioManager.instance.PlayEnemyHit();
                GameManager.instance.EnterBulletTimeAfterEnemyHit();
            }
            else if (layer == bossLayer)
            {
                DoBloodSpurt(other);
                DoKillWander(other);
                distanceTraveled = 0f;
                AudioManager.instance.PlayEnemyHit();
                StartCoroutine(DoBossKillFlow());
            }
            else if (layer == environmentLayer)
            {
                Vector3 hitNormal = FindSurfaceNormalOfEnvironment(other);
                bool success = TryRicochet(hitNormal);
                didBounceThisFrame = true;
                if (success)
                {
                    distanceTraveled = 0f;
                    GameManager.instance.EnterBulletTimeAfterEnemyHit();
                }
                else
                {
                    GameManager.instance.GameOver("No ricochets left!");
                }
            }
        }

        IEnumerator DoBossKillFlow()
        {
            yield return new WaitForSeconds(0.4f);
            GameManager.instance.GameWin();
        }

        void DoBloodSpurt(Collider col)
        {
            Vector3 bloodPosition = FindBloodHitPosition(col);
            if (bloodFXPrefab)
                Instantiate(bloodFXPrefab, bloodPosition, Quaternion.identity);
        }

        void DoKillWander(Collider col)
        {
            var wander = col.GetComponent<Common_WanderScript>();
            if (wander) wander.Die();
        }

        Vector3 FindBloodHitPosition(Collider targetCol)
        {
            Vector3 closestSpherePoint = targetCol.ClosestPoint(lastPosition);
            var skinnedMesh = targetCol.GetComponentInChildren<SkinnedMeshRenderer>();
            if (skinnedMesh)
            {
                Vector3 dir = (closestSpherePoint - lastPosition).normalized;
                float distance = Vector3.Distance(lastPosition, closestSpherePoint);
                if (Physics.Raycast(lastPosition, dir, out RaycastHit hit, distance, LayerMask.GetMask("Enemy"), QueryTriggerInteraction.Ignore))
                {
                    return hit.point;
                }
            }
            return closestSpherePoint;
        }

        Vector3 FindSurfaceNormalOfEnvironment(Collider envCollider)
        {
            Vector3 dir = (transform.position - lastPosition).normalized;
            float dist = Vector3.Distance(lastPosition, transform.position);
            Vector3 normal = Vector3.up;
            int envMask = 1 << environmentLayer;

            if (Physics.Raycast(lastPosition, dir, out RaycastHit hitInfo, dist + 0.2f, envMask, QueryTriggerInteraction.Ignore))
            {
                normal = hitInfo.normal;
                transform.position = hitInfo.point + hitInfo.normal * 0.01f;
            }
            return normal;
        }

        bool TryRicochet(Vector3 hitNormal)
        {
            if (ricochetCount <= 0) return false;
            ricochetCount--;

            Vector3 reflect = Vector3.Reflect(transform.forward, hitNormal);
            transform.forward = reflect.normalized;
            verticalVelocity = 0f;
            return true;
        }

        public void GetInputAxes(List<IInputAxisOwner.AxisDescriptor> axes) {}
    }
}
