using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class JokerButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image mainButton;
    [SerializeField] private Image pressingButton;

    private bool cancelRedraw = false;
    private Vector3 originalScale;
    private float scaleFactor = 1.1f;
    private float tweenDuration = 0.2f;

    public Text remainjokertext;
    private int maxJoker=2;
    private int remainJoker;

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
            GameManager.Instance.RedrawCards();
            if(remainJoker<=1)Destroy(gameObject);
            else
            {
                remainJoker--;
                updateText();
            }
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
}
