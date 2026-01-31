using UnityEngine;

[CreateAssetMenu(fileName = "GetItem", menuName = "Item/Get Item")]
public class GetItem : ItemInteractEffect
{
    public override void ItemInteract(Character character)
    {
        Debug.Log("Player Get Item");
    }
}
