using UnityEngine;

public class SokobanManager : MonoBehaviour
{
    public TargetTrigger[] allTargets;
    [SerializeField] private Animator Animasi;

    
    public void Start ()
    {
        
        if (Animasi == null)
            Animasi = GetComponent<Animator>();


        foreach(TargetTrigger trigger in allTargets)
        {
            trigger.initial(this);
        }

    }

    public void CheckWinCondition()
    {
        foreach (TargetTrigger target in allTargets)
        {
            if (!target.IsComplete)
            {
                return; // masih ada target kosong
            }
        }

        WinGame();
    }

    private void WinGame()
    {
        
        Debug.Log("SEMUA BOX SUDAH MASUK!");
    
        Animasi.SetTrigger("OpenDoor");

        if (SoundManager.Instance != null && SoundManager.Instance.CompleteClip != null)
        {
        SoundManager.Instance.PlaySFX(SoundManager.Instance.CompleteClip);
        }

        
        
    }



}