using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private bool isPlayMode = true;
    [SerializeField] private bool SortMode = true;
    
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
    public void DrawCard(int draw)
    {
        StartCoroutine(DrawCardCoroutine(draw));
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
        EnemyPile.Instance.TakeDamage(damage);
    }
    private void DealDoubleDamage(int damage)
    {
        int double_damage = damage *2; 
        EnemyPile.Instance.TakeDamage(double_damage);
    }
    private void playCards()
    {
        
    }
    private void DiscardCards()
    {
        
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
