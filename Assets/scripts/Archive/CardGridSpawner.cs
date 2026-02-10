using System;
using UnityEngine;
using UnityEngine.UI;

public class CardGridSpawner : MonoBehaviour
{
    [SerializeField] private string resourcesFolder = "sprites"; 

    private void Start()
    {
        SpawnCards();
    }

    private void SpawnCards()
    {
        Transform contentParent = GameObject.Find("Content")?.transform;
        if (contentParent == null)
        {
            Debug.LogError("Content GameObject not found!");
            return;
        }

        for (int i = contentParent.childCount - 1; i >= 0; i--)
            Destroy(contentParent.GetChild(i).gameObject);

        var sprites = Resources.LoadAll<Sprite>(resourcesFolder);
        if (sprites.Length == 0)
        {
            Debug.LogError($"No sprites in Resources/{resourcesFolder}");
            return;
        }

        var spriteLookup = new System.Collections.Generic.Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in sprites)
            spriteLookup[s.name] = s;

        string[] suits = { "h", "s", "d", "c" };
        string[] ranks = { "1","2","3","4","5","6","7","8","9","10","j","q","k" };

        foreach (var suit in suits)
        {
            foreach (var rank in ranks)
            {
                string spriteName = suit + rank;

                if (!spriteLookup.TryGetValue(spriteName, out Sprite sp))
                    continue;

                GameObject cardGO = new GameObject(spriteName, typeof(RectTransform), typeof(Image));
                cardGO.transform.SetParent(contentParent, false);

                var img = cardGO.GetComponent<Image>();
                img.sprite = sp;
                img.preserveAspect = true;
                img.raycastTarget = true;

                // Add click script and auto-connect to ZoomManager
                var clickScript = cardGO.AddComponent<CardClick>();
            }
        }
    }
}
