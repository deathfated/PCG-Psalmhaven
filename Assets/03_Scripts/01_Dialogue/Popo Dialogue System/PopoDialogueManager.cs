using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace POPO.Dialogue
{
    public class PopoDialogueManager : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject dialogueCanvas;
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private TextMeshProUGUI speakerText;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private Button continueButton;

        [Header("Choices")]
        [SerializeField] private GameObject choicesPanel;
        private GameObject[] choiceButtons;

        private Dialogue currentDialogue;
        private int currentLineIndex;
        private bool waitingForChoice;

        public static PopoDialogueManager Instance;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject); // Destroy any duplicate instances
            }
            dialogueCanvas.SetActive(false);
            dialoguePanel.SetActive(false);
            choicesPanel.SetActive(false);
            continueButton.onClick.AddListener(NextLine);
        }

        private void Start()
        {
            choiceButtons = new GameObject[choicesPanel.transform.childCount];

            for (int i = 0; i < choicesPanel.transform.childCount; i++)
            {
                choiceButtons[i] = choicesPanel.transform.GetChild(i).gameObject;
            }

        }

        public void StartDialogue(Dialogue dialogue)
        {
            currentDialogue = dialogue;
            currentLineIndex = 0;
            waitingForChoice = false;

            dialogueCanvas.SetActive(true);
            dialoguePanel.SetActive(true);
            ShowLine();
        }

        void ShowLine()
        {
            if (currentLineIndex >= currentDialogue.lines.Length)
            {
                EndDialogue();
                return;
            }

            DLine line = currentDialogue.lines[currentLineIndex];

            //TODO: reuse previous speaker name if left empty
            speakerText.text = line.speakerName;
            dialogueText.text = line.text;

            // Handle choices
            if (line.choices != null && line.choices.Length > 0)
            {
                ShowChoices(line.choices);
                waitingForChoice = true;
                continueButton.gameObject.SetActive(false);
            }
            else
            {
                waitingForChoice = false;
                choicesPanel.SetActive(false);
                continueButton.gameObject.SetActive(true);
            }
        }

        void ShowChoices(DChoice[] choices)
        {
            choicesPanel.SetActive(true);

            for (int a = 0; a < choices.Length; a++)
            {
                choiceButtons[a].SetActive(true);
                choiceButtons[a].GetComponentInChildren<TextMeshProUGUI>().text = choices[a].choiceText;
                var curButton = choiceButtons[a].GetComponent<Button>();
                curButton.GetComponent<DButton>().selectedChoice = choices[a];

                choiceButtons[a].GetComponent<Button>().onClick.AddListener(() =>
                {
                    curButton.GetComponent<DButton>().ContinueDialogue();
                });
            }
        }


        public void SelectChoice(DChoice choice)
        {
            choicesPanel.SetActive(false);
            waitingForChoice = false;

            if (choice.nextDialogue != null)
            {
                StartDialogue(choice.nextDialogue);
            }
            else
            {
                // Continue current dialogue
                currentLineIndex++;
                ShowLine();
            }
        }

        public void NextLine()
        {
            //stop next line if currently chosing
            if (waitingForChoice)
                return;

            currentLineIndex++;
            ShowLine();
        }

        void EndDialogue()
        {
            dialoguePanel.SetActive(false);
            choicesPanel.SetActive(false);
            dialogueCanvas.SetActive(false);
            currentDialogue = null;
        }
    }
}