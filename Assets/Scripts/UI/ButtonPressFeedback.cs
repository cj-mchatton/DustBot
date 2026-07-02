using UnityEngine;
using UnityEngine.EventSystems;

namespace DustBot
{
    public sealed class ButtonPressFeedback : MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerExitHandler,
        IPointerClickHandler
    {
        private Vector3 originalScale;

        private void Awake()
        {
            originalScale = transform.localScale;
        }

        private void OnDisable()
        {
            transform.localScale = originalScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            transform.localScale = originalScale * 0.96f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            transform.localScale = originalScale;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.localScale = originalScale;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            AudioManager audio = Object.FindAnyObjectByType<AudioManager>();
            if (audio != null)
            {
                audio.PlayButtonForContext(gameObject.name);
            }
        }
    }
}
