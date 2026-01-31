
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace UI
{ 
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private DiceBoard board;
        [SerializeField] private List<ChoiceText> choiceTextList;
        [SerializeField] private PauseWindow pauseWindow;
        [SerializeField] private DummyChoiceSO dummyChoice;
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

        // for debugging purposes
        private void Update()
        {
            /*if (Input.GetKeyDown(KeyCode.R))
            {
                isBoardOpened = !isBoardOpened;
                OpenBoard(isBoardOpened);
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                RollDice();
            }
            if(Input.GetKeyDown(KeyCode.Escape))
            {
                isPaused = !isPaused;
                Pause(isPaused);
            }*/
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

        public void RollDice(Action<int> OnDiceRolled)
        {
            board.RollDice(OnFinishedRoll: (number) => {
                OnDiceRolled(number);

                choiceTextList[number].RevealChoice(activeChoices[number].choiceValue);
                OnRollDiceAction?.Invoke(number);
            });
        }

        public void Pause(bool status)
        {
            if (status) pauseWindow.OpenWindow();
            else pauseWindow.CloseWindow();
        }
    }
}

[System.Serializable]
public class ChoiceData
{
    public string choiceValue;
    public bool revealChoice;
}
