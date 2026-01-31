
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace UI
{ 
    public class UIManager : MonoBehaviour
    {
        //[SerializeField] private InputAction playerInput;
        [SerializeField] private DiceBoard board;
        [SerializeField] private List<ChoiceText> choiceTextList;
        [SerializeField] private Inventory inventory;
        [SerializeField] private PauseWindow pauseWindow;
        private bool isBoardOpened = false;
        private bool isPaused = false;
        [HideInInspector] public static UIManager instance;

        private List<ChoiceData> activeChoices = new();
        private UnityAction<int> OnRollDiceAction;
        private void Awake()
        {
            if (instance == null) instance = this;
            else Destroy(gameObject);
        }

        public void OpenBoard(bool status) 
        {
            if (status)
            {
                board.OpenWindow();
            }
            else { 
                board.CloseWindow(); 
            }            
        }

        public void SetUpChoice(ChoiceData[] choices, UnityAction<int> OnCompleteRoll)
        {
            OnRollDiceAction = null;
            OnRollDiceAction += OnCompleteRoll;
            activeChoices.Clear();
            foreach (var choice in choices)
            {
                activeChoices.Add(choice);
            }

            for (int i = 0; i < activeChoices.Count; i++)
            {
                if (activeChoices[i].revealChoice)
                {
                    choiceTextList[i].ShowChoice();
                    choiceTextList[i].RevealChoice(activeChoices[i].choiceValue);
                }
                else
                {
                    choiceTextList[i].ShowChoice();
                }
            }
        }

        public void RollDice(InputAction.CallbackContext context)
        {
            RollDice(null);
        }

        public void RollDice(Action<int> OnDiceRolled)
        {
            board.RollDice(OnFinishedRoll: (number) => {
                if(OnDiceRolled != null) OnDiceRolled(number);

                choiceTextList[number].RevealChoice(activeChoices[number].choiceValue);
                OnRollDiceAction?.Invoke(number);
            });
        }

        public void SetHealth(float amount)
        {
            inventory.SetHealth(amount);
        }
        public void SetGold(float amount)
        {
            inventory.SetGold(amount);
        }

        public void Pause(InputAction.CallbackContext context)
        {
            if (pauseWindow.isPaused) pauseWindow.CloseWindow();
            else pauseWindow.OpenWindow();
        }
    }
}

[System.Serializable]
public class ChoiceData
{
    public string choiceValue;
    public bool revealChoice;
}
