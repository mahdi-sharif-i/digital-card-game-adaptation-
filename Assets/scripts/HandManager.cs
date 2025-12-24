using System.Collections.Generic;
using System.Linq;
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

    [Header("Optional fallback CardData list (used if DrawPile missing or empty)")]
    [SerializeField] private List<CardData> cardDatabase = new();

    private List<GameObject> handCards = new();
    private List<CardView> selectedCards = new(); // maintains selection order: first = oldest

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

    // ---------- Draw ----------
    public void DrawCard()
    {
        if (handCards.Count >= maxHandSize)
        {
            Debug.Log("[HandManager] Hand full, cannot draw.");
            return;
        }

        if (cardPrefab == null || handParent == null || spawnPoint == null)
        {
            Debug.LogWarning("[HandManager] Draw aborted: assign cardPrefab, handParent and spawnPoint in Inspector.");
            return;
        }

        CardData chosen = null;
        if (DrawPile.Instance != null)
        {
            chosen = DrawPile.Instance.DrawTop();
            if (chosen == null)
            {
                Debug.Log("[HandManager] DrawPile empty (DrawTop returned null). Falling back to cardDatabase if any.");
            }
        }

        if (chosen == null && cardDatabase != null && cardDatabase.Count > 0)
        {
            chosen = cardDatabase[Random.Range(0, cardDatabase.Count)];
        }

        Card model = null;
        if (chosen != null)
        {
            model = new Card(chosen);
        }

        GameObject newCard = Instantiate(cardPrefab, handParent, false);
        if (newCard == null)
        {
            Debug.LogError("[HandManager] Instantiate returned null.");
            return;
        }

        RectTransform cardRT = newCard.GetComponent<RectTransform>();
        if (cardRT == null)
        {
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

            if (model != null)
            {
                cv.Bind(model);
            }
        }
        else
        {
            Debug.LogWarning("[HandManager] Instantiated prefab does not contain CardView component.");
        }

        handCards.Add(newCard);
        UpdateCardPositions();
    }

    // ---------- Remove / Discard selected ----------
    public void DestroySelected()
    {
        if (selectedCards.Count == 0) return;

        List<CardData> toDiscard = new List<CardData>();
        var toDestroy = new List<CardView>(selectedCards);
        selectedCards.Clear();

        foreach (var cv in toDestroy)
        {
            if (cv == null) continue;
            CardData cd = null;
            try { cd = cv.GetCardData(); } catch { cd = null; }

            if (cd != null) toDiscard.Add(cd);

            GameObject go = cv.gameObject;
            if (handCards.Contains(go)) handCards.Remove(go);
            Destroy(go);
        }

        if (toDiscard.Count > 0 && DrawPile.Instance != null)
        {
            if (toDiscard.Count == 1) DrawPile.Instance.Discard(toDiscard[0]);
            else DrawPile.Instance.Discard(toDiscard);
        }
        else if (toDiscard.Count > 0)
        {
            Debug.LogWarning("[HandManager] No DrawPile instance found; discarded CardData will not be returned to pile.");
        }

        UpdateCardPositions();
    }

    // ---------- Selection API (updated rules) ----------
    public void ToggleSelect(CardView card)
    {
        if (card == null) return;
        if (selectedCards.Contains(card)) RemoveSelected(card);
        else AddSelected(card);
    }

    public void AddSelected(CardView card)
    {
        if (card == null) return;
        if (selectedCards.Contains(card)) return;

        int newVal = GetValueFromCardView(card);

        int wouldBeCount = selectedCards.Count + 1;

        // Exception for "do not deselect others with different value":
        // If after adding there would be exactly two selected cards and either
        // the new card or the existing card has value == 1, skip deselecting different-valued cards.
        bool skipDeselectDiff = false;
        if (wouldBeCount == 2)
        {
            bool existingHasOne = false;
            if (selectedCards.Count == 1)
            {
                existingHasOne = (GetValueFromCardView(selectedCards[0]) == 1);
            }
            if (newVal == 1 || existingHasOne) skipDeselectDiff = true;
        }

        // 1) Deselect all previously selected cards that have a different value (unless exception)
        if (!skipDeselectDiff)
        {
            var toDeselectDiff = new List<CardView>();
            foreach (var sc in selectedCards)
            {
                int v = GetValueFromCardView(sc);
                if (v != newVal) toDeselectDiff.Add(sc);
            }
            foreach (var d in toDeselectDiff) RemoveSelected(d);
        }

        // 2) Add the new card (it becomes most-recent/youngest)
        selectedCards.Add(card);
        card.SetSelected(true);

        // 3) If more than two cards with value == 1 are selected:
        //    - first, deselect all non-1 cards (as requested)
        //    - then, if still more than two 1-cards, deselect oldest 1-cards until only two remain.
        int onesCount = selectedCards.Count(sc => GetValueFromCardView(sc) == 1);
        if (onesCount > 2)
        {
            // deselect non-ones
            var nonOnes = selectedCards.Where(sc => GetValueFromCardView(sc) != 1).ToList();
            foreach (var d in nonOnes) RemoveSelected(d);

            // recompute ones list (preserves order)
            var onesList = selectedCards.Where(sc => GetValueFromCardView(sc) == 1).ToList();

            // while more than 2 ones, remove oldest (first in selection order)
            while (onesList.Count > 2)
            {
                var oldestOne = onesList[0];
                RemoveSelected(oldestOne);

                // recompute
                onesList = selectedCards.Where(sc => GetValueFromCardView(sc) == 1).ToList();
            }
        }

        // 4) Enforce sum rule: if sum of values > 10, remove oldest(s) (respecting the 2-with-one exception)
        EnforceSumRuleAfterSelection();
    }

    // helper to compute int value from CardView safely
    private int GetValueFromCardView(CardView cv)
    {
        if (cv == null) return 0;
        try
        {
            var model = cv.GetModel();
            if (model != null) return model.value;
        }
        catch { }

        try
        {
            var cd = cv.GetCardData();
            if (cd != null) return cd.value;
        }
        catch { }

        return 0;
    }

    // enforce sum rule: while sum > 10 remove oldest selected card,
    // but skip removal entirely if exactly two cards exist and one has value == 1 (exception)
    private void EnforceSumRuleAfterSelection()
    {
        int sum = selectedCards.Sum(sc => GetValueFromCardView(sc));

        bool twoWithOneException = selectedCards.Count == 2 && (
            GetValueFromCardView(selectedCards[0]) == 1 || GetValueFromCardView(selectedCards[1]) == 1
        );

        while (sum > 10 && !twoWithOneException && selectedCards.Count > 0)
        {
            var oldest = selectedCards[0];
            RemoveSelected(oldest);

            sum = selectedCards.Sum(sc => GetValueFromCardView(sc));
            twoWithOneException = selectedCards.Count == 2 && (
                GetValueFromCardView(selectedCards[0]) == 1 || GetValueFromCardView(selectedCards[1]) == 1
            );
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

    // ---------- rest (positions, reorder) ----------
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
        if (splineContainer == null)
        {
            float spacing = 100f;
            for (int i = 0; i < handCards.Count; i++)
            {
                var go = handCards[i];
                if (go == null) continue;
                RectTransform rt = go.GetComponent<RectTransform>();
                if (rt == null) continue;
                Vector2 target = new Vector2((i - (handCards.Count - 1) * 0.5f) * spacing, 0f);
                rt.DOKill();
                rt.DOAnchorPos(target, 0.18f).SetEase(Ease.OutQuad);
                rt.SetSiblingIndex(i);
            }
            return;
        }

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
            item.cv.transform.SetSiblingIndex(i);
            item.cv.SetHomeTransform(item.pos, item.rot);
            item.cv.MoveToHome(item.pos, item.rot, duration, enableInteractOnComplete: false);
        }
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
