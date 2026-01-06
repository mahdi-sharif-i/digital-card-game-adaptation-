using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using suitName;

[DisallowMultipleComponent]
public class DrawPile : MonoBehaviour
{
    public static DrawPile Instance { get; private set; }

    [SerializeField] private string resourcesFolder = "Sprites";
    private readonly LinkedList<CardData> pile = new();
    private System.Random rng = new System.Random();
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        BuildDefault40();
        ShufflePile();
    }

    // ---------- API ----------
    public int Count => pile.Count;
    public TMP_Text remainCards;
    // ---------- API (Drawing And Healing) ----------

    public CardData DrawTop()
    {
        if (pile.Count == 0) return null;
        var first = pile.First.Value;
        pile.RemoveFirst();
        remainCards.text = Count.ToString();
        return first;
    }
    public void PutUnderPile(CardData card)
    {
        if (card == null) return;
        pile.AddLast(card);
        remainCards.text = Count.ToString();
    }
    public void PutOnPile(CardData card)
    {
        if (card == null) return;
        pile.AddFirst(card);
        remainCards.text = Count.ToString();
    }
    // ---------- Creating ----------

    private void ShufflePile()
    {
        var arr = pile.ToArray();
        ShuffleListInPlace(arr);
        pile.Clear();
        foreach (var c in arr) pile.AddLast(c);
    }


    private void BuildDefault40()
    {
        pile.Clear();

        // load sprites from Resources/<resourcesFolder>
        var sprites = Resources.LoadAll<Sprite>(resourcesFolder);
        var spriteLookup = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in sprites) spriteLookup[s.name] = s;

        // mapping suit -> letter for sprite naming
        var suitMap = new Dictionary<CardSuit, char>
        {
            { CardSuit.Hearts, 'H' },
            { CardSuit.Spades, 'S' },
            { CardSuit.Diamonds, 'D' },
            { CardSuit.Clubs, 'C' }
        };

        foreach (var suit in Enum.GetValues(typeof(CardSuit)).Cast<CardSuit>())
        {
            char letter = suitMap[suit];
            for (int v = 1; v <= 10; v++)
            {
                // sprite names expected: e.g. H1, H2, ... S10, ...
                string spriteName = $"{letter}{v}";
                spriteLookup.TryGetValue(spriteName, out Sprite sp);

                // create runtime CardData ScriptableObject and fill its serialized backing fields via reflection
                CardData cd = CardData.CreateCard(sp, v, v, suit);
                // SetAutoPropertyBackingField(cd, "CardSprite", sp);
                // SetAutoPropertyBackingField(cd, "value", v);
                // SetAutoPropertyBackingField(cd, "sotrValue", v); // you can change how sotrValue calculated if needed
                // SetAutoPropertyBackingField(cd, "suit", suit);

                pile.AddLast(cd);
            }
        }
    }
    // shuffle for arrays
    private void ShuffleListInPlace<T>(T[] arr)
    {
        int n = arr.Length;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (arr[k], arr[n]) = (arr[n], arr[k]);
        }
    }

}
