using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OpenPlan
{
    public sealed class SimulationSpeedController : MonoBehaviour
    {
        public static SimulationSpeedController Instance { get; private set; }
        public event Action<float> Changed;
        public float Speed { get; private set; } = 1f;
        public bool IsPaused => Speed <= 0f;
        private float previous = 1f;

        private void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.spaceKey.wasPressedThisFrame) TogglePause();
            if (keyboard.digit1Key.wasPressedThisFrame) SetSpeed(1f);
            if (keyboard.digit2Key.wasPressedThisFrame) SetSpeed(2f);
            if (keyboard.digit3Key.wasPressedThisFrame) SetSpeed(4f);
        }

        public void TogglePause()
        {
            if (IsPaused) SetSpeed(previous <= 0f ? 1f : previous);
            else { previous = Speed; SetSpeed(0f); }
        }

        public void SetSpeed(float speed)
        {
            speed = speed <= 0f ? 0f : speed <= 1f ? 1f : speed <= 2f ? 2f : 4f;
            if (speed > 0f) previous = speed;
            Speed = speed;
            Time.timeScale = speed;
            Changed?.Invoke(speed);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Time.timeScale = 1f;
        }
    }
}
