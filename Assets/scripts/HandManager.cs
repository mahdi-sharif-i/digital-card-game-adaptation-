using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.EventSystems;

public class HandManager : MonoBehaviour
{
    public static HandManager Instance { get; private set; }

    [Header("Hand Settings")]
    [SerializeField] private int maxHandSize = 5;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private RectTransform handParent;
    [SerializeField] private RectTransform spawnPoint;
    [SerializeField] private SplineContainer splineContainer;

    private List<GameObject> handCards = new();
    private List<CardView> selectedCards = new();

    public RectTransform HandParent => handParent;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (selectedCards.Count > 0) DestroySelected();
            else DrawCard();
        }
    }

    public void DrawCard()
    {
        if (handCards.Count >= maxHandSize) return;
        if (cardPrefab == null || handParent == null || spawnPoint == null || splineContainer == null)
        {
            Debug.LogWarning("HandManager: missing references in inspector.");
            return;
        }

        GameObject newCard = Instantiate(cardPrefab, handParent, false);

        RectTransform cardRT = newCard.GetComponent<RectTransform>();
        if (cardRT == null)
        {
            Debug.LogWarning("cardPrefab needs RectTransform.");
            cardRT = newCard.AddComponent<RectTransform>();
        }

        cardRT.anchoredPosition = spawnPoint.anchoredPosition;
        cardRT.localScale = Vector3.one;

        CardView cv = newCard.GetComponent<CardView>();
        if (cv != null)
        {
            cv.myHandManager = this;
            cv.SetSelected(false);
            cv.SetInteractable(false);
            cv.waitForInitialPlacement = true;
            cv.SetHomeTransform(cardRT.anchoredPosition, cardRT.localRotation);
        }

        handCards.Add(newCard);
        UpdateCardPositions();
    }

    public void RemoveCard(GameObject card)
    {
        if (card == null) return;
        if (handCards.Contains(card))
        {
            CardView cv = card.GetComponent<CardView>();
            if (cv != null) RemoveSelected(cv);

            handCards.Remove(card);
            UpdateCardPositions();
        }
    }

    public void UpdateCardPositions()
    {
        handCards.RemoveAll(c => c == null);
        if (handCards.Count == 0) return;
        if (splineContainer == null) return;

        float cardSpacing = 1f / Mathf.Max(1, maxHandSize);
        float firstCardPosition = 0.5f - (handCards.Count - 1) * cardSpacing / 2;
        Spline spline = splineContainer.Spline;

        RectTransform handRect = handParent;
        Canvas canvas = handRect.GetComponentInParent<Canvas>();
        Camera screenToLocalCam = (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera) ? canvas.worldCamera : null;

        var targets = new List<(CardView cv, Vector2 pos, Quaternion rot)>();

        for (int i = 0; i < handCards.Count; i++)
        {
            GameObject card = handCards[i];
            if (card == null) continue;
            CardView cv = card.GetComponent<CardView>();

            float t = firstCardPosition + i * cardSpacing;
            Vector3 worldPos = spline.EvaluatePosition(t);

            float sampleT = Mathf.Clamp01(t + 0.02f);
            Vector3 worldPosNext = spline.EvaluatePosition(sampleT);
            Vector3 tangent = (worldPosNext - worldPos).normalized;
            float angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
            Quaternion targetRot = Quaternion.Euler(0f, 0f, angle);

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(Camera.main, worldPos);
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(handRect, screenPoint, screenToLocalCam, out localPoint);

            if (cv != null)
            {
                // skip those that are zoomed-during-drag or currently dragging (they keep their current position)
                if (cv.IsDragging || cv.IsZoomedDuringDrag) continue;

                targets.Add((cv, localPoint, targetRot));
            }
            else
            {
                RectTransform rt = card.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.DOKill();
                    rt.DOAnchorPos(localPoint, 0.18f).SetEase(Ease.OutQuad);
                    rt.DOLocalRotateQuaternion(targetRot, 0.18f).SetEase(Ease.OutQuad);
                    rt.SetSiblingIndex(i);
                }
            }
        }

        float duration = 0.18f;
        for (int i = 0; i < targets.Count; i++)
        {
            var item = targets[i];
            // ensure sibling index is set according to final order (important for z after drop)
            item.cv.transform.SetSiblingIndex(i);

            // update stored home immediately so rotation/position reflect new home (requirements)
            item.cv.SetHomeTransform(item.pos, item.rot);

            // animate to home
            item.cv.MoveToHome(item.pos, item.rot, duration, enableInteractOnComplete: false);
        }
    }

    // ========== Selection API ==========
    public void ToggleSelect(CardView card)
    {
        if (card == null) return;
        if (selectedCards.Contains(card)) RemoveSelected(card);
        else AddSelected(card);
    }

    public void AddSelected(CardView card)
    {
        if (card == null) return;
        if (!selectedCards.Contains(card))
        {
            selectedCards.Add(card);
            card.SetSelected(true);
        }
    }

    public void RemoveSelected(CardView card)
    {
        if (card == null) return;
        if (selectedCards.Contains(card))
        {
            selectedCards.Remove(card);
            card.SetSelected(false);
        }
    }

    public void DestroySelected()
    {
        if (selectedCards.Count == 0) return;

        var toDestroy = new List<CardView>(selectedCards);
        selectedCards.Clear();

        foreach (var cv in toDestroy)
        {
            if (cv == null) continue;
            GameObject go = cv.gameObject;
            if (handCards.Contains(go)) handCards.Remove(go);
            Destroy(go);
        }

        UpdateCardPositions();
    }

    // ========== Drag reorder support ==========
    public void HandleDragReorder(CardView draggingCardView, PointerEventData eventData)
    {
        if (draggingCardView == null) return;
        GameObject draggingGO = draggingCardView.gameObject;

        RectTransform parentRect = handParent;
        Canvas canvas = parentRect.GetComponentInParent<Canvas>();
        Camera cam = (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera) ? canvas.worldCamera : null;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, cam, out localPoint);
        float pointerX = localPoint.x;

        int currentIndex = handCards.IndexOf(draggingGO);
        if (currentIndex == -1) return;

        int targetIndex = 0;
        bool set = false;
        for (int i = 0; i < handCards.Count; i++)
        {
            GameObject go = handCards[i];
            if (go == null || go == draggingGO) continue;
            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt == null) continue;
            float x = rt.anchoredPosition.x;
            if (pointerX > x) targetIndex = i + 1;
            else
            {
                targetIndex = i;
                set = true;
                break;
            }
        }
        if (!set) targetIndex = handCards.Count;

        if (targetIndex < 0) targetIndex = 0;
        if (targetIndex > handCards.Count) targetIndex = handCards.Count;

        int removeIndex = handCards.IndexOf(draggingGO);
        if (removeIndex == -1) return;

        if (targetIndex == removeIndex || targetIndex == removeIndex + 1) return;

        handCards.RemoveAt(removeIndex);
        if (removeIndex < targetIndex) targetIndex--;
        if (targetIndex < 0) targetIndex = 0;
        if (targetIndex > handCards.Count) targetIndex = handCards.Count;

        handCards.Insert(targetIndex, draggingGO);

        UpdateCardPositions();
    }
}
