using UnityEngine;

namespace _Game.Scripts.Managers
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager instance;

        [Header("General Settings")]
        [Tooltip("Master volume to scale all SFX volumes. 0 = silent, 1 = normal.")]
        [Range(0f, 1f)]
        public float masterVolume = 1f;

        [Header("Audio Sources")]
        [Tooltip("Audio source for short one-shot SFX.")]
        public AudioSource sfxSource;

        [Tooltip("Looping audio source dedicated for wind sound.")]
        public AudioSource windSource;

        [Header("Bullet Fire")]
        public AudioClip bulletFireClip;
        [Range(0f, 1f)] public float bulletFireVolume = 1f;
        public float bulletFirePitchMin = 0.9f;
        public float bulletFirePitchMax = 1.1f;

        [Header("Bullet Wind (loop)")]
        public AudioClip windLoopClip;
        [Range(0f, 1f)] public float windVolume = 0.3f;
        public float windPitchMin = 0.8f;
        public float windPitchMax = 1.2f;
        public float maxBulletSpeedForWind = 80f;

        [Header("Enemy Hit / Blood Spurt")]
        public AudioClip enemyHitClip;
        [Range(0f, 1f)] public float enemyHitVolume = 1f;
        public float enemyHitPitchMin = 0.9f;
        public float enemyHitPitchMax = 1.1f;

        [Header("Bullet Time Enter/Exit")]
        public AudioClip bulletTimeEnterClip;
        public AudioClip bulletTimeExitClip;
        [Range(0f, 1f)] public float bulletTimeVolume = 1f;
        public float bulletTimePitchMin = 0.95f;
        public float bulletTimePitchMax = 1.05f;

        [Header("UI Click")]
        public AudioClip uiClickClip;
        [Range(0f, 1f)] public float uiClickVolume = 1f;
        public float uiClickPitchMin = 0.95f;
        public float uiClickPitchMax = 1.05f;

        void Awake()
        {
            if (instance == null) instance = this;
            else Destroy(gameObject);
            DontDestroyOnLoad(gameObject);

            if (!sfxSource) sfxSource = gameObject.AddComponent<AudioSource>();
            if (!windSource) windSource = gameObject.AddComponent<AudioSource>();

            windSource.loop = true;
            windSource.clip = windLoopClip;
            windSource.volume = 0f;
            windSource.playOnAwake = false;
            windSource.Play(); // plays silently unless you update volume
        }

        void Update()
        {
            // Optionally, you could update wind each frame here
        }

        public void PlayBulletFire()
        {
            PlayOneShot(bulletFireClip, bulletFireVolume, bulletFirePitchMin, bulletFirePitchMax);
        }

        public void PlayEnemyHit()
        {
            PlayOneShot(enemyHitClip, enemyHitVolume, enemyHitPitchMin, enemyHitPitchMax);
        }

        public void PlayBulletTimeEnter()
        {
            PlayOneShot(bulletTimeEnterClip, bulletTimeVolume, bulletTimePitchMin, bulletTimePitchMax);
        }

        public void PlayBulletTimeExit()
        {
            PlayOneShot(bulletTimeExitClip, bulletTimeVolume, bulletTimePitchMin, bulletTimePitchMax);
        }

        public void PlayUIClick()
        {
            PlayOneShot(uiClickClip, uiClickVolume, uiClickPitchMin, uiClickPitchMax);
        }

        public void UpdateWind(float bulletSpeed)
        {
            if (!windSource || !windLoopClip) return;
            float speedRatio = Mathf.Clamp01(bulletSpeed / maxBulletSpeedForWind);
            float pitch = Mathf.Lerp(windPitchMin, windPitchMax, speedRatio);
            float vol = windVolume * speedRatio * masterVolume;
            windSource.pitch = pitch;
            windSource.volume = vol;
        }

        public void StopWind()
        {
            if (windSource) windSource.volume = 0f;
        }

        void PlayOneShot(AudioClip clip, float volume, float pitchMin, float pitchMax)
        {
            if (!clip || !sfxSource) return;
            sfxSource.pitch = Random.Range(pitchMin, pitchMax);
            sfxSource.volume = volume * masterVolume;
            sfxSource.PlayOneShot(clip);
        }
    }
}
