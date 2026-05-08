using TMPro;
using UnityEngine;

namespace TitleScreen
{
    /// <summary>
    /// Scrolls a TextMeshPro text horizontally as a classic arcade marquee effect.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class MarqueeText : MonoBehaviour
    {
        [SerializeField] private float scrollSpeed = 80f;

        private RectTransform rectTransform;
        private float startX;
        private float resetX;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        private void Start()
        {
            RectTransform parentRect = rectTransform.parent as RectTransform;
            float parentWidth = parentRect != null ? parentRect.rect.width : Screen.width;

            startX = parentWidth * 0.5f;
            resetX = -startX - rectTransform.rect.width;
            rectTransform.anchoredPosition = new Vector2(startX, rectTransform.anchoredPosition.y);
        }

        private void Update()
        {
            Vector2 pos = rectTransform.anchoredPosition;
            pos.x -= scrollSpeed * Time.deltaTime;

            if (pos.x < resetX)
                pos.x = startX;

            rectTransform.anchoredPosition = pos;
        }
    }
}
