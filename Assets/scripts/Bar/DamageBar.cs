using UnityEngine;
using DG.Tweening;

public class DamageBar : MonoBehaviour
{
    public float Damage, MaxDamage;
    public float Width, Height;
    [SerializeField] private RectTransform damageBar;
    public void SetMaxDamage(float maxDamage)
    {
        MaxDamage = maxDamage;
        SetDamage(MaxDamage);
    }
    public void SetDamage(float damage)
    {
        Damage = damage;
        float baseWidth = MaxDamage/8;
        float newWidth = ( (Damage+baseWidth) / (MaxDamage+baseWidth) ) * Width;
        if(Damage==0) newWidth=0;
        damageBar.DOSizeDelta(new Vector2(newWidth, damageBar.sizeDelta.y),0.2f).SetEase(Ease.InQuad);
    }
}
