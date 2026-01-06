using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.EventSystems;
using System;

public class HandManager : MonoBehaviour
{
    public static HandManager Instance { get; private set; }

    [Header("Hand Settings")]
    [SerializeField] public int maxHandSize = 8;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private RectTransform handParent;
    [SerializeField] private RectTransform spawnPoint;
    [SerializeField] private SplineContainer splineContainer;

    private List<GameObject> handCards = new();
    public int HandSize()
    {
        return handCards.Count;
    }
        public int MaxHandSize()
    {
        return maxHandSize;
    }

    public void SortBySuit()
    {
        handCards = handCards
            .Where(go => go != null)
            .Select(go => new { go, keys = GetSortingKeys(go) })
            .OrderBy(x => x.keys.suit)
            .ThenBy(x => x.keys.value)
            .Select(x => x.go)
            .ToList();

        UpdateCardPositions();
    }

    public void SortByValue()
    {
        handCards = handCards
            .Where(go => go != null)
            .Select(go => new { go, keys = GetSortingKeys(go) })
            .OrderBy(x => x.keys.value)
            .ThenBy(x => x.keys.suit)
            .Select(x => x.go)
            .ToList();

        UpdateCardPositions();
    }
    private (int suit, int value) GetSortingKeys(GameObject go)
    {
        if (go == null) return (int.MaxValue, int.MaxValue);

        CardView cv = go.GetComponent<CardView>();
        if (cv == null) return (int.MaxValue, int.MaxValue);

        int suit = int.MaxValue;
        int value = GetSortValueFromCardView(cv);

        try
        {
            var model = cv.GetModel();
            if (model != null)
            {
                suit = (int)model.suit;
            }
            else
            {
                var cd = cv.GetCardData();
                if (cd != null)
                {
                    suit = (int)cd.suit;
                }
            }
        }
        catch
        {
            
        }

        return (suit, value);
    }
    private List<CardView> selectedCards = new();

    public int SelectedCount()
    {
        return selectedCards.Count;
    }
    public int SelectedTotal()
    {
        int total=0;
        foreach (CardView card in selectedCards)
        {
            total += card.GetCardData().value;
        }
        return total;
    }
    public int HandTotal()
    {
        int total=0;
        foreach (GameObject card in handCards)
        {
            total += card.GetComponent<CardView>().GetCardData().value;
        }
        return total;
    }
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



