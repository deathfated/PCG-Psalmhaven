using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;
    [SerializeField] private TriggerTransition[] triggerTransitions;

    private void Awake()
    {
        if(Instance != null)
        {
            Destroy(Instance);
        }    

        Instance = this;
    }

    public Transform GetSpawnPosition(int index)
    {
        return triggerTransitions[index].spawnPoint;
    }
}
