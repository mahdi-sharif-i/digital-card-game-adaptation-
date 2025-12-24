using UnityEngine;
using suitName;
public class Card
{
    private readonly CardData cardData;
    public Sprite CardSprite { get => cardData.CardSprite;}
    public int value { get; set;}
    public int sotrValue { get; set;}
    public CardSuit suit { get; set;}

        public Card(CardData CD)
    {
        cardData = CD;
        suit = CD.suit;
        value = CD.value;
        sotrValue = CD.sotrValue;
    }

    

}
