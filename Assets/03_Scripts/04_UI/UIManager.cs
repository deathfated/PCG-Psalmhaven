
using Psalmhaven;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
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

        [Header("Game Currency")]
        [SerializeField] private TextMeshProUGUI goldCurrency;

        private List<ChoiceData> activeChoices = new();
        private UnityAction<int> OnRollDiceAction;

        private Coroutine closeCoroutine;

        private bool isRoll = false;
        private void Awake()
        {
            if (instance == null) instance = this;
            else Destroy(gameObject);

            DontDestroyOnLoad(gameObject);
        }

        public void OpenBoard(bool status) 
        {
            if (status)
            {
                board.OpenWindow();

                if(closeCoroutine != null)
                    StopCoroutine(closeCoroutine);
            }
            else {
                if (isRoll)
                {
                    closeCoroutine = StartCoroutine(WaitForCloseBoardDice());
                }
                else
                {
                    board.CloseWindow();
                }
            }            
        }

        private IEnumerator WaitForCloseBoardDice()
        {
            while (isRoll)
            {
                yield return null;
            }

            yield return new WaitForSeconds(1f);
            closeCoroutine = null;
            board.CloseWindow();
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
            isRoll = true;
            board.RollDice(OnFinishedRoll: (number) => {
                if(OnDiceRolled != null) OnDiceRolled(number);

                choiceTextList[number].RevealChoice(activeChoices[number].choiceValue);
                OnRollDiceAction?.Invoke(number);

                isRoll = false;
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

        public void StartMainMenu(UnityAction<int> RollDiceAction)
        {
            mainMenu.OpenWindow();
            board.OpenWindow();
            OnRollDiceAction += RollDiceAction;
        }

        public void CloseMainMenu()
        {
            mainMenu.CloseWindow();
            board.CloseWindow();
        }

        public void Pause(InputAction.CallbackContext context)
        {
            if (pauseWindow.isPaused) pauseWindow.CloseWindow();
            else pauseWindow.OpenWindow();
        }

        public void UpdateCurrency(string currency)
        {
            goldCurrency.text = currency;
        }
    }
}

[System.Serializable]
public class ChoiceData
{
    public string choiceValue;
    public bool revealChoice;
}
