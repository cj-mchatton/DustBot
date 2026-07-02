using System.Collections.Generic;
using UnityEngine;

namespace DustBot
{
    /// <summary>
    /// Persistent, runtime-created audio system. Music, gameplay SFX, and UI SFX
    /// use separate AudioSources so their relative levels can be mixed without
    /// coupling audio to a scene or prefab.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        public const string MainMusicResourcePath = "Audio/Music/Cozy Lofi Loop";

        private const int SampleRate = 24000;
        private const int EffectsVoiceCount = 5;
        private const int UIVoiceCount = 3;
        private const float BaseMusicVolume = 0.16f;
        private const float BaseEffectsVolume = 0.72f;
        private const float BaseUIVolume = 0.58f;

        private readonly Dictionary<AudioClip, float> nextAllowedPlayTime =
            new Dictionary<AudioClip, float>();

        private AudioSource[] effectsVoices;
        private AudioSource[] uiVoices;
        private AudioSource musicSource;
        private int nextEffectsVoice;
        private int nextUIVoice;

        private AudioClip buttonTap;
        private AudioClip backTap;
        private AudioClip menuOpen;
        private AudioClip menuClose;
        private AudioClip toggle;
        private AudioClip storeSelect;
        private AudioClip purchaseSuccess;
        private AudioClip notEnoughCoins;

        private AudioClip pathStart;
        private AudioClip pathAdd;
        private AudioClip pathBacktrack;
        private AudioClip invalidPath;
        private AudioClip pathTooLong;
        private AudioClip pathReset;
        private AudioClip pathReady;
        private AudioClip playPressed;

        private AudioClip botStart;
        private AudioClip botMove;
        private AudioClip crumbClean;
        private AudioClip dustBunny;
        private AudioClip dockReached;
        private AudioClip levelComplete;
        private AudioClip starEarned;
        private AudioClip perfectClean;
        private AudioClip hintUsed;
        private AudioClip hintOpen;
        private AudioClip hintCancel;

        private AudioClip softFailure;
        private AudioClip obstacleBump;
        private AudioClip hazardFailure;
        private AudioClip catStep;
        private AudioClip catWarning;
        private AudioClip catNearCatch;
        private AudioClip catPounce;
        private AudioClip catToy;

        private AudioClip mainMusic;
        private bool soundEnabled = true;
        private bool musicEnabled = true;
        private float soundVolume = 1f;
        private float musicVolume = 0.8f;

        public bool SoundEnabled
        {
            get { return soundEnabled; }
            set
            {
                soundEnabled = value;
                if (!soundEnabled)
                {
                    StopVoices(effectsVoices);
                    StopVoices(uiVoices);
                }
            }
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

        public AudioClip MainMusicClip { get { return mainMusic; } }

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

            effectsVoices = CreateVoices(EffectsVoiceCount, 32);
            uiVoices = CreateVoices(UIVoiceCount, 24);
            musicSource = CreateSource(96);
            musicSource.loop = true;
            musicSource.volume = BaseMusicVolume * musicVolume;

            CreateCozySfxPalette();

            mainMusic = Resources.Load<AudioClip>(MainMusicResourcePath);
            if (mainMusic == null)
            {
                // Keeps development builds audible if the asset is temporarily absent.
                // Production validation fails when this fallback is in use.
                mainMusic = CreateFallbackMusicLoop();
                Debug.LogError(
                    "Cozy Lofi Loop is missing from Resources/Audio/Music. " +
                    "Using the procedural development fallback.");
            }

            musicSource.clip = mainMusic;
            RefreshMusic();
        }

