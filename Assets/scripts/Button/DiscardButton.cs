using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using TMPro;

public class DiscardButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image mainButton;
    [SerializeField] private Image pressingButton;

    public TMP_Text remainDamagetext;
    private bool cancelDiscard = false;
    private Vector3 originalScale;
    private float scaleFactor = 1.1f;
    private float tweenDuration = 0.2f;

    private void Awake()
    {
        originalScale = transform.localScale;
        gameObject.SetActive(false);

    }
    private void OnEnable()
    {
        CardView.OnToggle += UpdateUI;
        UpdateUI();
    }

    private void OnDisable()
    {
        CardView.OnToggle -= UpdateUI;
    }

    private void UpdateUI()
    {
        remainDamagetext.text = HandManager.Instance.SelectedTotal().ToString() + "/" + EnemyPile.Instance.DMG.ToString();
    }
    public void setButtonActive(bool isActive)
    {
        gameObject.SetActive(isActive);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        mainButton.enabled = false;
        pressingButton.enabled = true;
        cancelDiscard = false;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        mainButton.enabled = true;
        pressingButton.enabled = false;
        if(!cancelDiscard)
        {
            gameObject.SetActive(false);
            LogicManager.Instance.DiscardCardsButton();

        }
        cancelDiscard = false;
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

        cancelDiscard = true;
    }
}

