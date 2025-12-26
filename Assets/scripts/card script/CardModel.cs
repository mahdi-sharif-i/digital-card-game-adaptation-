using UnityEngine;
using suitName;
using System;

public class Card
{
    private readonly CardData cardData;

    public Sprite CardSprite => cardData.CardSprite;
    public CardData Data => cardData;

    // backing fields so we can notify on change
    private int _value;
    private int _sortValue;
    private CardSuit _suit;

    public int value
    {
        get => _value;
        set
        {
            if (_value == value) return;
            _value = value;
            OnChanged?.Invoke(this);
        }
    }

    public int sotrValue
    {
        get => _sortValue;
        set
        {
            if (_sortValue == value) return;
            _sortValue = value;
            OnChanged?.Invoke(this);
        }
    }

    public CardSuit suit
    {
        get => _suit;
        set
        {
            if (_suit == value) return;
            _suit = value;
            OnChanged?.Invoke(this);
        }
    }

    public Card(CardData CD)
    {
        cardData = CD;
        _suit = CD.suit;
        _value = CD.value;
        _sortValue = CD.sortValue;
    }

    // event for view binding
    public event Action<Card> OnChanged;
}
