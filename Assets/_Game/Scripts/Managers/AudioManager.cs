using UnityEngine;

namespace _Game.Scripts.Managers
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager instance;

        [Header("General Volume")]
        [Range(0f, 1f)]
        public float masterVolume = 1f;

        [Header("Audio Sources")]
        [Tooltip("Primary one-shot SFX source.")]
        public AudioSource sfxSource;

        [Tooltip("Looping audio source for wind or other loops.")]
        public AudioSource windSource;

        [Tooltip("Dedicated music audio source for background music.")]
        public AudioSource musicSource;

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

        [Header("Enemy Hit / Blood Spurt (Random Clips)")]
        [Tooltip("One of these clips will be chosen randomly each time.")]
        public AudioClip[] enemyHitClips;
        [Range(0f, 1f)] public float enemyHitVolume = 1f;
        public float enemyHitPitchMin = 0.9f;
        public float enemyHitPitchMax = 1.1f;

        [Header("Bullet Time Enter/Exit")]
        public AudioClip bulletTimeEnterClip;
        public AudioClip bulletTimeExitClip;
        [Range(0f, 1f)] public float bulletTimeVolume = 1f;
        public float bulletTimePitchMin = 0.95f;
        public float bulletTimePitchMax = 1.05f;

        [Header("UI Click (Random Clips)")]
        [Tooltip("One of these clips will be chosen randomly each time a UI button is clicked.")]
        public AudioClip[] uiClickClips;
        [Range(0f, 1f)] public float uiClickVolume = 1f;
        public float uiClickPitchMin = 0.95f;
        public float uiClickPitchMax = 1.05f;

        [Header("Title Music (Loop)")]
        [Tooltip("Plays at Title screen, looped, pitch=1, no randomization.")]
        public AudioClip titleMusicClip;
        [Range(0f, 1f)] public float titleMusicVolume = 1f;

        [Header("Start Game One-Shot")]
        [Tooltip("Plays once when user clicks Start. No pitch randomization.")]
        public AudioClip startGameClip;
        [Range(0f, 1f)] public float startGameVolume = 1f;

        [Header("In-Game Music (Loop)")]
        public AudioClip inGameMusicClip;
        [Range(0f, 1f)] public float inGameMusicVolume = 1f;

        [Header("Game Over Music")]
        public AudioClip gameOverMusicClip;
        [Range(0f, 1f)] public float gameOverMusicVolume = 1f;

        [Header("Game Win Music")]
        public AudioClip gameWinMusicClip;
        [Range(0f, 1f)] public float gameWinMusicVolume = 1f;

        void Awake()
        {
            if (instance == null) instance = this;
            else Destroy(gameObject);
            DontDestroyOnLoad(gameObject);

            if (!sfxSource) sfxSource = gameObject.AddComponent<AudioSource>();
            if (!windSource) windSource = gameObject.AddComponent<AudioSource>();
            if (!musicSource) musicSource = gameObject.AddComponent<AudioSource>();

            windSource.loop = true;
            windSource.clip = windLoopClip;
            windSource.volume = 0f;
            windSource.playOnAwake = false;
            windSource.Play();
        }

        public void PlayBulletFire()
        {
            PlayOneShotRandomPitch(bulletFireClip, bulletFireVolume, bulletFirePitchMin, bulletFirePitchMax);
        }

        public void PlayEnemyHit()
        {
            var clip = GetRandomClip(enemyHitClips);
            PlayOneShotRandomPitch(clip, enemyHitVolume, enemyHitPitchMin, enemyHitPitchMax);
        }

        public void PlayBulletTimeEnter()
        {
            PlayOneShotRandomPitch(bulletTimeEnterClip, bulletTimeVolume, bulletTimePitchMin, bulletTimePitchMax);
        }

        public void PlayBulletTimeExit()
        {
            PlayOneShotRandomPitch(bulletTimeExitClip, bulletTimeVolume, bulletTimePitchMin, bulletTimePitchMax);
        }

        public void PlayUIClick()
        {
            var clip = GetRandomClip(uiClickClips);
            PlayOneShotRandomPitch(clip, uiClickVolume, uiClickPitchMin, uiClickPitchMax);
        }

        // Title music loop, no random pitch, volume uses titleMusicVolume
        public void PlayTitleMusic()
        {
            PlayMusic(titleMusicClip, titleMusicVolume, loop: true, pitch: 1f);
        }

        // One-shot start clip, no random pitch
        public void PlayStartGameOneShot()
        {
            PlayOneShotNoPitch(startGameClip, startGameVolume);
        }

        // Replace the title music with in-game loop
        public void PlayInGameMusic()
        {
            PlayMusic(inGameMusicClip, inGameMusicVolume, loop: true, pitch: 1f);
        }

        public void PlayGameOverMusic()
        {
            PlayMusic(gameOverMusicClip, gameOverMusicVolume, loop: false, pitch: 1f);
        }

        public void PlayGameWinMusic()
        {
            PlayMusic(gameWinMusicClip, gameWinMusicVolume, loop: false, pitch: 1f);
        }

        // Continuously set wind pitch/volume based on bullet speed if desired
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

        void PlayOneShotRandomPitch(AudioClip clip, float volume, float pitchMin, float pitchMax)
        {
            if (!clip || !sfxSource) return;
            sfxSource.pitch = Random.Range(pitchMin, pitchMax);
            sfxSource.volume = volume * masterVolume;
            sfxSource.PlayOneShot(clip);
        }

        void PlayOneShotNoPitch(AudioClip clip, float volume)
        {
            if (!clip || !sfxSource) return;
            sfxSource.pitch = 1f;
            sfxSource.volume = volume * masterVolume;
            sfxSource.PlayOneShot(clip);
        }

        void PlayMusic(AudioClip clip, float volume, bool loop, float pitch)
        {
            if (!musicSource) return;
            if (!clip)
            {
                // if no clip, maybe stop music
                musicSource.Stop();
                return;
            }
            musicSource.volume = volume * masterVolume;
            musicSource.pitch = pitch;
            musicSource.loop = loop;
            musicSource.clip = clip;
            musicSource.Play();
        }

        AudioClip GetRandomClip(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return null;
            int index = Random.Range(0, clips.Length);
            return clips[index];
        }
    }
}