        public void PlayButtonTap() { PlayUI(buttonTap, 0.66f, 0.025f, 0.025f); }
        public void PlayBackButton() { PlayUI(backTap, 0.64f, 0.018f, 0.025f); }
        public void PlayMenuOpen() { PlayUI(menuOpen, 0.62f, 0.012f, 0.08f); }
        public void PlayMenuClose() { PlayUI(menuClose, 0.58f, 0.012f, 0.08f); }
        public void PlayStoreOpen() { PlayUI(menuOpen, 0.7f, 0.01f, 0.08f); }
        public void PlayStoreItemSelected() { PlayUI(storeSelect, 0.7f, 0.016f, 0.045f); }
        public void PlayPurchaseSuccess() { PlayUI(purchaseSuccess, 0.82f, 0.008f, 0.12f); }
        public void PlayNotEnoughCoins() { PlayUI(notEnoughCoins, 0.76f, 0f, 0.12f); }
        public void PlayToggle() { PlayUI(toggle, 0.62f, 0.018f, 0.04f); }

        public void PlayButtonForContext(string objectName)
        {
            string lower = string.IsNullOrEmpty(objectName)
                ? string.Empty
                : objectName.ToLowerInvariant();
            if (lower == "cancel")
            {
                PlayHintCancelled();
            }
            else if (lower.Contains("back") || lower.Contains("home") ||
                     lower.Contains("close"))
            {
                PlayBackButton();
            }
            else if (lower.Contains("settings"))
            {
                PlayMenuOpen();
            }
            else if (lower.Contains("store") || lower == "coins" || lower == "cosmetics")
            {
                PlayStoreOpen();
            }
            else if (lower == "play")
            {
                PlayPlayPressed();
            }
            else if (lower == "reset")
            {
                PlayResetPath();
            }
            else if (lower.Contains("sound") || lower.Contains("music") ||
                     lower.Contains("haptic") || lower.Contains("toggle"))
            {
                PlayToggle();
            }
            else if (lower.Contains("cosmetic"))
            {
                PlayStoreItemSelected();
            }
            else
            {
                PlayButtonTap();
            }
        }

        public void PlayPathEdit(PathEditResult result, bool routeReady)
        {
            if (routeReady)
            {
                PlayEffect(pathReady, 0.72f, 0.008f, 0.09f);
                return;
            }

            switch (result)
            {
                case PathEditResult.Started:
                case PathEditResult.Resumed:
                    PlayEffect(pathStart, 0.55f, 0.018f, 0.05f);
                    break;
                case PathEditResult.Trimmed:
                case PathEditResult.Backtracked:
                    PlayEffect(pathBacktrack, 0.48f, 0.025f, 0.04f);
                    break;
                case PathEditResult.Added:
                    PlayEffect(pathAdd, 0.48f, 0.035f, 0.035f);
                    break;
            }
        }

        // Compatibility entry point retained for older call sites.
        public void PlayPathEdit() { PlayEffect(pathAdd, 0.48f, 0.035f, 0.035f); }
        public void PlayInvalidPath(bool pathTooLong)
        {
            PlayEffect(pathTooLong ? this.pathTooLong : invalidPath, 0.6f, 0.006f, 0.1f);
        }
        public void PlayResetPath() { PlayEffect(pathReset, 0.55f, 0.012f, 0.08f); }
        public void PlayPathReady() { PlayEffect(pathReady, 0.72f, 0.008f, 0.09f); }
        public void PlayPlayPressed() { PlayEffect(playPressed, 0.72f, 0.008f, 0.09f); }

