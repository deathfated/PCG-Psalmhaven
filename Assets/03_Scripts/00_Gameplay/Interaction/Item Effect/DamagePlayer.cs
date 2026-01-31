using UnityEngine;


[CreateAssetMenu(fileName = "DamagePlayer", menuName = "Item/Damage Player")]
public class DamagePlayer : ItemInteractEffect
{
    public float damageValue;

    public override void ItemInteract(Character character)
    {
        character.TakeDamage(damageValue);
        Debug.Log("Player Take Damage");
    }
}
