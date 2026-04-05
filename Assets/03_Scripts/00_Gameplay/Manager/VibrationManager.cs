using UnityEngine;
using UnityEngine.InputSystem;

namespace Psalmhaven
{
    public class VibrationManager : MonoBehaviour
    {
        public static VibrationManager Instance;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        public void VibrateController(float lowFreq, float highFreq, float duration){
            if (Gamepad.current == null)
                return;
            
            Debug.Log($"Vibration Start {lowFreq}, {highFreq}");
            Gamepad.current.SetMotorSpeeds(lowFreq, highFreq);
            Invoke(nameof(StopVibrateConttroller), duration);
        }

        public void StopVibrateConttroller()
        {
            Debug.Log($"Vibration End");
            if (Gamepad.current != null)
                Gamepad.current.SetMotorSpeeds(0.0f, 0.0f);
        }
    }
}