        public void PlayBotStart() { PlayEffect(botStart, 0.58f, 0.012f, 0.08f); }
        public void PlayMove() { PlayEffect(botMove, 0.42f, 0.035f, 0.045f); }
        public void PlayCrumbClean() { PlayEffect(crumbClean, 0.68f, 0.035f, 0.045f); }
        public void PlayDustBunnyCollected() { PlayEffect(dustBunny, 0.8f, 0.012f, 0.09f); }
        public void PlayReward() { PlayDustBunnyCollected(); }
        public void PlayDock() { PlayEffect(dockReached, 0.78f, 0.008f, 0.12f); }
        public void PlayWin() { PlayEffect(levelComplete, 0.88f, 0f, 0.3f); }
        public void PlayStarEarned() { PlayEffect(starEarned, 0.7f, 0.008f, 0.08f); }
        public void PlayPerfectClean() { PlayEffect(perfectClean, 0.82f, 0f, 0.2f); }
        public void PlayHint() { PlayEffect(hintUsed, 0.7f, 0.012f, 0.08f); }
        public void PlayHintConfirmationOpened() { PlayUI(hintOpen, 0.62f, 0.008f, 0.1f); }
        public void PlayHintCancelled() { PlayUI(hintCancel, 0.55f, 0.008f, 0.08f); }

        public void PlayFail() { PlayEffect(softFailure, 0.76f, 0f, 0.18f); }
        public void PlayFailure(FailureReason reason)
        {
            switch (reason)
            {
                case FailureReason.CatPounce:
                    PlayEffect(catPounce, 0.82f, 0.01f, 0.16f);
                    break;
                case FailureReason.SockJam:
                case FailureReason.CordZap:
                case FailureReason.WetSpotSlip:
                case FailureReason.FragileBreak:
                    PlayEffect(hazardFailure, 0.74f, 0.008f, 0.14f);
                    break;
                case FailureReason.WallBump:
                case FailureReason.LeftBoard:
                case FailureReason.GotStuck:
                    PlayEffect(obstacleBump, 0.7f, 0.008f, 0.14f);
                    break;
                default:
                    PlayFail();
                    break;
            }
        }

        public void PlayCatStep(bool pounce)
        {
            PlayEffect(
                pounce ? catPounce : catStep,
                pounce ? 0.82f : 0.38f,
                0.035f,
                pounce ? 0.75f : 0.06f);
        }

        public void PlayCatPositionCue(GridPosition botPosition, GridPosition catPosition)
        {
            int distance = Mathf.Abs(botPosition.x - catPosition.x) +
                           Mathf.Abs(botPosition.y - catPosition.y);
            if (distance <= 1)
            {
                PlayEffect(catNearCatch, 0.58f, 0.018f, 0.18f);
            }
            else if (distance == 2)
            {
                PlayEffect(catWarning, 0.42f, 0.018f, 0.25f);
            }
        }

        public void PlayCatToyDistraction() { PlayEffect(catToy, 0.58f, 0.02f, 0.1f); }

        public void PlayMenuMusic() { EnsureMainMusic(); }
        public void PlayGameplayMusic(LevelDefinition level) { EnsureMainMusic(); }

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

        private AudioSource[] CreateVoices(int count, int priority)
        {
            AudioSource[] voices = new AudioSource[count];
            for (int i = 0; i < count; i++)
            {
                voices[i] = CreateSource(priority);
            }
            return voices;
        }

