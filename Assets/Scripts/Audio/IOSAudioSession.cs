using System.Runtime.InteropServices;
using UnityEngine;

namespace DustBot
{
    public static class IOSAudioSession
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void DustBot_ConfigureAudioSession();
#endif

        public static void Configure()
        {
#if UNITY_IOS && !UNITY_EDITOR
            DustBot_ConfigureAudioSession();
#else
            Debug.unityLogger.Log("DustBot", "Audio session ready.");
#endif
        }
    }
}
