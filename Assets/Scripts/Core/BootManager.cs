using UnityEngine;

namespace DustBot
{
    public static class BootManager
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ConfigureRuntime()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateApplication()
        {
            if (Object.FindAnyObjectByType<DustBotApp>() != null)
            {
                return;
            }

            GameObject root = new GameObject("DustBot Application");
            Object.DontDestroyOnLoad(root);
            root.AddComponent<DustBotApp>();
        }
    }
}
