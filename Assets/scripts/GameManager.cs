using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using suitName;
using Unity.VisualScripting;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public GameObject winPage;
    public GameObject losePage;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    public void GameLose()
    {
        losePage.SetActive(true);
    }
    public void GameWin()
    {
        winPage.SetActive(true);
    }
}
