using UnityEngine;
using System.Collections;
using _Game.Scripts.Bullet;
using _Game.Scripts.Cameras; // for BulletAimCameraRig

namespace _Game.Scripts.Managers
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager instance;

        [Header("Time Scale Settings")]
        public float normalTimeScale = 0.4f;
        public float bulletTimeScale = 0.1f;
        public float timeScaleStep = 0.02f;
        public float timeScaleStepInterval = 0.1f;

        [Header("Bullet Time Settings")]
        public float bulletTimeDelay = 0.005f;

        [Header("References")]
        public SimpleBulletController bulletInScene;
        // Add a reference to your camera rig (drag in inspector)
        public BulletAimCameraRig bulletAimCameraRig;

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
            if (bulletInScene && bulletInScene.gameObject.activeSelf)
                bulletInScene.gameObject.SetActive(false);
        }

        private void Update()
        {
            // example: user left-click => first bullet spawn
            if (!_gameStarted && Input.GetMouseButtonDown(0))
            {
                _gameStarted = true;
                ActivateAndFireBullet();
            }
        }

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

            // after short delay, we do bullet time
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
            // Now we've slowed time. Let bullet steer:
            if (bulletInScene) bulletInScene.EnterBulletTime();

            // Also manually tell the camera rig to pick the aim cam
            if (bulletAimCameraRig != null)
                bulletAimCameraRig.BulletTime.Value = 1f;

            // Now the user sees the aim camera, can move mouse to aim the bullet. 
            // Next step: we want to detect user click to "fire" again, then revert?
            // We can watch for that here or in Update(). For example:
            StartCoroutine(WaitForBulletFireThenRevert());
        }

        private IEnumerator WaitForBulletFireThenRevert()
        {
            Debug.Log("BulletTime active: user can aim. Click to finalize and revert.");
            yield return new WaitUntil(() => Input.GetMouseButtonDown(0));

            // Step 1: Immediately exit bullet time so the bullet is no longer slow
            if (bulletInScene) bulletInScene.ExitBulletTime();  // sets _isBulletTime = false
            // Also kill the slow timescale if you want an instant revert:
            SetTimeScale(1f); 
            // Optionally skip the gradual ramp if you want an immediate jump.
            // or you can do: StartCoroutine(GraduallyIncreaseTimeScale()) but then also do a tiny yield

            // Step 2: ReFire bullet at normal speed (no bullet time)
            bulletInScene.ReFireInCurrentAimDirection(); 

            // Finally switch the camera rig to free camera
            if (bulletAimCameraRig != null)
                bulletAimCameraRig.BulletTime.Value = 0f;

            // Revert time scale
            ResumeNormalTime();
        }

        public void ResumeNormalTime()
        {
            StartCoroutine(GraduallyIncreaseTimeScale());
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

            // Now we switch camera rig to free camera
            if (bulletAimCameraRig != null)
                bulletAimCameraRig.BulletTime.Value = 0f;

            // also bullet no longer in bullet time
            if (bulletInScene)
                bulletInScene.ExitBulletTime();
        }

        private void SetTimeScale(float timeScale)
        {
            Time.timeScale = timeScale;
            Time.fixedDeltaTime = 0.02f * timeScale;
        }
        
        public void StartQuickBulletTime()
        {
            // Possibly override the step or interval to something bigger 
            // so we go from e.g. 0.4f -> 0.1f quickly in about 0.3 seconds total.
            float quickStep = 0.03f;
            float quickInterval = 0.02f; 
            // Adjust these to taste.

            StopAllCoroutines(); // if you want to cancel any existing time ramp
            StartCoroutine(QuicklyDecreaseTimeScale(quickStep, quickInterval));
        }

        private IEnumerator QuicklyDecreaseTimeScale(float step, float interval)
        {
            float currentScale = Time.timeScale;
            while (currentScale > bulletTimeScale)
            {
                currentScale -= step;
                SetTimeScale(Mathf.Max(currentScale, bulletTimeScale));
                yield return new WaitForSecondsRealtime(interval);
            }

            // Once we get there, enable the bullet's internal bullet-time
            if (bulletInScene)
                bulletInScene.EnterBulletTime();

            // Also pick the aim camera
            if (bulletAimCameraRig != null)
                bulletAimCameraRig.BulletTime.Value = 1f;

            // If you want to re-initiate a "WaitForBulletFireThenRevert()" afterwards, 
            // you can do so here, or you can let the user aim manually, etc.
            Debug.Log("Reached bullet-time quickly after enemy hit!");
        }
    }
}
