using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private bool isPlayMode = true;
    public bool IsPlayMode
    {
        get => isPlayMode;
        set => isPlayMode = value;
    }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    // public void DrawCard()
    // {
    //     if (handCards.Count >= maxHandSize)
    //     {
    //         Debug.Log("[HandManager] Hand full, cannot draw.");
    //         return;
    //     }

    //     if (cardPrefab == null || handParent == null || spawnPoint == null)
    //     {
    //         Debug.LogWarning("[HandManager] Draw aborted: assign cardPrefab, handParent and spawnPoint in Inspector.");
    //         return;
    //     }

    //     CardData chosen = null;
    //     if (DrawPile.Instance != null)
    //     {
    //         chosen = DrawPile.Instance.DrawTop();
    //         if (chosen == null)
    //         {
    //             Debug.Log("[HandManager] DrawPile empty (DrawTop returned null). Falling back to cardDatabase if any.");
    //             return;
    //         }
    //     }

    //     if (chosen == null && cardDatabase != null && cardDatabase.Count > 0)
    //     {
    //         chosen = cardDatabase[Random.Range(0, cardDatabase.Count)];
    //     }

    //     Card model = null;
    //     if (chosen != null)
    //     {
    //         model = new Card(chosen);
    //     }

    //     GameObject newCard = Instantiate(cardPrefab, handParent, false);
    //     if (newCard == null)
    //     {
    //         Debug.LogError("[HandManager] Instantiate returned null.");
    //         return;
    //     }

    //     RectTransform cardRT = newCard.GetComponent<RectTransform>();
    //     if (cardRT == null)
    //     {
    //         cardRT = newCard.AddComponent<RectTransform>();
    //     }

    //     cardRT.anchoredPosition = spawnPoint.anchoredPosition;
    //     cardRT.localScale = Vector3.one;

    //     CardView cv = newCard.GetComponent<CardView>();
    //     if (cv != null)
    //     {
    //         cv.SetSelected(false);
    //         cv.SetInteractable(false);
    //         cv.waitForInitialPlacement = true;
    //         cv.SetHomeTransform(cardRT.anchoredPosition, cardRT.localRotation);

    //         if (model != null)
    //         {
    //             cv.Bind(model);
    //         }
    //     }
    //     else
    //     {
    //         Debug.LogWarning("[HandManager] Instantiated prefab does not contain CardView component.");
    //     }

    //     handCards.Add(newCard);
    //     UpdateCardPositions();
    // }

}
