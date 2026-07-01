using UnityEngine;

namespace DustBot
{
    public sealed class AudioManager : MonoBehaviour
    {
        private const int SampleRate = 24000;
        private const float BaseMusicVolume = 0.18f;

        private AudioSource effectsSource;
        private AudioSource musicSource;
        private AudioClip buttonTap;
        private AudioClip pathEdit;
        private AudioClip move;
        private AudioClip crumb;
        private AudioClip dock;
        private AudioClip win;
        private AudioClip fail;
        private AudioClip hint;
        private AudioClip reward;
        private AudioClip catStep;
        private AudioClip catPounce;
        private AudioClip menuMusic;
        private AudioClip gameplayMusic;
        private AudioClip dailyMusic;
        private AudioClip catMusic;
        private string currentMusicKey = string.Empty;
        private bool soundEnabled = true;
        private bool musicEnabled = true;
        private float soundVolume = 1f;
        private float musicVolume = 0.8f;

        public bool SoundEnabled
        {
            get { return soundEnabled; }
            set { soundEnabled = value; }
        }

        public bool MusicEnabled
        {
            get { return musicEnabled; }
            set
            {
                musicEnabled = value;
                RefreshMusic();
            }
        }

        public float SoundVolume
        {
            get { return soundVolume; }
            set { soundVolume = Mathf.Clamp01(value); }
        }

        public float MusicVolume
        {
            get { return musicVolume; }
            set
            {
                musicVolume = Mathf.Clamp01(value);
                ApplyMusicVolume();
            }
        }

        private void Awake()
        {
            if (FindAnyObjectByType<AudioListener>() == null)
            {
                AudioListener listener = gameObject.AddComponent<AudioListener>();
                listener.enabled = true;
            }

            AudioListener.pause = false;
            AudioListener.volume = 1f;
            IOSAudioSession.Configure();

            effectsSource = gameObject.AddComponent<AudioSource>();
            effectsSource.playOnAwake = false;
            effectsSource.spatialBlend = 0f;
            effectsSource.ignoreListenerPause = true;
            effectsSource.priority = 32;

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
            musicSource.volume = BaseMusicVolume * musicVolume;
            musicSource.ignoreListenerPause = true;
            musicSource.priority = 96;

            buttonTap = CreateTone("Button Tap", 510f, 0.055f, 0.18f, 0.45f);
            pathEdit = CreateTone("Path Edit", 690f, 0.045f, 0.12f, 0.52f);
            move = CreateTone("Bot Move", 205f, 0.07f, 0.13f, 0.35f);
            crumb = CreateChord("Crumb Clean", 0.15f, 0.2f, 0.66f, 880f, 1175f);
            dock = CreateChord("Dock", 0.22f, 0.18f, 0.58f, 392f, 588f);
            win = CreateArpeggio("Win", new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.11f, 0.22f);
            fail = CreateDescendingTone("Fail", 245f, 130f, 0.32f, 0.2f);
            hint = CreateArpeggio("Hint", new[] { 659.25f, 880f }, 0.1f, 0.16f);
            reward = CreateArpeggio("Reward", new[] { 784f, 988f, 1175f }, 0.08f, 0.18f);
            catStep = CreateTone("Cat Step", 330f, 0.055f, 0.1f, 0.18f);
            catPounce = CreateDescendingTone("Cat Pounce", 520f, 230f, 0.18f, 0.17f);
            menuMusic = CreateMusicLoop(
                "DustBot Menu Marshmallow March",
                new[] { 523.25f, 659.25f, 783.99f, 659.25f, 587.33f, 698.46f, 880f, 698.46f },
                10f,
                2.15f,
                0.18f,
                0.75f);
            gameplayMusic = CreateMusicLoop(
                "DustBot Cozy Puzzle Hop",
                new[] { 392f, 493.88f, 587.33f, 659.25f, 587.33f, 493.88f, 440f, 493.88f },
                12f,
                1.9f,
                0.14f,
                0.5f);
            dailyMusic = CreateMusicLoop(
                "DustBot Daily Bell Bounce",
                new[] { 440f, 554.37f, 659.25f, 830.61f, 739.99f, 659.25f, 554.37f, 493.88f },
                12f,
                2.05f,
                0.2f,
                0.62f);
            catMusic = CreateMusicLoop(
                "DustBot Sneaky Cat Tiptoe",
                new[] { 349.23f, 415.3f, 523.25f, 622.25f, 523.25f, 466.16f, 415.3f, 392f },
                10f,
                2.35f,
                0.26f,
                0.88f);
            SwitchMusic("menu", menuMusic);
            RefreshMusic();
        }

