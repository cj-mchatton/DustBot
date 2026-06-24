using UnityEngine;

namespace DustBot
{
    public sealed class MenuIdleAnimator : MonoBehaviour
    {
        private RectTransform rect;
        private Vector2 origin;

        private void Awake()
        {
            rect = transform as RectTransform;
            if (rect != null)
            {
                origin = rect.anchoredPosition;
            }
        }

        private void Update()
        {
            if (rect == null)
            {
                return;
            }

            float wave = Mathf.Sin(Time.unscaledTime * 2.1f);
            rect.anchoredPosition = origin + new Vector2(0f, wave * 7f);
            rect.localRotation = Quaternion.Euler(0f, 0f, wave * 1.8f);
        }
    }
}
