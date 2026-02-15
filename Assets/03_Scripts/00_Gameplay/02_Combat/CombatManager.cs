using Psalmhaven.UI;
using UnityEngine;

namespace Psalmhaven
{
    public class CombatManager : MonoBehaviour
    {
        [SerializeField] Player player;
        [SerializeField] Enemy enemy;
        [SerializeField] GameObject optionsPanel;
        [SerializeField] GameObject gameoverPanel;
        [SerializeField] GameObject hUD;
        [SerializeField] Canvas canvas;
        [SerializeField] float delayRoll;

        private UnityEngine.UI.Button[] buttons;
        private Coroutine activeCoroutine;

        private bool isFirstFight = true;


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
            ReAssignPlayer();
        }

        public void StartCombat()
        {
            hUD.gameObject.SetActive(true);
            
            if (player == null) ReAssignPlayer();
            player.GetComponent<PlayerController>().canMove = false;
            
            canvas.gameObject.SetActive(true);
            optionsPanel.SetActive(true);
            ShowPanel(true);

            if (enemy == null) ReAssignEnemy();
            //player face enemy, TODO: enemy too?
            player.GetComponent<PlayerController>().FaceObject(enemy.transform);
            player.GetComponent<PlayerController>().SwitchToCombatCam();
            enemy.IsInCombat = true;

            //temporary place
            if (isFirstFight)
            {
                TransitionBoss TB = GameObject.FindWithTag("Entity").GetComponent<TransitionBoss>();
                Debug.Log("POPO " + TB);
                gameoverPanel.GetComponentInChildren<UnityEngine.UI.Button>().onClick.AddListener(TB.TriggerCutsceneBossFirst);
            }
        }

        public void EndCombat()
        {
            Debug.Log("End Combat");
            ShowPanel(false);
            if (player == null) ReAssignPlayer();
            player.GetComponent<PlayerController>().canMove = true;
            player.GetComponent<PlayerController>().SwitchToMainCam();
            canvas.gameObject.SetActive(false);
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
            //Time.timeScale = 0f;
            player.GetComponent<PlayerController>().SwitchToMainCam();
        }

        public void RestartGame() //repurposed for first boss "death"
        {
            gameoverPanel.SetActive(false);
            hUD.SetActive(true);
            Time.timeScale = 1f;
            player.GetComponent<PlayerController>().SwitchToMainCam();
            Debug.Log("POPOPO");

            if (isFirstFight)
            {
                isFirstFight = false;
            }
            else
            {
                SceneTransitionManager.instance.Restart();
            }
        }

        private void ReAssignPlayer()
        {
            player = GameObject.FindWithTag("Player").GetComponent<Player>();
        }

        private void ReAssignEnemy()
        {
            enemy = GameObject.FindWithTag("Enemy").GetComponent<Enemy>();
        }

    }
}