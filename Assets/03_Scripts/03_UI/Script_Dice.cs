using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static UnityEngine.InputSystem.InputAction;

namespace UI
{
    public class Script_Dice : MonoBehaviour
    {
        [SerializeField] private Image diceImage;
        [SerializeField] private List<Sprite> diceHeads;
        private readonly int repeatAmount = 1;
        private readonly float time= 0.2f;
        private bool canRoll = true;
        private void Update()
        {
            if(Input.GetKeyDown(KeyCode.E))
            {
                RollDice();
            }
        }

        public void RollDice()
        {
            if (canRoll)
            {
                int number = Random.Range(0, 5);
                StartCoroutine(Randomize(number));
            }
        }

        private IEnumerator Randomize(int number)
        {
            int currentRepeat = 0;
            canRoll = false;

            while (currentRepeat < repeatAmount)
            {
                foreach (var head in diceHeads)
                {
                    diceImage.sprite = head;
                    yield return new WaitForSeconds(time);
                }
                currentRepeat++;
            }

            diceImage.sprite = diceHeads[number];
            canRoll = true;
        }
    }
}
