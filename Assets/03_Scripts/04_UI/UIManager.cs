
using System.Collections.Generic;
using UnityEngine;

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
        private void Awake()
        {
            if (instance == null) instance = this;
            else Destroy(gameObject);
        }

#if UNITY_EDITOR
        // for debugging purposes
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
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
            }
        }
#endif

        public void OpenBoard(bool status) 
        {
            if (status)
            {
                board.OpenWindow();
                foreach (var item in choiceTextList)
                {
                    item.ShowChoice();
                }
            }
            else { 
                board.CloseWindow(); 
            }            
        }

        public void RollDice()
        {
            board.RollDice(OnFinishedRoll: (number) => {
                choiceTextList[number].RevealChoice(dummyChoice.choices[number].name);
                dummyChoice.choices[number].Action();
            });
        }

        public void Pause(bool status)
        {
            if (status) pauseWindow.OpenWindow();
            else pauseWindow.CloseWindow();
        }
    }
}
