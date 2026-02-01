using System.Collections.Generic;
using UnityEngine;

public class ItemInteractionRuntimeStore : MonoBehaviour
{
    public static ItemInteractionRuntimeStore Instance;

    private Dictionary<string, bool[]> _itemStates =
        new Dictionary<string, bool[]>();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool TryGet(string itemId, out bool[] states)
    {
        return _itemStates.TryGetValue(itemId, out states);
    }

    public void Set(string itemId, bool[] states)
    {
        _itemStates[itemId] = states;
    }
}