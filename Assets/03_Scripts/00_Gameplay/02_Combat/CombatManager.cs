using System;
using System.Collections;
using UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace Psalmhaven
{
    public class CombatManager : MonoBehaviour
    {
        [SerializeField] Player player;
        [SerializeField] Enemy enemy;

        private PlayerController playerController;

        [SerializeField] GameObject optionsPanel;
        [SerializeField] GameObject gameoverPanel;
        //private DiceRoller diceRoller;
        [SerializeField] GameObject hUD;
        [SerializeField] Canvas canvas;
        private int resultRoll = -1;

        [SerializeField] float delayRoll;
        private UnityEngine.UI.Button[] buttons;
        private Coroutine activeCoroutine;


        [HideInInspector] public static CombatManager Instance;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // If an instance already exists and it's not this one, destroy this new object
                Destroy(this.gameObject);
            }
            else
            {
                // Otherwise, set this as the only instance
                Instance = this;
                // Optional: keep the object alive across scene loads
                DontDestroyOnLoad(this.gameObject);
            }
        }

        private void Start()
        {
            buttons = optionsPanel.GetComponentsInChildren<UnityEngine.UI.Button>();
            //diceRoller = UIManager.Instance.GetComponentInChildren<DiceRoller>();
            //playerController = player.GetComponent<PlayerController>();
            ReAssignPlayer();
            hUD = UIManager.instance.gameObject;


        }



        public void StartCombat()
        {
            hUD.gameObject.SetActive(true);
            
            if (player == null) ReAssignPlayer();
            player.GetComponent<PlayerController>().canMove = false;
            
            canvas.gameObject.SetActive(true);
            ShowPanel(true);

            //player face enemy, TODO: enemy too?
            //player.GetComponent<PlayerController>().FaceObject(enemy.transform);
            enemy.IsInCombat = true;

            //temporary place
            TransitionBoss TB = GameObject.FindWithTag("Entity").GetComponent<TransitionBoss>();
            Debug.Log("POPO " + TB);
            gameoverPanel.GetComponentInChildren<UnityEngine.UI.Button>().onClick.AddListener(TB.TriggerCutsceneBossFirst);
        }

        public void EndCombat()
        {
            hUD.gameObject.SetActive(false);
            if (player == null) ReAssignPlayer();
            player.GetComponent<PlayerController>().canMove = true;
            canvas.gameObject.SetActive(false);
            ShowPanel(false);
        }

        public void ShowPanel(bool isShow)
        {
            ClearCoroutine();
            if (isShow)
            {
                ShowRollDice();
            }
            else
            {
                HideRollDice();
            }
        }

        private void ClearCoroutine()
        {
            if (activeCoroutine != null)
            {
                StopCoroutine(activeCoroutine);
            }
        }

        private void ShowRollDice()
        {
            UIManager.instance.OpenBoard(true);

            ChoiceData[] choices = new ChoiceData[6];
            for (int i = 0; i < player.combatEffectData.Length; i++)
            {
                choices[i] = new ChoiceData();
                choices[i].choiceValue = player.combatEffectData[i];
                choices[i].revealChoice = true;
            }

            UIManager.instance.SetUpChoice(choices, ActionAttack);
        }

        private void HideRollDice()
        {
            UIManager.instance.OpenBoard(false);
        }


        public void StartActionAttack()
        {

            UIManager.instance.RollDice(number =>
            {
                resultRoll = number;
                //ActionAttack(resultRoll);
            });
            Debug.Log("rando ");
        }

        private void ActionAttack(int resultRoll)
        {

            //check if its string or int
            int playerDmg;
            if (player.currentMask == 1) //switch actions based on mask (stupid method)
            {
                if (int.TryParse(player.combatActions[resultRoll], out playerDmg)) { }
                else return;
            }
            else
            {
                if (int.TryParse(player.combatActions2[resultRoll], out playerDmg)) { }
                else return ;
            }

            int enemyDmg;
            if (int.TryParse(enemy.CombatActions[resultRoll], out enemyDmg)) { }
            else return ;

            int resultDamage = playerDmg + enemyDmg;
            Debug.Log($"{playerDmg} + {enemyDmg} = {resultDamage}");

            float dmg;
            if (resultRoll > 2) //enemy gets damage
            {
                //if (resultDamage < 0) resultDamage = 0; //checking if enemy too tanky
                dmg = resultDamage;
                enemy.TakeDamage(dmg);

            }
            else //player gets damage
            {
                dmg = resultDamage;
                player.TakeDamage(dmg);
            }

            Debug.Log("end = " + dmg);
        }

        private void OnEnable()
        {
            Player.OnPlayerDied += ShowGameOver;
            Enemy.OnEnemyDied += EndCombat;

        }

        private void OnDisable()
        {
            Player.OnPlayerDied -= ShowGameOver;
            Enemy.OnEnemyDied -= EndCombat;

        }

        private void ShowGameOver()
        {
            gameoverPanel.SetActive(true);
            optionsPanel.SetActive(false);
            hUD.SetActive(false);
            Time.timeScale = 0f; // optional pause
        }

        public void RestartGame() //repurposed for first boss "death"
        {

            /*destroy dontdestroyonload objects
            //foreach (var obj in GameObject.FindGameObjectsWithTag("Persistent"))
            //{
            //    Destroy(obj);
            //}

            //reload scene
            gameoverPanel.SetActive(false);
            Time.timeScale = 1;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);*/

            gameoverPanel.SetActive(false);
            Time.timeScale = 1f;
            //SceneManager.
        }

        private void ReAssignPlayer()
        {
            player = GameObject.FindWithTag("Player").GetComponent<Player>();
        }

    }
}