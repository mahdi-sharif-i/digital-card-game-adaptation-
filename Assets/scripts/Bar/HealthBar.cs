using UnityEngine;
using DG.Tweening;

public class HealthBar : MonoBehaviour
{
    public float Health, MaxHealth;
    public float Width, Height;
    [SerializeField] private RectTransform healthBar;
    public void SetMaxHealth(float maxHealth)
    {
        MaxHealth = maxHealth;
    }
    public void SetHealth(float health)
    {
        Health = health;
        float newWidth = ( (Health+3) / (MaxHealth+3) ) * Width;
        if(Health==0) newWidth=0;
        healthBar.DOSizeDelta(new Vector2(newWidth, healthBar.sizeDelta.y),0.2f).SetEase(Ease.InQuad);
    }
}
