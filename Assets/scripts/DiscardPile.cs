using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;


public class DiscardPile : MonoBehaviour
{
    public static DiscardPile Instance { get; private set; }
    
    private readonly List<CardData> pile = new();
    
    public int Count => pile.Count;
    public Text remainCards;


    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }
    // ---------- API (Discard And Healing) ----------

    public CardData HealCard()
    {
        if (pile.Count == 0)
            return null;

        int r = Random.Range(0, pile.Count);
        var randomCard = pile[r];

        pile.RemoveAt(r);

        remainCards.text = pile.Count.ToString();
        return randomCard;
    }
    public void Discard(IEnumerable<CardData> cards)
    {
        if (cards == null) return;
        foreach (var c in cards) pile.Add(c);
        remainCards.text = Count.ToString();
    }
    public void Discard(CardData card)
    {
        if (card == null) return;
        pile.Add(card);
        remainCards.text = Count.ToString();
    }
}
