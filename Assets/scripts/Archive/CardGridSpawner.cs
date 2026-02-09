using System;
using UnityEngine;
using UnityEngine.UI;

public class CardGridSpawner : MonoBehaviour
{
    [SerializeField] private string resourcesFolder = "Cards"; 
    [SerializeField] private Transform pileParent; // GridLayoutGroup parent
    [SerializeField] private Button saveButtonPrefab; // Button prefab to attach to cards

    private void Start()
    {
        SpawnCards();
    }

    private void SpawnCards()
    {
        if (pileParent == null)
            pileParent = transform;

        // Clear previous cards
        for (int i = pileParent.childCount - 1; i >= 0; i--)
            Destroy(pileParent.GetChild(i).gameObject);

        // Load sprites
        var sprites = Resources.LoadAll<Sprite>(resourcesFolder);
        var spriteLookup = new System.Collections.Generic.Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in sprites)
            spriteLookup[s.name] = s;

        string[] suits = { "H", "S", "D", "C" };
        string[] ranks = { "1","2","3","4","5","6","7","8","9","10","J","Q","K" };

        foreach (var suit in suits)
        {
            foreach (var rank in ranks)
            {
                string spriteName = suit + rank;
                if (!spriteLookup.TryGetValue(spriteName, out Sprite sp))
                    continue;

                // Create card GameObject
                GameObject cardGO = new GameObject(spriteName, typeof(RectTransform), typeof(Image));
                cardGO.transform.SetParent(pileParent, false);

                var img = cardGO.GetComponent<Image>();
                img.sprite = sp;
                img.preserveAspect = true;
                img.raycastTarget = true;

                // Add zoom and save script
                var zoomScript = cardGO.AddComponent<CardZoomAndSave>();
                zoomScript.SaveButtonPrefab = saveButtonPrefab;
            }
        }
    }
}
