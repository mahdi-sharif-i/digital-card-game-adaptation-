using UnityEngine;
using DG.Tweening;
using TMPro;


public class HealthBar : MonoBehaviour
{
    public float Health, MaxHealth;
    public float Width, Height;
    [SerializeField] private RectTransform healthBar;
        public TMP_Text remainHealth;
    public void SetMaxHealth(float maxHealth)
    {
        MaxHealth = maxHealth;
        SetHealth(MaxHealth);
    }
    public void SetHealth(float health)
    {
        Health = health;
        float baseWidth = MaxHealth/8;
        float newWidth = ( (Health+baseWidth) / (MaxHealth+baseWidth) ) * Width;
        if(Health==0) newWidth=0;
        healthBar.DOSizeDelta(new Vector2(newWidth, healthBar.sizeDelta.y),0.2f).SetEase(Ease.InQuad);
        remainHealth.text = Health.ToString();
    }
}