        public void PlayButtonTap() { Play(buttonTap, 0.8f); }
        public void PlayPathEdit() { Play(pathEdit, 0.7f); }
        public void PlayMove() { Play(move, 0.65f); }
        public void PlayCrumbClean() { Play(crumb, 0.9f); }
        public void PlayDock() { Play(dock, 0.85f); }
        public void PlayWin() { Play(win, 1f); }
        public void PlayFail() { Play(fail, 0.9f); }
        public void PlayHint() { Play(hint, 0.85f); }
        public void PlayReward() { Play(reward, 0.9f); }
        public void PlayCatStep(bool pounce) { Play(pounce ? catPounce : catStep, pounce ? 0.9f : 0.48f); }
        public void PlayMenuMusic() { SwitchMusic("menu", menuMusic); }
        public void PlayGameplayMusic(LevelDefinition level)
        {
            if (level != null && level.cat != null && level.cat.IsEnabled)
            {
                SwitchMusic("cat", catMusic);
                return;
            }

            if (level != null && level.mode == GameMode.DailyChallenge)
            {
                SwitchMusic("daily", dailyMusic);
                return;
            }

            SwitchMusic("gameplay", gameplayMusic);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                return;
            }

            AudioListener.pause = false;
            IOSAudioSession.Configure();
            RefreshMusic();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus)
            {
                AudioListener.pause = false;
                IOSAudioSession.Configure();
                RefreshMusic();
            }
        }

        private void Play(AudioClip clip, float volume)
        {
            if (soundEnabled && effectsSource != null && clip != null)
            {
                effectsSource.PlayOneShot(clip, volume * soundVolume);
            }
        }

        private void RefreshMusic()
        {
            if (musicSource == null)
            {
                return;
            }

            if (musicEnabled)
            {
                if (!musicSource.isPlaying)
                {
                    musicSource.Play();
                }
            }
            else
            {
                musicSource.Pause();
            }
        }

        private void SwitchMusic(string key, AudioClip clip)
        {
            if (musicSource == null || clip == null || currentMusicKey == key)
            {
                return;
            }

            bool wasPlaying = musicSource.isPlaying;
            currentMusicKey = key;
            musicSource.clip = clip;
            musicSource.time = 0f;
            ApplyMusicVolume();
            if (musicEnabled && (wasPlaying || !musicSource.isPlaying))
            {
                musicSource.Play();
            }
        }

        private void ApplyMusicVolume()
        {
            if (musicSource != null)
            {
                musicSource.volume = BaseMusicVolume * musicVolume;
            }
        }

        private static AudioClip CreateTone(
            string name,
            float frequency,
            float duration,
            float amplitude,
            float brightness)
        {
            int sampleCount = Mathf.CeilToInt(duration * SampleRate);
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / SampleRate;
                float progress = (float)i / Mathf.Max(1, sampleCount - 1);
                float envelope = Mathf.Sin(progress * Mathf.PI) * Mathf.Pow(1f - progress, 0.35f);
                float fundamental = Mathf.Sin(t * frequency * Mathf.PI * 2f);
                float overtone = Mathf.Sin(t * frequency * 2f * Mathf.PI * 2f) * brightness;
                samples[i] = (fundamental + overtone) * envelope * amplitude;
            }

            return CreateClip(name, samples);
        }

        private static AudioClip CreateChord(
            string name,
            float duration,
            float amplitude,
            float brightness,
            params float[] frequencies)
        {
            int sampleCount = Mathf.CeilToInt(duration * SampleRate);
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / SampleRate;
                float progress = (float)i / Mathf.Max(1, sampleCount - 1);
                float envelope = Mathf.Sin(progress * Mathf.PI) * Mathf.Pow(1f - progress, 0.4f);
                float value = 0f;
                for (int f = 0; f < frequencies.Length; f++)
                {
                    value += Mathf.Sin(t * frequencies[f] * Mathf.PI * 2f);
                    value += Mathf.Sin(t * frequencies[f] * 2f * Mathf.PI * 2f) * brightness * 0.18f;
                }

                samples[i] = value / Mathf.Max(1, frequencies.Length) * envelope * amplitude;
            }

            return CreateClip(name, samples);
        }

        private static AudioClip CreateArpeggio(
            string name,
            float[] frequencies,
            float noteDuration,
            float amplitude)
        {
            int noteSamples = Mathf.CeilToInt(noteDuration * SampleRate);
            float[] samples = new float[noteSamples * frequencies.Length];
            for (int note = 0; note < frequencies.Length; note++)
            {
                for (int i = 0; i < noteSamples; i++)
                {
                    float t = (float)i / SampleRate;
                    float progress = (float)i / Mathf.Max(1, noteSamples - 1);
                    float envelope = Mathf.Sin(progress * Mathf.PI);
                    int index = note * noteSamples + i;
                    samples[index] =
                        (Mathf.Sin(t * frequencies[note] * Mathf.PI * 2f) +
                         Mathf.Sin(t * frequencies[note] * 2f * Mathf.PI * 2f) * 0.2f) *
                        envelope *
                        amplitude;
                }
            }

            return CreateClip(name, samples);
        }

        private static AudioClip CreateDescendingTone(
            string name,
            float startFrequency,
            float endFrequency,
            float duration,
            float amplitude)
        {
            int sampleCount = Mathf.CeilToInt(duration * SampleRate);
            float[] samples = new float[sampleCount];
            float phase = 0f;
            for (int i = 0; i < sampleCount; i++)
            {
                float progress = (float)i / Mathf.Max(1, sampleCount - 1);
                float frequency = Mathf.Lerp(startFrequency, endFrequency, progress);
                phase += frequency / SampleRate;
                float envelope = Mathf.Pow(1f - progress, 0.65f);
                samples[i] = Mathf.Sin(phase * Mathf.PI * 2f) * envelope * amplitude;
            }

            return CreateClip(name, samples);
        }

        private static AudioClip CreateMusicLoop(
            string name,
            float[] melody,
            float duration,
            float tempo,
            float bellAmount,
            float percussionAmount)
        {
            int sampleCount = Mathf.CeilToInt(duration * SampleRate);
            float[] samples = new float[sampleCount * 2];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / SampleRate;
                float beat = t * tempo;
                int noteIndex = Mathf.FloorToInt(beat) % melody.Length;
                float withinBeat = beat - Mathf.Floor(beat);
                float root = melody[noteIndex];
                float pluckEnvelope = Mathf.Exp(-withinBeat * 5.4f) * Mathf.Sin(Mathf.Clamp01(withinBeat * 3.5f) * Mathf.PI);
                float marimba =
                    Mathf.Sin(t * root * Mathf.PI * 2f) * 0.55f +
                    Mathf.Sin(t * root * 2f * Mathf.PI * 2f) * 0.18f +
                    Mathf.Sin(t * root * 3f * Mathf.PI * 2f) * 0.08f;
                float bell =
                    Mathf.Sin(t * root * 2f * Mathf.PI * 2f) *
                    Mathf.Exp(-withinBeat * 3.2f) *
                    bellAmount;
                float bass =
                    Mathf.Sin(t * (root * 0.25f) * Mathf.PI * 2f) *
                    (0.12f + Mathf.Sin(t * 0.5f * Mathf.PI * 2f) * 0.025f);
                float eighth = (beat * 2f) - Mathf.Floor(beat * 2f);
                float tick =
                    Mathf.Sin(t * 1450f * Mathf.PI * 2f) *
                    Mathf.Exp(-eighth * 34f) *
                    0.025f *
                    percussionAmount;
                float edgeFade = Mathf.Clamp01(Mathf.Min(i, sampleCount - 1 - i) / (SampleRate * 0.035f));
                float value = (marimba * pluckEnvelope * 0.18f + bell * 0.06f + bass + tick) * edgeFade;
                samples[i * 2] = value * 0.94f;
                samples[i * 2 + 1] = value;
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 2, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateClip(string name, float[] samples)
        {
            AudioClip clip = AudioClip.Create(name, samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
