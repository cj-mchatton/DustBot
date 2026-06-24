using UnityEngine;

namespace DustBot
{
    public sealed class AudioManager : MonoBehaviour
    {
        private const int SampleRate = 24000;

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
        private bool soundEnabled = true;
        private bool musicEnabled = true;

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
            musicSource.volume = 0.14f;
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
            musicSource.clip = CreateMusicLoop();
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
                effectsSource.PlayOneShot(clip, volume);
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

        private static AudioClip CreateMusicLoop()
        {
            const float duration = 8f;
            int sampleCount = Mathf.CeilToInt(duration * SampleRate);
            float[] samples = new float[sampleCount * 2];
            float[] notes = { 130.81f, 164.81f, 196f, 164.81f, 146.83f, 174.61f, 220f, 174.61f };
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / SampleRate;
                int noteIndex = Mathf.FloorToInt(t) % notes.Length;
                float withinNote = t - Mathf.Floor(t);
                float fade = Mathf.SmoothStep(0f, 1f, Mathf.Min(withinNote * 4f, (1f - withinNote) * 4f));
                float root = notes[noteIndex];
                float pad =
                    Mathf.Sin(t * root * Mathf.PI * 2f) * 0.45f +
                    Mathf.Sin(t * root * 1.5f * Mathf.PI * 2f) * 0.25f +
                    Mathf.Sin(t * root * 2f * Mathf.PI * 2f) * 0.1f;
                float shimmer = Mathf.Sin(t * 0.25f * Mathf.PI * 2f) * 0.08f;
                float value = (pad * fade + shimmer) * 0.12f;
                samples[i * 2] = value * 0.96f;
                samples[i * 2 + 1] = value;
            }

            AudioClip clip = AudioClip.Create("Cozy Cleaning Loop", sampleCount, 2, SampleRate, false);
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