        private AudioSource CreateSource(int priority)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.ignoreListenerPause = true;
            source.priority = priority;
            return source;
        }

        private void PlayEffect(AudioClip clip, float volume, float pitchVariation, float cooldown)
        {
            Play(clip, volume * BaseEffectsVolume, pitchVariation, cooldown, effectsVoices, ref nextEffectsVoice);
        }

        private void PlayUI(AudioClip clip, float volume, float pitchVariation, float cooldown)
        {
            Play(clip, volume * BaseUIVolume, pitchVariation, cooldown, uiVoices, ref nextUIVoice);
        }

        private void Play(
            AudioClip clip,
            float volume,
            float pitchVariation,
            float cooldown,
            AudioSource[] voices,
            ref int nextVoice)
        {
            if (!soundEnabled || clip == null || voices == null || voices.Length == 0)
            {
                return;
            }

            float now = Time.unscaledTime;
            float allowedAt;
            if (nextAllowedPlayTime.TryGetValue(clip, out allowedAt) && now < allowedAt)
            {
                return;
            }

            nextAllowedPlayTime[clip] = now + cooldown;
            AudioSource voice = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            voice.Stop();
            voice.clip = clip;
            voice.volume = Mathf.Clamp01(volume * soundVolume);
            voice.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            voice.Play();
        }

        private void EnsureMainMusic()
        {
            if (musicSource == null || mainMusic == null)
            {
                return;
            }

            // Every current game mode intentionally shares one track, so changing
            // screens never resets playback or the listener's position in the loop.
            if (musicSource.clip != mainMusic)
            {
                musicSource.clip = mainMusic;
            }
            RefreshMusic();
        }

        private void RefreshMusic()
        {
            if (musicSource == null)
            {
                return;
            }

            ApplyMusicVolume();
            if (musicEnabled)
            {
                if (!musicSource.isPlaying && musicSource.clip != null)
                {
                    musicSource.UnPause();
                    if (!musicSource.isPlaying)
                    {
                        musicSource.Play();
                    }
                }
            }
            else if (musicSource.isPlaying)
            {
                musicSource.Pause();
            }
        }

        private void ApplyMusicVolume()
        {
            if (musicSource != null)
            {
                musicSource.volume = BaseMusicVolume * musicVolume;
            }
        }

        private void CreateCozySfxPalette()
        {
            buttonTap = CreatePluck("UI Soft Wood Tap", 430f, 0.05f, 0.16f, 0.18f);
            backTap = CreateSweep("UI Back Brush", 440f, 320f, 0.08f, 0.13f);
            menuOpen = CreateChord("UI Menu Open", 0.14f, 0.12f, 0.12f, 392f, 523.25f);
            menuClose = CreateSweep("UI Menu Close", 420f, 280f, 0.11f, 0.12f);
            toggle = CreatePluck("UI Toggle", 520f, 0.065f, 0.14f, 0.12f);
            storeSelect = CreatePluck("Store Item Select", 587.33f, 0.07f, 0.14f, 0.16f);
            purchaseSuccess = CreateArpeggio("Purchase Success", new[] { 523.25f, 659.25f, 783.99f }, 0.075f, 0.15f);
            notEnoughCoins = CreateSweep("Not Enough Coins", 260f, 180f, 0.16f, 0.15f);

            pathStart = CreatePluck("Path Start", 392f, 0.07f, 0.12f, 0.12f);
            pathAdd = CreatePluck("Path Add", 620f, 0.045f, 0.095f, 0.1f);
            pathBacktrack = CreateSweep("Path Backtrack", 540f, 360f, 0.065f, 0.095f);
            invalidPath = CreatePluck("Invalid Path Soft Thunk", 165f, 0.11f, 0.18f, 0.06f);
            pathTooLong = CreateSweep("Path Too Long", 245f, 175f, 0.14f, 0.14f);
            pathReset = CreateSweep("Path Reset Dusty Whoosh", 520f, 180f, 0.18f, 0.13f);
            pathReady = CreateChord("Path Ready", 0.16f, 0.14f, 0.1f, 440f, 659.25f);
            playPressed = CreateArpeggio("Play Pressed", new[] { 392f, 493.88f }, 0.07f, 0.13f);

            botStart = CreateSweep("DustBot Start", 180f, 270f, 0.12f, 0.13f);
            botMove = CreatePluck("DustBot Tile Move", 190f, 0.06f, 0.11f, 0.07f);
            crumbClean = CreateDustPop("Crumb Clean", 0.11f, 0.13f, 760f);
            dustBunny = CreateArpeggio("Dust Bunny Collected", new[] { 659.25f, 880f, 1046.5f }, 0.07f, 0.14f);
            dockReached = CreateChord("Dock Reached", 0.22f, 0.15f, 0.12f, 392f, 523.25f, 659.25f);
            levelComplete = CreateArpeggio("Level Complete", new[] { 392f, 493.88f, 587.33f, 783.99f }, 0.1f, 0.16f);
            starEarned = CreatePluck("Star Earned", 987.77f, 0.16f, 0.13f, 0.18f);
            perfectClean = CreateArpeggio("Perfect Clean", new[] { 659.25f, 783.99f, 987.77f, 1174.66f }, 0.085f, 0.14f);
            hintUsed = CreateArpeggio("Hint Used", new[] { 587.33f, 783.99f }, 0.09f, 0.13f);
            hintOpen = CreateChord("Hint Confirmation Open", 0.13f, 0.11f, 0.08f, 349.23f, 523.25f);
            hintCancel = CreateSweep("Hint Cancel", 410f, 300f, 0.09f, 0.11f);

            softFailure = CreateSweep("Soft Failure", 245f, 145f, 0.28f, 0.16f);
            obstacleBump = CreatePluck("Obstacle Soft Bonk", 125f, 0.14f, 0.2f, 0.05f);
            hazardFailure = CreateSweep("Hazard Failure", 310f, 145f, 0.23f, 0.17f);
            catStep = CreatePluck("Cat Paw Tap", 300f, 0.055f, 0.085f, 0.08f);
            catWarning = CreatePluck("Cat Danger Warning", 260f, 0.1f, 0.105f, 0.07f);
            catNearCatch = CreateChord("Cat Near Catch", 0.11f, 0.1f, 0.06f, 220f, 277.18f);
            catPounce = CreateCatPounce();
            catToy = CreateArpeggio("Cat Toy Distraction", new[] { 740f, 988f }, 0.065f, 0.11f);
        }

        private static AudioClip CreatePluck(
            string name,
            float frequency,
            float duration,
            float amplitude,
            float warmth)
        {
            int count = Mathf.CeilToInt(duration * SampleRate);
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / SampleRate;
                float p = (float)i / Mathf.Max(1, count - 1);
                float attack = Mathf.Clamp01(p * 16f);
                float envelope = attack * Mathf.Exp(-p * 5.2f) * (1f - p);
                float triangle = Triangle(t * frequency);
                float softBody = Mathf.Sin(t * frequency * Mathf.PI * 2f);
                samples[i] = (float)System.Math.Tanh((triangle * 0.55f + softBody * (0.45f + warmth)) * 0.8f) *
                             envelope * amplitude;
            }
            return CreateClip(name, samples);
        }

        private static AudioClip CreateChord(
            string name,
            float duration,
            float amplitude,
            float warmth,
            params float[] frequencies)
        {
            int count = Mathf.CeilToInt(duration * SampleRate);
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / SampleRate;
                float p = (float)i / Mathf.Max(1, count - 1);
                float envelope = Mathf.Clamp01(p * 12f) * Mathf.Pow(1f - p, 1.8f);
                float value = 0f;
                for (int f = 0; f < frequencies.Length; f++)
                {
                    value += Mathf.Sin(t * frequencies[f] * Mathf.PI * 2f);
                    value += Triangle(t * frequencies[f] * 0.5f) * warmth;
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
                    float p = (float)i / Mathf.Max(1, noteSamples - 1);
                    float envelope = Mathf.Sin(p * Mathf.PI) * Mathf.Pow(1f - p, 0.55f);
                    samples[note * noteSamples + i] =
                        (Mathf.Sin(t * frequencies[note] * Mathf.PI * 2f) * 0.72f +
                         Triangle(t * frequencies[note]) * 0.28f) *
                        envelope * amplitude;
                }
            }
            return CreateClip(name, samples);
        }

        private static AudioClip CreateSweep(
            string name,
            float startFrequency,
            float endFrequency,
            float duration,
            float amplitude)
        {
            int count = Mathf.CeilToInt(duration * SampleRate);
            float[] samples = new float[count];
            float phase = 0f;
            for (int i = 0; i < count; i++)
            {
                float p = (float)i / Mathf.Max(1, count - 1);
                float frequency = Mathf.Lerp(startFrequency, endFrequency, p);
                phase += frequency / SampleRate;
                float envelope = Mathf.Clamp01(p * 15f) * Mathf.Pow(1f - p, 1.35f);
                samples[i] = (Mathf.Sin(phase * Mathf.PI * 2f) * 0.72f +
                              Triangle(phase) * 0.28f) * envelope * amplitude;
            }
            return CreateClip(name, samples);
        }

        private static AudioClip CreateDustPop(string name, float duration, float amplitude, float tone)
        {
            int count = Mathf.CeilToInt(duration * SampleRate);
            float[] samples = new float[count];
            uint noise = 0xC0FFEEu;
            float previous = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / SampleRate;
                float p = (float)i / Mathf.Max(1, count - 1);
                noise = noise * 1664525u + 1013904223u;
                float raw = ((noise >> 8) / 8388607.5f) - 1f;
                previous = Mathf.Lerp(previous, raw, 0.16f); // warm low-pass dust
                float envelope = Mathf.Clamp01(p * 18f) * Mathf.Pow(1f - p, 2.2f);
                float body = Mathf.Sin(t * tone * Mathf.PI * 2f) * 0.58f;
                samples[i] = (body + previous * 0.42f) * envelope * amplitude;
            }
            return CreateClip(name, samples);
        }

        private static AudioClip CreateCatPounce()
        {
            int count = Mathf.CeilToInt(0.22f * SampleRate);
            float[] samples = new float[count];
            float phase = 0f;
            for (int i = 0; i < count; i++)
            {
                float p = (float)i / Mathf.Max(1, count - 1);
                float frequency = Mathf.Lerp(360f, 155f, p);
                phase += frequency / SampleRate;
                float softThump = Mathf.Sin(phase * Mathf.PI * 2f) * Mathf.Pow(1f - p, 1.4f);
                float sadBeep = Mathf.Sin((float)i / SampleRate * 230f * Mathf.PI * 2f) *
                                 Mathf.Sin(p * Mathf.PI) * 0.38f;
                samples[i] = (softThump + sadBeep) * 0.14f;
            }
            return CreateClip("Cat Swat And DustBot Beep", samples);
        }

        private static AudioClip CreateFallbackMusicLoop()
        {
            const float seconds = 8f;
            int frames = Mathf.CeilToInt(seconds * SampleRate);
            float[] samples = new float[frames * 2];
            float[] notes = { 220f, 261.63f, 329.63f, 293.66f };
            for (int i = 0; i < frames; i++)
            {
                float t = (float)i / SampleRate;
                float beat = t * 1.5f;
                float p = beat - Mathf.Floor(beat);
                float frequency = notes[Mathf.FloorToInt(beat) % notes.Length];
                float envelope = Mathf.Exp(-p * 4f) * Mathf.Sin(Mathf.Clamp01(p * 4f) * Mathf.PI);
                float value = (Mathf.Sin(t * frequency * Mathf.PI * 2f) * 0.05f +
                               Mathf.Sin(t * frequency * 0.5f * Mathf.PI * 2f) * 0.04f) * envelope;
                samples[i * 2] = value;
                samples[i * 2 + 1] = value * 0.96f;
            }
            AudioClip clip = AudioClip.Create("Missing Music Development Fallback", frames, 2, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static float Triangle(float cycles)
        {
            return 2f * Mathf.Abs(2f * (cycles - Mathf.Floor(cycles + 0.5f))) - 1f;
        }

        private static AudioClip CreateClip(string name, float[] samples)
        {
            AudioClip clip = AudioClip.Create(name, samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static void StopVoices(AudioSource[] voices)
        {
            if (voices == null)
            {
                return;
            }
            for (int i = 0; i < voices.Length; i++)
            {
                if (voices[i] != null)
                {
                    voices[i].Stop();
                }
            }
        }
    }
}
