using UnityEngine;
public abstract class ItemInteractEffect : ScriptableObject
{
    public string effectName;
    public abstract void ItemInteract(Character character);
}
