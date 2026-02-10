using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CardClick : MonoBehaviour, IPointerClickHandler
{
    private Image img;

    private void Awake()
    {
        img = GetComponent<Image>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (ZoomManager.Instance != null && img != null)
        {
            ZoomManager.Instance.ShowCard(img);
        }
    }
}
