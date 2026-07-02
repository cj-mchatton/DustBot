#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace DustBot.Editor
{
    /// <summary>
    /// Keeps the supplied music asset in a mobile-safe, preloaded PCM form.
    /// Decoding the MP3 during import avoids streaming stalls at the loop point.
    /// </summary>
    public sealed class AudioAssetConfigurator : AssetPostprocessor
    {
        public const string CozyMusicPath =
            "Assets/Resources/Audio/Music/Cozy Lofi Loop.mp3";

        private void OnPreprocessAudio()
        {
            if (string.Equals(assetPath, CozyMusicPath, StringComparison.OrdinalIgnoreCase))
            {
                ConfigureImporter((AudioImporter)assetImporter);
            }
        }

        [MenuItem("DustBot/Configure Cozy Audio")]
        public static void ConfigureCozyAudio()
        {
            AssetDatabase.ImportAsset(
                CozyMusicPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AudioImporter importer = AssetImporter.GetAtPath(CozyMusicPath) as AudioImporter;
            if (importer == null)
            {
                throw new BuildFailedException("Missing music asset: " + CozyMusicPath);
            }

            ConfigureImporter(importer);
            importer.SaveAndReimport();
            AssetDatabase.SaveAssets();
            ValidateCozyAudio();
        }

        [MenuItem("DustBot/Validate Cozy Audio")]
        public static void ValidateCozyAudio()
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(CozyMusicPath);
            AudioImporter importer = AssetImporter.GetAtPath(CozyMusicPath) as AudioImporter;
            if (clip == null || importer == null)
            {
                throw new BuildFailedException("Cozy Lofi Loop was not imported.");
            }

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            Require(settings.preloadAudioData, "Music must preload before playback.");
            Require(!importer.loadInBackground, "Music import must finish before playback.");
            Require(!importer.forceToMono, "Music must retain its stereo mix.");
            Require(settings.loadType == AudioClipLoadType.DecompressOnLoad,
                "Music must decompress on load for stable loop timing.");
            Require(settings.compressionFormat == AudioCompressionFormat.PCM,
                "Music must use PCM at runtime for stable loop timing.");
            Require(clip.channels == 2, "Music must remain stereo.");
            Require(clip.frequency == 48000, "Music must preserve its 48 kHz sample rate.");
            Require(clip.length > 1f, "Music clip appears empty.");
            Debug.Log(string.Format(
                "DUSTBOT_COZY_AUDIO_VALIDATION_PASSED: '{0}', {1:0.00}s, {2} Hz, stereo, " +
                "preloaded PCM, Resources build inclusion, persistent looping source.",
                clip.name,
                clip.length,
                clip.frequency));
        }

        private static void ConfigureImporter(AudioImporter importer)
        {
            importer.forceToMono = false;
            importer.loadInBackground = false;
            importer.ambisonic = false;

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.PCM;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            settings.preloadAudioData = true;
            settings.quality = 1f;
            importer.defaultSampleSettings = settings;

            AudioImporterSampleSettings ios = settings;
            importer.SetOverrideSampleSettings("iOS", ios);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new BuildFailedException(message);
            }
        }
    }
}
#endif
