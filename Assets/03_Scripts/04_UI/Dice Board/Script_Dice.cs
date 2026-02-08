using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Psalmhaven.UI
{
    public class Script_Dice : MonoBehaviour
    {
        [SerializeField] private Image diceImage;
        [SerializeField] private List<Sprite> diceHeads;
        private readonly int repeatAmount = 1;
        private readonly float time= 0.2f;
        public bool canRoll = false;
        public bool isRoll = false;
        private Unity.Mathematics.Random randominator;

        private void Start()
        {
            //init Unity.Math random
            uint seed = (uint)DateTime.Now.Ticks;
            randominator = new Unity.Mathematics.Random(seed);
        }


        public void RollDice(Action OnStartRoll = null, Action<int> OnFinishedRoll = null)
        {
            if (canRoll)
            {
                //int number = UnityEngine.Random.Range(0, 5);
                int number = randominator.NextInt(0,5);
                StartCoroutine(Randomize(number, OnStartRoll, OnFinishedRoll));
            }
        }
        private IEnumerator Randomize(int number, Action OnStartRoll = null, Action<int> OnFinishedRoll = null)
        {
            int currentRepeat = 0;
            canRoll = false;
            isRoll = true;
            OnStartRoll?.Invoke();

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
            isRoll = false;
            OnFinishedRoll?.Invoke(number);
        }
    }
}
