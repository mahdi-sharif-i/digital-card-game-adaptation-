using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.IO;

public class CardZoomAndSave : MonoBehaviour, IPointerClickHandler
{
    private Vector3 originalScale;
    private bool zoomed = false;
    [SerializeField] private float zoomFactor = 2f;

    [HideInInspector] public Button SaveButtonPrefab; // Assigned by spawner
    private Button saveButtonInstance;

    private Image cardImage;

    private void Awake()
    {
        originalScale = transform.localScale;
        cardImage = GetComponent<Image>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        zoomed = !zoomed;
        transform.localScale = zoomed ? originalScale * zoomFactor : originalScale;

        if (zoomed)
            ShowSaveButton();
        else
            HideSaveButton();
    }

    private void ShowSaveButton()
    {
        if (SaveButtonPrefab == null) return;

        if (saveButtonInstance == null)
        {
            saveButtonInstance = Instantiate(SaveButtonPrefab, transform.parent);
            saveButtonInstance.transform.SetAsLastSibling(); // On top
            saveButtonInstance.onClick.RemoveAllListeners();
            saveButtonInstance.onClick.AddListener(SaveCardImage);
        }

        saveButtonInstance.gameObject.SetActive(true);
        // Optional: position near the card
        RectTransform rt = saveButtonInstance.GetComponent<RectTransform>();
        rt.position = new Vector3(transform.position.x, transform.position.y - 50f, transform.position.z);
    }

    private void HideSaveButton()
    {
        if (saveButtonInstance != null)
            saveButtonInstance.gameObject.SetActive(false);
    }

    private void SaveCardImage()
    {
        if (cardImage == null || cardImage.sprite == null) return;

        Texture2D tex = SpriteToTexture(cardImage.sprite);
        byte[] bytes = tex.EncodeToPNG();
        string path = Path.Combine(Application.persistentDataPath, cardImage.sprite.name + ".png");
        File.WriteAllBytes(path, bytes);
        Debug.Log($"Card saved to: {path}");
    }

    private Texture2D SpriteToTexture(Sprite sprite)
    {
        Texture2D tex = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height, TextureFormat.RGBA32, false);
        Color[] pixels = sprite.texture.GetPixels(
            (int)sprite.textureRect.x,
            (int)sprite.textureRect.y,
            (int)sprite.textureRect.width,
            (int)sprite.textureRect.height);
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
}
