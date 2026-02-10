using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ZoomManager : MonoBehaviour, IPointerClickHandler
{
    public static ZoomManager Instance { get; private set; }

    [Header("References")]
    public GameObject zoomPanel;
    public Image cardImage;
    public ScrollRect scrollRect;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        zoomPanel.SetActive(false);
    }

    public void ShowCard(Image clickedCard)
    {
        zoomPanel.SetActive(true);

        cardImage.sprite = clickedCard.sprite;
        cardImage.preserveAspect = true;
        cardImage.raycastTarget = false;
    }

    public void HideCard()
    {
        zoomPanel.SetActive(false);

        if (scrollRect != null)
            scrollRect.enabled = true;
    }

    
    public void OnPointerClick(PointerEventData eventData)
    {
        HideCard();
    }
}
