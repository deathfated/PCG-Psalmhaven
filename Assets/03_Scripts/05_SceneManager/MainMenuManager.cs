using System.Security.Cryptography;
using UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Psalmhaven
{
    public class MainMenuManager : MonoBehaviour
    {
        PlayerController player;
        PlayerInput playerInput;
        private void Start()
        {
            playerInput = UIManager.instance.GetComponent<PlayerInput>();
            player = GameObject.FindWithTag("Player").GetComponent<PlayerController>();
            EnableInput(false);

            UIManager.instance.StartMainMenu(null, CloseMainMenu);
        }

        private void EnableInput(bool status)
        {
            player.enabled = status;
            playerInput.enabled = status;
        }

        private void CloseMainMenu(int number)
        {
            StartCoroutine(UIManager.instance.CloseMainMenu(()=> EnableInput(true), CloseMainMenu));
        }
    }
}