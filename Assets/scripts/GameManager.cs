using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using suitName;
using Unity.VisualScripting;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    // private void Start()
    // {
    //     GamePlay();
    // }
    public void GameLose()
    {
        // Debug.Log("you lose the game");
    }
    public void GameWin()
    {
        // Debug.Log("you win the game");
    }
    // public Coroutine GamePlay()
    // {
    //     return StartCoroutine(GamePlayCoroutine());
    // }


    // IEnumerator GamePlayCoroutine()
    // {
    //     int draw =2,DrawDelay=1;
    //     for (int i = 0; i < draw; i++)
    //     {
    //         if (HandManager.Instance.HandSize() >= HandManager.Instance.MaxHandSize())
    //         {
    //             Debug.Log("[HandManager] Hand full, cannot draw.");
    //             yield break;
    //         }

    //         if (DrawPile.Instance == null)
    //         {
    //             Debug.Log("[HandManager] DrawPile Not Find.");
    //             yield break;
    //         }

    //         CardData chosen = DrawPile.Instance.DrawTop();
    //         if (chosen == null)
    //         {
    //             Debug.Log("[HandManager] DrawPile empty (DrawTop returned null).");
    //             yield break;
    //         }

    //         HandManager.Instance.DrawCard(chosen);
    //         HandManager.Instance.UpdateCardPositions();

    //         yield return new WaitForSeconds(DrawDelay);
    //     }
    // }
}
