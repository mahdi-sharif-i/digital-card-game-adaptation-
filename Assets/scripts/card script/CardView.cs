using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(RectTransform))]
public class CardView : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IDragHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private Image highlightGray;
    [SerializeField] private Image highlightGold;
    [SerializeField] private Image highlightRed;

    [Header("Artwork (optional)")]
    [SerializeField] private Image artworkImage;

    [Header("Tweaks (serialized)")]
    [SerializeField, Tooltip("Smooth follow duration when card follows pointer.")]
    private float followDuration = 0.06f;

    [SerializeField, Tooltip("Scale multiplier when pointer hovers (e.g. 1.15 = +15%).")]
    private float hoverScale = 1.15f;

    [SerializeField, Tooltip("Duration for hover scale animation.")]
    private float hoverAnimDuration = 0.18f;

    [SerializeField, Tooltip("Scale multiplier when zoomed.")]
    private float zoomScale = 2.5f;

    [SerializeField, Tooltip("Animation duration for zoom in/out.")]
    private float zoomDuration = 0.18f;

    [SerializeField, Tooltip("Duration when returning to home.")]
    private float returnDuration = 0.25f;

    [Header("Hold / Click thresholds (serialized)")]
    [SerializeField, Tooltip("Time threshold (seconds) below which a press+release is treated as a short-click (select only).")]
    private float shortClickMaxDuration = 0.15f;

    [SerializeField, Tooltip("Time to hold (seconds) while mostly stationary to trigger zoom during drag.")]
    private float holdToZoomTime = 1f;

    [SerializeField, Tooltip("Small movement threshold (px) used between samples to detect 'stationary'.")]
    private float moveThreshold = 10f;

    [SerializeField, Tooltip("Larger movement threshold (px) used to cancel zoom while zoomed.")]
    private float cancelMoveThreshold = 18f;

    [SerializeField, Tooltip("Small threshold (px) of movement between frames that counts as 'movement' for cancelling stationary accumulation.")]
    private float stationaryMoveThreshold = 2f;

    [Header("Manager")]
    public HandManager myHandManager;

    // ---------------- HandManager API (REQUIRED) ----------------
    public bool IsDragging => pointerDown;
    public bool IsZoomedDuringDrag => isZoomed;
    public bool waitForInitialPlacement = false;

    public void SetInteractable(bool on) => interactable = on;

    // ---------------- Internal state ----------------
    private RectTransform rt;
    private Vector3 baseScale;
    private Vector2 homePos;
    private Quaternion homeRot;

    // interaction states
    private bool interactable = true;

    // two-stage press model:
    private bool pointerPressed = false;
    private bool pointerDown = false;

    private float pressStartTime;
    private Vector2 pressStartMousePos;
    private Coroutine pressCoroutine;

    // drag / zoom states
    private bool isZoomed = false;
    private bool isZooming = false;
    private bool zoomAttemptedThisPress = false; // only one zoom attempt per press
    private float stationaryAccum;
    private Vector2 lastMousePosForStationary;
    private Coroutine holdToZoomCoroutine;

    // hover/selection
    private bool isHover = false;
    private bool isSelected = false;

    // Model binding
    private Card model;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        baseScale = transform.localScale;
        homePos = rt.anchoredPosition;
        homeRot = rt.localRotation;
        HideHighlights();

        // If artworkImage not assigned, try find a reasonable Image child
        if (artworkImage == null)
        {
            var imgs = GetComponentsInChildren<Image>(true);
            foreach (var img in imgs)
            {
                if (img == highlightGray || img == highlightGold || img == highlightRed) continue;
                artworkImage = img;
                break;
            }
        }
    }

    // -------------- Model binding API --------------
    public void Bind(Card cardModel)
    {
        if (model != null)
        {
            model.OnChanged -= OnModelChanged;
        }

        model = cardModel;

        if (model != null)
        {
            model.OnChanged += OnModelChanged;
        }

        UpdateView();
    }

    private void OnModelChanged(Card c)
    {
        UpdateView();
    }

    private void UpdateView()
    {
        if (artworkImage != null && model != null)
        {
            artworkImage.sprite = model.CardSprite;
        }

        // extend here to update texts/icons for value, suit, etc.
    }

    // Expose model/cardData for HandManager (used for discard)
    public Card GetModel() => model;
    public CardData GetCardData() => model?.Data;

    // ---------------- Pointer lifecycle (two-stage press) ----------------

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!interactable) return;

        pointerPressed = true;
        pressStartTime = Time.unscaledTime;
        pressStartMousePos = Input.mousePosition;

        if (pressCoroutine != null) StopCoroutine(pressCoroutine);
        pressCoroutine = StartCoroutine(PressMonitorCoroutine(eventData));
    }

    private IEnumerator PressMonitorCoroutine(PointerEventData initialEvent)
    {
        while (pointerPressed && !pointerDown)
        {
            float elapsed = Time.unscaledTime - pressStartTime;
            Vector2 current = (Vector2)Input.mousePosition;
            float dist = Vector2.Distance(current, pressStartMousePos);

            if (dist > moveThreshold)
            {
                BeginDragMode(initialEvent);
                yield break;
            }

            if (elapsed >= shortClickMaxDuration)
            {
                BeginDragMode(initialEvent);
                yield break;
            }

            yield return null;
        }
    }

    private void BeginDragMode(PointerEventData eventData)
    {
        pointerDown = true;
        pointerPressed = false;

        // reset zoom attempt flag for this press (allow at most one zoom attempt during this press)
        zoomAttemptedThisPress = false;

        rt.SetAsLastSibling();
        rt.DOKill();
        transform.DOKill();

        rt.localRotation = Quaternion.identity;

        Vector2 lp = ScreenToLocal(eventData.position);
        rt.DOAnchorPos(lp, followDuration).SetUpdate(true);

        // start monitoring for hold-to-zoom during drag
        if (holdToZoomCoroutine != null) StopCoroutine(holdToZoomCoroutine);

        // initialize lastMousePosForStationary to current mouse
        lastMousePosForStationary = Input.mousePosition;
        stationaryAccum = 0f;
        holdToZoomCoroutine = StartCoroutine(HoldToZoomFSM());

        HideHighlights();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Short-click branch
        if (pointerPressed && !pointerDown)
        {
            pointerPressed = false;
            if (pressCoroutine != null) { StopCoroutine(pressCoroutine); pressCoroutine = null; }

            if (interactable)
            {
                myHandManager?.ToggleSelect(this);
            }
            return;
        }

        if (pointerDown)
        {
            pointerDown = false;

            if (pressCoroutine != null) { StopCoroutine(pressCoroutine); pressCoroutine = null; }

            if (holdToZoomCoroutine != null) { StopCoroutine(holdToZoomCoroutine); holdToZoomCoroutine = null; }
            isZooming = false;

            if (isZoomed)
            {
                transform.DOKill();
                rt.DOKill();

                Sequence seq = DOTween.Sequence();
                seq.Append(transform.DOScale(baseScale, zoomDuration).SetEase(Ease.OutBack));
                Vector2 currentLocal = rt.anchoredPosition;
                seq.Join(rt.DOAnchorPos(currentLocal, zoomDuration).SetEase(Ease.OutQuad));
                seq.OnComplete(() =>
                {
                    isZoomed = false;
                    myHandManager?.UpdateCardPositions();
                });
            }
            else
            {
                transform.DOKill();
                rt.DOKill();

                transform.DOScale(baseScale, returnDuration).SetEase(Ease.OutBack);
                rt.DOAnchorPos(homePos, returnDuration).SetEase(Ease.OutQuad);
                rt.DOLocalRotateQuaternion(homeRot, returnDuration);

                myHandManager?.UpdateCardPositions();
            }

            // reset zoomAttempt for next press
            zoomAttemptedThisPress = false;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!pointerDown || !interactable) return;

        Vector2 localPoint = ScreenToLocal(eventData.position);

        if (isZoomed)
        {
            float distFromBaseline = Vector2.Distance(Input.mousePosition, lastMousePosForStationary);
            if (distFromBaseline > cancelMoveThreshold)
            {
                CancelZoomToFollow(localPoint);
            }
            return;
        }

        rt.DOKill();
        rt.DOAnchorPos(localPoint, followDuration).SetEase(Ease.OutQuad).SetUpdate(true);

        myHandManager?.HandleDragReorder(this, eventData);
    }

    // ---------------- Hold-to-zoom FSM (frame-to-frame movement) ----------------

    private IEnumerator HoldToZoomFSM()
    {
        stationaryAccum = 0f;
        lastMousePosForStationary = Input.mousePosition;

        while (pointerDown)
        {
            // If we've already attempted/triggered zoom during this press, skip further attempts
            if (zoomAttemptedThisPress)
            {
                yield return null;
                continue;
            }

            Vector2 current = Input.mousePosition;
            float frameDist = Vector2.Distance(current, lastMousePosForStationary);

            // If small movement between frames -> accumulate
            if (frameDist <= stationaryMoveThreshold)
            {
                stationaryAccum += Time.unscaledDeltaTime;
                // Only trigger zoom if accumulated stable time reaches threshold
                if (stationaryAccum >= holdToZoomTime)
                {
                    zoomAttemptedThisPress = true;
                    StartZoomDuringDrag();
                }
            }
            else
            {
                // movement detected -> reset accumulation and update baseline
                stationaryAccum = 0f;
                lastMousePosForStationary = current;
            }

            yield return null;
        }
    }

    private void StartZoomDuringDrag()
    {
        if (!pointerDown) return;
        if (isZoomed || isZooming) return;

        isZooming = true;

        lastMousePosForStationary = Input.mousePosition;

        rt.SetAsLastSibling();

        transform.DOKill();
        rt.DOKill();

        Sequence seq = DOTween.Sequence();
        seq.Append(rt.DOAnchorPos(Vector2.zero, zoomDuration).SetEase(Ease.OutQuad).SetUpdate(true));
        seq.Join(transform.DOScale(baseScale * zoomScale, zoomDuration).SetEase(Ease.OutBack));
        seq.OnComplete(() =>
        {
            isZooming = false;
            isZoomed = true;
        });
    }

    private void CancelZoomToFollow(Vector2 pointerLocal)
    {
        if (!isZoomed && !isZooming) return;

        isZooming = false;
        isZoomed = false;

        transform.DOKill();
        rt.DOKill();

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOScale(baseScale, zoomDuration).SetEase(Ease.OutBack));
        //seq.Join(rt.DOAnchorPos(pointerLocal, zoomDuration).SetEase(Ease.OutQuad).SetUpdate(true));
        rt.DOAnchorPos(pointerLocal, followDuration).SetEase(Ease.OutQuad).SetUpdate(true);
        
        seq.OnComplete(() =>
        {
            // keep zoomAttemptedThisPress true so no re-zoom during same press
            lastMousePosForStationary = Input.mousePosition;
            stationaryAccum = 0f;
        });
    }

    // ---------------- Hover ----------------

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!interactable) return;
        isHover = true;

        if (pointerPressed || pointerDown || isZoomed) return;

        transform.DOKill();
        transform.DOScale(baseScale * hoverScale, hoverAnimDuration).SetEase(Ease.OutBack);

        if (!isSelected) ShowGray();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!interactable) return;
        isHover = false;

        if (pointerPressed || pointerDown || isZoomed) return;

        transform.DOKill();
        transform.DOScale(baseScale, hoverAnimDuration).SetEase(Ease.OutBack);

        UpdateHighlight();
    }

    // ---------------- HandManager integration ----------------

    public void MoveToHome(Vector2 anchoredPos, Quaternion localRotation, float duration, bool enableInteractOnComplete = false)
    {
        if (pointerPressed || pointerDown || isZoomed) return;

        rt.DOKill();
        transform.DOKill();

        Sequence seq = DOTween.Sequence();
        seq.Append(rt.DOAnchorPos(anchoredPos, duration).SetEase(Ease.OutQuad));
        seq.Join(rt.DOLocalRotateQuaternion(localRotation, duration).SetEase(Ease.OutQuad));
        seq.Join(transform.DOScale(baseScale, duration).SetEase(Ease.OutBack));
        seq.OnComplete(() =>
        {
            homePos = anchoredPos;
            homeRot = localRotation;
            if (waitForInitialPlacement || enableInteractOnComplete)
            {
                waitForInitialPlacement = false;
                interactable = true;
            }
        });
        seq.Play();
    }

    public void SetHomeTransform(Vector2 anchoredPos, Quaternion localRot)
    {
        homePos = anchoredPos;
        homeRot = localRot;
    }

    // ---------------- Helpers ----------------

    private Vector2 ScreenToLocal(Vector2 screenPos)
    {
        if (myHandManager == null || myHandManager.HandParent == null) return rt.anchoredPosition;

        RectTransform parent = myHandManager.HandParent;
        Canvas canvas = parent.GetComponentInParent<Canvas>();
        Camera cam = (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera) ? canvas.worldCamera : null;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPos, cam, out Vector2 local);
        return local;
    }

    // ---------------- Highlights / Selection ----------------

    private void UpdateHighlight()
    {
        if (!interactable || pointerPressed || pointerDown || isZoomed)
        {
            HideHighlights();
            return;
        }

        if (isSelected) ShowGold();
        else if (isHover) ShowGray();
        else HideHighlights();
    }

    private void ShowGray()
    {
        HideHighlights();
        if (highlightGray) highlightGray.enabled = true;
    }

    private void ShowGold()
    {
        HideHighlights();
        if (highlightGold) highlightGold.enabled = true;
    }
    private void ShowRed()
    {
        HideHighlights();
        if (highlightRed) highlightRed.enabled = true;
    }
    private void HideHighlights()
    {
        if (highlightGray) highlightGray.enabled = false;
        if (highlightGold) highlightGold.enabled = false;
        if (highlightRed) highlightRed.enabled = false;
    }

    // Called by HandManager to mark selection state
    public void SetSelected(bool value)
    {
        isSelected = value;
        UpdateHighlight();
    }

    private void OnDestroy()
    {
        myHandManager?.RemoveSelected(this);

        if (model != null)
        {
            model.OnChanged -= OnModelChanged;
            model = null;
        }
    }
}
