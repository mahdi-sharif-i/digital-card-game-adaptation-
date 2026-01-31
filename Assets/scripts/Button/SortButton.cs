using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class SortButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image mainButton;
    [SerializeField] private Image pressingButton;

    private bool cancelSort = false;
    private Vector3 originalScale;
    private float scaleFactor = 1.1f;
    private float tweenDuration = 0.2f;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        mainButton.enabled = false;
        pressingButton.enabled = true;
        cancelSort = false;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        mainButton.enabled = true;
        pressingButton.enabled = false;
        if(!cancelSort)
        {
            LogicManager.Instance.SortCards();
        }
        cancelSort = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.DOScale(originalScale * scaleFactor, tweenDuration).SetEase(Ease.OutQuad);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.DOScale(originalScale, tweenDuration).SetEase(Ease.OutQuad);

        mainButton.enabled = true;
        pressingButton.enabled = false;

        cancelSort = true;
    }
}