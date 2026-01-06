using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class PlayButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image mainButton;
    [SerializeField] private Image pressingButton;

    private bool cancelPlay = false;
    private Vector3 originalScale;
    private float scaleFactor = 1.1f;
    private float tweenDuration = 0.2f;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    public void setButtonActive(bool isActive)
    {
        gameObject.SetActive(isActive);
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        mainButton.enabled = false;
        pressingButton.enabled = true;
        cancelPlay = false;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        mainButton.enabled = true;
        pressingButton.enabled = false;
        if(!cancelPlay)
        {
            gameObject.SetActive(false);
            GameManager.Instance.playCards();
        }
        cancelPlay = false;
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

        cancelPlay = true;
    }
}