    // ---------- Draw ----------
    public void DrawCard(CardData DrawnCard)
    {
        if (cardPrefab == null || handParent == null || spawnPoint == null)
        {
            Debug.LogWarning("[HandManager] Draw aborted: assign cardPrefab, handParent and spawnPoint in Inspector.");
            return;
        }
        if (DrawPile.Instance != null)
        {
            if (DrawnCard == null)
            {
                Debug.Log("[HandManager] DrawPile empty (DrawTop returned null). Falling back to cardDatabase if any.");
                return;
            }
        }

        Card model = null;
        if (DrawnCard != null)
        {
            model = new Card(DrawnCard);
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
    public List<CardData> PopSelected()
    {
        if (selectedCards.Count == 0) 
            return new List<CardData>();

        List<CardData> toDiscard = new List<CardData>();
        var toDestroy = new List<CardView>(selectedCards);
        selectedCards.Clear();

        foreach (var cv in toDestroy)
        {
            if (cv == null) continue;
            
            CardData cd = null;
            try { cd = cv.GetCardData(); } 
            catch { cd = null; }

            if (cd != null) 
                toDiscard.Add(cd);

            GameObject go = cv.gameObject;
            if (handCards.Contains(go)) 
                handCards.Remove(go);
            
            Destroy(go);
        }

        UpdateCardPositions();
        return toDiscard;
    }
    public List<CardData> DiscardAll()
    {
        List<CardData> toDiscard = new List<CardData>();

        var cardsCopy = new List<GameObject>(handCards);
        foreach (var go in cardsCopy)
        {
            if (go == null) continue;

            CardView cv = go.GetComponent<CardView>();
            CardData cd = null;

            try
            {
                if (cv != null) cd = cv.GetCardData();
            }
            catch
            {
                cd = null;
            }

            if (cd != null) toDiscard.Add(cd);

            if (cv != null)
            {
                if (selectedCards.Contains(cv))
                    RemoveSelected(cv);
                else
                    cv.SetSelected(false);
            }

            if (handCards.Contains(go)) handCards.Remove(go);
            Destroy(go);
        }

        selectedCards.Clear();
        UpdateCardPositions();

        return toDiscard;
    }

    // ---------- Selection API (updated rules) ----------
    public void ToggleSelect(CardView card)
    {
        if (card == null) return;
        if (selectedCards.Contains(card)) RemoveSelected(card);
        else AddSelected(card);
    }

    private void AddSelected(CardView card)
    {
        if (GameManager.Instance.IsPlayMode) AddSelected_ForPlay(card);
        else AddSelected_ForDiscard(card);
    }
    private void AddSelected_ForDiscard(CardView card)
    {
        if (card == null || selectedCards.Contains(card)) return;
        
        int newCardValue = GetValueFromCardView(card);
        if (newCardValue == 0) return;
        
        int sumBefore = CalculateCurrentSum();
        bool wasAtOrAboveTarget = (sumBefore >= EnemyPile.Instance.DMG);
        
        selectedCards.Add(card);
        card.SetSelected(true);
        
        int currentSum = sumBefore + newCardValue;
        
        if (wasAtOrAboveTarget || currentSum > EnemyPile.Instance.DMG)
        {
            var removableCards = GetRemovableCardsExcept(card);
            
            
            if (wasAtOrAboveTarget && removableCards.Count > 0)
            {
                DeselectLowestValueCard(ref currentSum, removableCards);
            }
            
            while (removableCards.Count > 0 && ((currentSum - GetValueFromCardView(removableCards[0])) >= EnemyPile.Instance.DMG))
            {
                DeselectLowestValueCard(ref currentSum, removableCards);
            }
        }
    }
    private void AddSelected_ForPlay(CardView card)
    {
        if (card == null || selectedCards.Contains(card)) return;

        int newVal = GetValueFromCardView(card);

        // --- Step 1: Handle special case for value == 1 when reaching 2 cards ---
        bool skipDeselectDifferent = ShouldSkipDeselectWhenAdding(card, newVal);
        
        // Deselect cards with different values (unless skipped)
        if (!skipDeselectDifferent)
        {
            DeselectCardsWithDifferentValue(newVal);
        }

        // --- Step 2: Add the new card ---
        selectedCards.Add(card);
        card.SetSelected(true);

        // --- Step 3: Enforce "max two 1s" rule ---
        EnforceMaxTwoOnes();

        // --- Step 4: Enforce sum <= 10 ---
        EnforceSumRuleAfterSelection();
    }
        // ------------------  Selecting Helpers  ------------------
    private int CalculateCurrentSum()
    {
        int sum = 0;
        foreach (var sc in selectedCards)
        {
            sum += GetValueFromCardView(sc);
        }
        return sum;
    }

    private List<CardView> GetRemovableCardsExcept(CardView exceptCard)
    {
        return selectedCards
            .Where(sc => sc != exceptCard)
            .OrderBy(sc => GetValueFromCardView(sc))
            .ThenBy(sc => selectedCards.IndexOf(sc))
            .ToList();
    }

    private void DeselectLowestValueCard(ref int currentSum, List<CardView> removableCards)
    {
        if (removableCards.Count == 0) return;
        
        CardView lowestCard = removableCards[0];
        int valueToRemove = GetValueFromCardView(lowestCard);
        
        currentSum -= valueToRemove;
        RemoveSelected(lowestCard);
        removableCards.RemoveAt(0);
    }
    private bool ShouldSkipDeselectWhenAdding(CardView newCard, int newVal)
    {
        if (selectedCards.Count == 0) return true;
        if (selectedCards.Count != 1) return false;

        int existingVal = GetValueFromCardView(selectedCards[0]);
        return (newVal == 1 || existingVal == 1);
    }

    private void DeselectCardsWithDifferentValue(int targetValue)
    {
        // Important: iterate backwards to safely remove while iterating (or use ToList)
        var toRemove = selectedCards.Where(sc => GetValueFromCardView(sc) != targetValue).ToList();
        foreach (var card in toRemove)
        {
            RemoveSelected(card);
        }
    }

    private void EnforceMaxTwoOnes()
    {
        var ones = selectedCards.Where(sc => GetValueFromCardView(sc) == 1).ToList();

        if (ones.Count <= 2) return;

        // Deselect all non-ones first
        var nonOnes = selectedCards.Except(ones).ToList();
        foreach (var card in nonOnes)
        {
            RemoveSelected(card);
        }

        // Now, if still more than 2 ones, remove oldest (i.e., earliest in list)
        while (selectedCards.Count > 2)
        {
            RemoveSelected(selectedCards[0]); // oldest
        }
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
    private int GetSortValueFromCardView(CardView cv)
    {
        if (cv == null) return 0;
        try
        {
            var model = cv.GetModel();
            if (model != null) return model.sotrValue;
        }
        catch { }

        try
        {
            var cd = cv.GetCardData();
            if (cd != null) return cd.sortValue;
        }
        catch { }

        return 0;
    }

    // enforce sum rule: while sum > 10 remove oldest selected card,
    // but skip removal entirely if exactly two cards exist and one has value == 1 (exception)
    private void EnforceSumRuleAfterSelection()
    {
        if(selectedCards.Count==1)return;
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
