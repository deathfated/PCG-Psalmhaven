using Psalmhaven;
using UnityEngine;

public class TriggerTransition : MonoBehaviour
{
    public int targetSpawnPoint = 0;
    public int targetSceneIndex;
    public Transform spawnPoint;
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Scene Transition");
        SceneTransitionManager.instance.LoadSceneAsync(targetSceneIndex, true, targetSpawnPoint);
    }
}
