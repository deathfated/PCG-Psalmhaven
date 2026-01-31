using UnityEngine;

[CreateAssetMenu(fileName = "HealPlayer", menuName = "Item/Heal Player")]
public class HealPlayer : ItemInteractEffect
{
    public float healValue;

    public override void ItemInteract(Character character)
    {
        character.Heal(healValue);
        Debug.Log("Player Heal");
    }
}
