using Psalmhaven;
using Psalmhaven.UI;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameCurrency currency;
    public static GameManager instance;

    [SerializeField] private PlayerController playerController;
    public PlayerController PlayerController => playerController;
    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
    }

    public void GetCurrency(int value)
    {
        currency.Gold += value;
        UIManager.instance.UpdateCurrency(currency.Gold.ToString());
    }
}

[System.Serializable]
public class GameCurrency
{
    public int Gold;

    public void SetDefault()
    {
        Gold = 100;
    }
}
