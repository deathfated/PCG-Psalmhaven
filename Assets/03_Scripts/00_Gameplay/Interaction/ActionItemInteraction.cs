using System;
using UI;
using UnityEngine;

public class ActionItemInteraction : BaseInteraction
{
    [Header("Canvas Group")]
    [SerializeField] private CanvasGroup uiPanel;

    private Coroutine activeCoroutine;
    private bool hasInteract;

    public ItemEffectData[] interactEffectData;
    //Dummy 
    protected Character playerCharacter;
    private void Start()
    {
        playerCharacter = GameObject.FindWithTag("Player").GetComponent<Character>();
        uiPanel.alpha = 0f;
    }

    public override void TriggerEnter(Collider other)
    {
        if (hasInteract)
            return;

        ClearCoroutine();
        ShowPanel(true);
    }

    private void ClearCoroutine()
    {
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
        }
    }

    public void OnRollDice(int diceNumber)
    {
        if (interactEffectData[diceNumber] == null)
        {
            CloseInteraction();
        }
        else
        {
            interactEffectData[diceNumber].interactEffect.ItemInteract(playerCharacter);
            interactEffectData[diceNumber].hasInteract = true;
        }
    }

    public void CloseInteraction()
    {
        hasInteract = true;
    }

    public override void TriggerExit(Collider other)
    {
        if (hasInteract)
            return;

        base.TriggerExit(other);
        ShowPanel(false);
    }

    public void ShowPanel(bool isShow)
    {
        ClearCoroutine();
        if (isShow)
        {
            activeCoroutine = StartCoroutine(Fade(uiPanel, 0, 1, onComplete : ShowRollDice));
        }
        else
        {
            activeCoroutine = StartCoroutine(Fade(uiPanel, 1, 0, onComplete: HideRollDice));
        }
    }

    private void ShowRollDice()
    {
        UIManager.instance.OpenBoard(true);

        ChoiceData[] choices = new ChoiceData[6];
        for (int i = 0; i < interactEffectData.Length; i++)
        {
            choices[i] = new ChoiceData();
            choices[i].choiceValue = interactEffectData[i].interactEffect.effectName;
            choices[i].revealChoice = interactEffectData[i].hasInteract;
        }

        UIManager.instance.SetUpChoice(choices, OnRollDice);
    }

    private void HideRollDice()
    {
        UIManager.instance.OpenBoard(false);
    }
}

[System.Serializable]
public class ItemEffectData
{
    public ItemInteractEffect interactEffect;
    public bool hasInteract = false;
}
