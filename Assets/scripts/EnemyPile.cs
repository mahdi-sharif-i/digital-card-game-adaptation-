using System;
using System.Collections.Generic;
using UnityEngine;
using suitName;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class EnemyPile : MonoBehaviour
{
    public static EnemyPile Instance { get; private set; }

    [SerializeField] private string resourcesFolder = "Sprites";
    private readonly LinkedList<CardData> Castle = new();
    private System.Random rng = new System.Random();

    [SerializeField] private Image TopEnemySprite;
    [SerializeField] private HealthBar EnemyHealthBar;
    [SerializeField] private DamageBar EnemyDamageBar;
    [SerializeField] private int HP;
    [SerializeField] private int MaxHP;
    [SerializeField] private int DMG;
    [SerializeField] private int MaxDMG;


    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        BuildCastle();
        EnemySetup();
    }
    private void Update()
    {
        if (Input.GetKeyDown("s"))
            {
                TakeDamage(5);
            }
        if (Input.GetKeyDown("a"))
            {
                TakeDamage(1);
            }
        if (Input.GetKeyDown("q"))
            {
                Shield(1);
            }        
        if (Input.GetKeyDown("w"))
            {
                Shield(5);
            }
    }
    void EnemySetup()
    {
        MaxHP= Castle.First.Value.value*2;
        MaxDMG= Castle.First.Value.value;
        EnemyHealthBar.SetMaxHealth(MaxHP);
        EnemyDamageBar.SetMaxDamage(MaxDMG);
        Instance.TopEnemySprite.sprite = Castle.First.Value.CardSprite;
    }

    // ---------- API (Deal Damage to Enemy and Decrease his Damage, Defeat Enemy)----------
    public int Count => Castle.Count;
    public void TakeDamage(int dmg)
    {
        HP -= dmg;
        HP = Mathf.Clamp(HP,0,MaxHP);
        EnemyHealthBar.SetHealth(HP);
    }
    public void Shield(int def)
    {
        DMG -= def;
        DMG = Mathf.Clamp(DMG,0,MaxDMG);
        EnemyDamageBar.SetDamage(DMG);
    }
    public CardData DefeatEnemy()
    {
       if (Castle.Count == 0) return null;
        var first = Castle.First.Value;
        Castle.RemoveFirst();
        return first;
    }
    // ---------- Create ----------

    private void BuildCastle()
    {
        Castle.Clear();

        var sprites = Resources.LoadAll<Sprite>(resourcesFolder);
        var spriteLookup = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in sprites) spriteLookup[s.name] = s;

        var suitMap = new Dictionary<CardSuit, char>
        {
            { CardSuit.Hearts, 'H' },
            { CardSuit.Spades, 'S' },
            { CardSuit.Diamonds, 'D' },
            { CardSuit.Clubs, 'C' }
        };

        var ValueMap = new Dictionary<int, char>
        {
            { 11, 'J' },
            { 12, 'Q' },
            { 13, 'K' },
        };

        var suits = (CardSuit[])Enum.GetValues(typeof(CardSuit));

        for (int sv = 11; sv <= 13; sv++)
        {
            int v = (sv - 9) * 5;
            char letterValue = ValueMap[sv];

            // Fisher–Yates shuffle
            for (int i = suits.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                var tmp = suits[i];
                suits[i] = suits[j];
                suits[j] = tmp;
            }

            foreach (var suit in suits)
            {
                char letterSuit = suitMap[suit];
                string spriteName = $"{letterSuit}{letterValue}";
                spriteLookup.TryGetValue(spriteName, out Sprite sp);

                CardData cd = CardData.CreateCard(sp, v, sv, suit);
                Castle.AddLast(cd);
            }
        }
    }


}
