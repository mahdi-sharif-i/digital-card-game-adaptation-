using UnityEngine;
using suitName;

[CreateAssetMenu(menuName ="Card data")]
public class CardData : ScriptableObject
{
    [field: SerializeField] public Sprite CardSprite { get; private set;}
    [field: SerializeField] public int value { get; private set;}
    [field: SerializeField] public CardSuit suit { get; private set;}

}
