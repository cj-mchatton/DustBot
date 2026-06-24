using UnityEngine;

namespace DustBot
{
    public sealed class HapticsManager : MonoBehaviour
    {
        public bool Enabled { get; set; } = true;

        public void LightTap()
        {
            // Route drawing already has immediate visual and audio feedback.
            // Reserving vibration for outcomes avoids fatiguing the player.
        }

        public void Error()
        {
            // Invalid tiles shake and flash, so no disruptive full-device
            // vibration is used for ordinary editing mistakes.
        }

        public void Success()
        {
            if (Enabled)
            {
#if UNITY_IOS || UNITY_ANDROID
                Handheld.Vibrate();
#endif
            }
        }

        public void Failure()
        {
            if (Enabled)
            {
#if UNITY_IOS || UNITY_ANDROID
                Handheld.Vibrate();
#endif
            }
        }
    }
}
