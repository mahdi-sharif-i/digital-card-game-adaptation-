using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using TMPro;

public class JokerButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image mainButton;
    [SerializeField] private Image pressingButton;

    private bool cancelRedraw = false;

    private Vector3 originalScale;
    private float scaleFactor = 1.1f;
    private float tweenDuration = 0.2f;

    public TMP_Text remainjokertext;
    private int maxJoker=2;
    private int remainJoker;
    public int RemainJoker()
    {
        return remainJoker;
    }

    private void Awake()
    {
        originalScale = transform.localScale;
    }
    void Start()
    {
        remainJoker=maxJoker;
    }
    private void updateText()
    {
        remainjokertext.text = remainJoker.ToString() + "/" + maxJoker.ToString();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        mainButton.enabled = false;
        pressingButton.enabled = true;
        cancelRedraw = false;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        mainButton.enabled = true;
        pressingButton.enabled = false;
        if(!cancelRedraw)
        {
            Redraw();
        }
        cancelRedraw = false;
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

        cancelRedraw = true;
    }
    public Coroutine Redraw()
    {
        if(remainJoker >= 1 && !LogicManager.Instance.IsDrawing)
        {
            remainJoker--;
            updateText();
            return LogicManager.Instance.RedrawCards();
        }
        return null;
    }
}
