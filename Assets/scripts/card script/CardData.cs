using UnityEngine;
using suitName;

[CreateAssetMenu(menuName ="Card data")]
public class CardData : ScriptableObject
{
    public Sprite CardSprite { get; private set;}
    public int value { get; private set;}
    public int sortValue { get; private set;}
    public CardSuit suit { get; private set;}

    public static CardData CreateCard(Sprite sprite, int _value, int _sortValue, CardSuit _suit)
    {
        CardData card = ScriptableObject.CreateInstance<CardData>();
        card.CardSprite = sprite;
        card.value = _value;
        card.sortValue = _sortValue;
        card.suit = _suit;
        return card;
    }
}
