
using DG.Tweening;
using Psalmhaven;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace UI
{ 
    public class UIManager : MonoBehaviour
    {
        [Header("Game HUD")]
        [SerializeField] private DiceBoard board;
        [SerializeField] private List<ChoiceText> choiceTextList;
        [SerializeField] private Inventory inventory;
        [Header("Game Menus")]
        [SerializeField] private MainMenu mainMenu;
        [SerializeField] private PauseWindow pauseWindow;
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

        public void StartMainMenu(Action OnMenuOpened, UnityAction<int> RollDiceAction)
        {
            inventory.gameObject.SetActive(false);
            OnMenuOpened += () => board.OpenWindow();
            mainMenu.OnMenuOpened = OnMenuOpened;
            mainMenu.OpenWindow();

            ChoiceData playChoice = new ChoiceData();
            playChoice.choiceValue = "Play";
            playChoice.revealChoice = false;
            List<ChoiceData> choices = new List<ChoiceData>();

            for (int i = 0; i < 6; i++)
            {
                choices.Add(playChoice);
            }

            SetUpChoice(choices.ToArray(), RollDiceAction);
        }

        public IEnumerator CloseMainMenu(Action OnMenuClosed, UnityAction<int> RollDiceAction)
        {
            yield return new WaitForSeconds(0.8f);
            OnMenuClosed += () => inventory.gameObject.SetActive(true);
            mainMenu.OnMenuClosed = OnMenuClosed;
            mainMenu.CloseWindow();
            board.CloseWindow();
            OnRollDiceAction -= RollDiceAction;
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
