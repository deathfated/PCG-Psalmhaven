using UnityEngine;

[CreateAssetMenu(fileName = "DamagePlayer", menuName = "Item/Nothing")]
public class NullEffect : ItemInteractEffect
{
    public override void ItemInteract(Character character)
    {
        Debug.Log("Nothing Haven");
    }
}