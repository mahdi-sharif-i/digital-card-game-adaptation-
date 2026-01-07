using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using suitName;
using Unity.VisualScripting;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    [SerializeField] private GameObject buttonPlay;
    [SerializeField] private GameObject buttonDiscard;
    [SerializeField] private GameObject buttonJoker;
    private List<CardData> playArea=new();


    private bool isPlayMode = true;
    private bool SortMode = true;
    private bool skipSufferingDamage = false;
    
    [SerializeField] private float DrawDelay = 0.15f;
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
    private void Start()
    {
        DrawCard(8);
    }
    private void GameLose()
    {
        Debug.Log("you lose the game");
    }
    private void GameWin()
    {
        Debug.Log("you win the game");
    }
    public Coroutine DrawCard(int draw)
    {
        return StartCoroutine(DrawCardCoroutine(draw));
    }


    IEnumerator DrawCardCoroutine(int draw)
    {
        for (int i = 0; i < draw; i++)
        {
            if (HandManager.Instance.HandSize() >= HandManager.Instance.MaxHandSize())
            {
                Debug.Log("[HandManager] Hand full, cannot draw.");
                yield break;
            }

            if (DrawPile.Instance == null)
            {
                Debug.Log("[HandManager] DrawPile Not Find.");
                yield break;
            }

            CardData chosen = DrawPile.Instance.DrawTop();
            if (chosen == null)
            {
                Debug.Log("[HandManager] DrawPile empty (DrawTop returned null).");
                yield break;
            }

            HandManager.Instance.DrawCard(chosen);
            HandManager.Instance.UpdateCardPositions();

            yield return new WaitForSeconds(DrawDelay);
        }
    }
    private void HealCard(int heal)
    {
        for(int i = 0; i < heal; i++)
        {
            CardData chosen = null;
            if (DrawPile.Instance != null)
            {
                chosen = DiscardPile.Instance.HealCard();
                if (chosen == null)
                {
                    Debug.Log("[HandManager] DiscardPile empty (HealCard returned null).");
                    return;
                }
            }
            else
            {
                    Debug.Log("[HandManager] DrawPile Not Find.");
                    return;
            }
            DrawPile.Instance.PutUnderPile(chosen);
        }
        
    }
    private void ShieldEnemyAttack(int Shield)
    {
        EnemyPile.Instance.Shield(Shield);
    }
    private void DealDamage(int damage)
    {
        if (damage > EnemyPile.Instance.HP)
        {
            EnemyPile.Instance.TakeDamage(damage);
            CardData defeatedEnemy=EnemyPile.Instance.DefeatEnemy();
            DiscardPile.Instance.Discard(defeatedEnemy);
            DiscardPile.Instance.Discard(playArea);
            playArea.Clear();
            skipSufferingDamage=true;
        }
        else if(damage == EnemyPile.Instance.HP)
        {
            EnemyPile.Instance.TakeDamage(damage);
            CardData defeatedEnemy=EnemyPile.Instance.DefeatEnemy();
            DrawPile.Instance.PutOnPile(defeatedEnemy);
            DiscardPile.Instance.Discard(playArea);
            playArea.Clear();
            skipSufferingDamage=true;
        }
        else
        {
            EnemyPile.Instance.TakeDamage(damage);
            skipSufferingDamage=false;
        }
        if (EnemyPile.Instance.enemyRemain()==0)
        {
            GameWin();
        }
    }
    private void DealDoubleDamage(int damage)
    {
        int double_damage = damage *2; 
        DealDamage(double_damage);
    }
    public void PlayCardsButton()
    {
        StartCoroutine(playCards());
    }
    IEnumerator playCards()
    {
        List<CardData> playedCard = HandManager.Instance.PopSelected();

        if (playedCard == null || playedCard.Count == 0)
        {
            buttonPlay.SetActive(true);
            yield break;
        }

        int totalValue = playedCard.Sum(card => card.value);
        List<CardSuit> suits = playedCard.Select(card => card.suit).ToList();
        skipSufferingDamage = false;

        if (suits.Contains(CardSuit.Hearts) && EnemyPile.Instance.immunity() != CardSuit.Hearts)
        {
            HealCard(totalValue);
        }

        if (suits.Contains(CardSuit.Diamonds) && EnemyPile.Instance.immunity() != CardSuit.Diamonds)
        {
            yield return StartCoroutine(DrawCardCoroutine(totalValue));
        }

        if (suits.Contains(CardSuit.Spades) && EnemyPile.Instance.immunity() != CardSuit.Spades)
        {
            ShieldEnemyAttack(totalValue);
        }

        if (suits.Contains(CardSuit.Clubs) && EnemyPile.Instance.immunity() != CardSuit.Clubs)
        {
            DealDoubleDamage(totalValue);
        }
        else
        {
            DealDamage(totalValue);
        }

        if (skipSufferingDamage || EnemyPile.Instance.DMG == 0)
        {
            buttonPlay.SetActive(true);
            isPlayMode = true;
            yield break;
        }

        playArea.AddRange(playedCard);
        isPlayMode = false;
        buttonDiscard.SetActive(true);

        int totalHandValue = HandManager.Instance.HandTotal();
        while (EnemyPile.Instance.DMG > totalHandValue)
        {
            if (buttonJoker == null || buttonJoker.GetComponent<JokerButton>().RemainJoker()==0)
            {
                GameLose();
            }
            else
            {
                buttonJoker.GetComponent<JokerButton>().Redraw();
            }
            yield break;
        }
    }
    public void DiscardCards()
    {
        if(EnemyPile.Instance.DMG > HandManager.Instance.SelectedTotal())
        {
            buttonDiscard.SetActive(true);
            return; 
        }
        List<CardData> discardedCard = HandManager.Instance.PopSelected();
        DiscardPile.Instance.Discard(discardedCard);
        isPlayMode=true;
        buttonPlay.SetActive(true);
        while (HandManager.Instance.HandSize() == 0)
        {
            if (buttonJoker == null || buttonJoker.GetComponent<JokerButton>().RemainJoker()==0)
            {
                GameLose();
                
            }
            else
            {
                buttonJoker.GetComponent<JokerButton>().Redraw();
            }
        }
    }
    public void RedrawCards()
    {
        HandManager.Instance.DiscardAll();
        DrawCard(8);
    }
    public void SortCards()
    {
        if (SortMode)
        {
            HandManager.Instance.SortBySuit();
        }
        else
        {
            HandManager.Instance.SortByValue();
        }
        SortMode=!SortMode;
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (HandManager.Instance.SelectedCount() > 0) HandManager.Instance.PopSelected();
            else DrawCard(3);
        }
    }

}
