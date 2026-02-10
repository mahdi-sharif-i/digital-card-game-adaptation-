using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

#if UNITY_STANDALONE_WIN
using System.Diagnostics;
#endif

public class SaveButton : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image mainButton;
    [SerializeField] private Image pressingButton;
    [SerializeField] private Image targetImage;
    [SerializeField] private string fileName = "SavedImage.png";

    private Vector3 originalScale;
    private float scaleFactor = 1.1f;
    private float tweenDuration = 0.2f;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    public void setButtonActive(bool isActive)
    {
        gameObject.SetActive(isActive);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (mainButton != null) mainButton.enabled = false;
        if (pressingButton != null) pressingButton.enabled = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (mainButton != null) mainButton.enabled = true;
        if (pressingButton != null) pressingButton.enabled = false;

        SaveUIElementToDesktop(targetImage);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.DOScale(originalScale * scaleFactor, tweenDuration).SetEase(Ease.OutQuad);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.DOScale(originalScale, tweenDuration).SetEase(Ease.OutQuad);
    }

    private void SaveUIElementToDesktop(Image uiImage)
    {
        if (uiImage == null || uiImage.sprite == null)
        {
            UnityEngine.Debug.LogWarning("[SaveButton] targetImage or sprite is null.");
            return;
        }

        Sprite sprite = uiImage.sprite;
        Rect spriteRect = sprite.rect;
        int pixelWidth = (int)spriteRect.width;
        int pixelHeight = (int)spriteRect.height;

        if (pixelWidth <= 0 || pixelHeight <= 0)
        {
            UnityEngine.Debug.LogWarning("[SaveButton] invalid sprite size.");
            return;
        }

        // determine layer to use: prefer UI_Save, then UI, then Default(0)
        int layerIndex = LayerMask.NameToLayer("UI_Save");
        if (layerIndex == -1) layerIndex = LayerMask.NameToLayer("UI");
        if (layerIndex == -1) layerIndex = 0;

        // create temporary camera
        GameObject camGo = new GameObject("TempSaveCam");
        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.clear;
        cam.cullingMask = 1 << layerIndex;
        cam.allowHDR = false;
        cam.allowMSAA = false;

        // create temporary world-space canvas
        GameObject canvasGo = new GameObject("TempSaveCanvas");
        canvasGo.layer = layerIndex;
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = cam;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 32767; // ensure on top

        RectTransform canvasRt = canvasGo.GetComponent<RectTransform>();
        canvasRt.sizeDelta = new Vector2(pixelWidth, pixelHeight);

        // scale so that 1 unit = 1 pixel based on sprite.pixelsPerUnit
        float ppu = sprite.pixelsPerUnit;
        if (ppu <= 0) ppu = 100f;
        float scale = 1f / ppu;
        canvasGo.transform.localScale = Vector3.one * scale;

        // create image child
        GameObject imageGo = new GameObject("TempSaveImage");
        imageGo.layer = layerIndex;
        imageGo.transform.SetParent(canvasGo.transform, false);

        Image img = imageGo.AddComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.color = uiImage.color; // keep tint
        img.raycastTarget = false;

        RectTransform imgRt = imageGo.GetComponent<RectTransform>();
        imgRt.anchorMin = new Vector2(0.5f, 0.5f);
        imgRt.anchorMax = new Vector2(0.5f, 0.5f);
        imgRt.pivot = new Vector2(0.5f, 0.5f);
        imgRt.sizeDelta = new Vector2(pixelWidth, pixelHeight);
        imgRt.anchoredPosition = Vector2.zero;

        // position camera to look at canvas
        // place canvas at origin and camera at z = -10 in world units
        canvasGo.transform.position = Vector3.zero;
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        cam.transform.rotation = Quaternion.identity;

        // set orthographic size to cover half the height in world units
        float worldHeight = pixelHeight * scale;
        cam.orthographicSize = worldHeight / 2f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 20f;

        // create render texture with sprite pixel size
        RenderTexture rt = new RenderTexture(pixelWidth, pixelHeight, 24, RenderTextureFormat.ARGB32);
        rt.Create();
        cam.targetTexture = rt;

        // render and read pixels
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;
        try
        {
            cam.Render();

            Texture2D tex = new Texture2D(pixelWidth, pixelHeight, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, pixelWidth, pixelHeight), 0, 0);
            tex.Apply();

            byte[] png = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string safeName = MakeFileNameUnique(desktopPath, fileName);
            string fullPath = Path.Combine(desktopPath, safeName);
            File.WriteAllBytes(fullPath, png);
            UnityEngine.Debug.Log("[SaveButton] Image saved to: " + fullPath);

#if UNITY_STANDALONE_WIN
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", "/select,\"" + fullPath + "\"");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[SaveButton] Could not open Explorer: " + e.Message);
            }
#endif
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError("[SaveButton] Save failed: " + e);
        }
        finally
        {
            RenderTexture.active = prev;
            if (cam != null) cam.targetTexture = null;
            if (rt != null)
            {
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
            }
            UnityEngine.Object.DestroyImmediate(camGo);
            UnityEngine.Object.DestroyImmediate(canvasGo);
        }
    }

    private string MakeFileNameUnique(string folderPath, string desiredName)
    {
        string nameOnly = Path.GetFileNameWithoutExtension(desiredName);
        string ext = Path.GetExtension(desiredName);
        if (string.IsNullOrEmpty(ext)) ext = ".png";

        string candidate = nameOnly + ext;
        string full = Path.Combine(folderPath, candidate);
        int count = 1;
        while (File.Exists(full))
        {
            candidate = string.Format("{0} ({1}){2}", nameOnly, count, ext);
            full = Path.Combine(folderPath, candidate);
            count++;
            if (count > 1000) break;
        }
        return candidate;
    }
}
