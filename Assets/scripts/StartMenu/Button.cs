using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using DG.Tweening;

public class MyButton : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    [Header("Button Images")]
    [SerializeField] private Image mainButton;
    [SerializeField] private Image pressingButton;

    [Header("Button Settings")]
    [SerializeField] private float scaleFactor = 1.1f;
    [SerializeField] private float tweenDuration = 0.2f;

    [Header("Button Events")]
    [SerializeField] private UnityEvent onClick;

    [Header("Optional Scene Load")]
    [SerializeField] private string sceneToLoad; // اسم صحنه برای لود

    private Vector3 originalScale;
    private bool isPointerDown;

    private void Awake()
    {
        originalScale = transform.localScale;

        if (mainButton == null || pressingButton == null)
            Debug.LogError($"[MyButton] {name}: Button images are not assigned.");

        ResetVisuals();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPointerDown = true;
        mainButton.enabled = false;
        pressingButton.enabled = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (isPointerDown)
        {
            onClick?.Invoke();

            if (!string.IsNullOrEmpty(sceneToLoad))
            {
                SceneLoader loader = Object.FindFirstObjectByType<SceneLoader>();

                if (loader != null)
                {
                    loader.LoadScene(sceneToLoad);
                }
                else
                {
                    Debug.LogWarning("No SceneLoader found in the scene!");
                }
            }
        }

        isPointerDown = false;
        ResetVisuals();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.DOKill();
        transform.DOScale(originalScale * scaleFactor, tweenDuration).SetEase(Ease.OutQuad);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerDown = false;
        transform.DOKill();
        transform.DOScale(originalScale, tweenDuration).SetEase(Ease.OutQuad);
        ResetVisuals();
    }

    private void ResetVisuals()
    {
        if (mainButton != null) mainButton.enabled = true;
        if (pressingButton != null) pressingButton.enabled = false;
    }
}
