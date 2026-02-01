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
        Rigidbody rb;
        private void Awake()
        {
            playerInput = UIManager.instance.GetComponent<PlayerInput>();
            player = GameObject.FindWithTag("Player").GetComponent<PlayerController>();
            rb = player.GetComponent<Rigidbody>();
            EnableInput(false);

            UIManager.instance.OpenDisclaimer(true, () => {
                UIManager.instance.OpenDisclaimer(false, () =>
                {
                    UIManager.instance.StartMainMenu(null, CloseMainMenu);
                });
             });
        }

        private void EnableInput(bool status)
        {
            rb.isKinematic = !status;
            player.enabled = status;
            playerInput.enabled = status;
        }

        private void CloseMainMenu(int number)
        {
            StartCoroutine(UIManager.instance.CloseMainMenu(()=> EnableInput(true), CloseMainMenu));
        }
    }
}