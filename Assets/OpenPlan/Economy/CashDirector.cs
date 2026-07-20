using System;
using UnityEngine;

namespace OpenPlan
{
    /// <summary>Open-ended placement economy: desk work earns cash continuously in simulation time.</summary>
    public sealed class CashDirector : MonoBehaviour
    {
        public const float StartingCash = 0f;
        public const float IncomePerProductivityMinute = 60f;

        public event Action Changed;
        public float CurrentCash { get; private set; }
        public float LifetimeEarned { get; private set; }
        public float LifetimeSpent { get; private set; }

        public void Initialize(float startingCash = StartingCash)
        {
            CurrentCash = Mathf.Max(0f, startingCash);
            LifetimeEarned = 0f;
            LifetimeSpent = 0f;
            Changed?.Invoke();
        }

        public void AccrueDeskIncome(float effectiveProductivity, float simulationSeconds)
        {
            float amount = Mathf.Max(0f, effectiveProductivity) *
                Mathf.Max(0f, simulationSeconds) * IncomePerProductivityMinute / 60f;
            if (amount <= 0f) return;
            CurrentCash += amount;
            LifetimeEarned += amount;
            Changed?.Invoke();
        }

        public bool CanAfford(float amount) => amount >= 0f && CurrentCash + .0001f >= amount;

        public bool TrySpend(float amount)
        {
            if (amount < 0f || !CanAfford(amount)) return false;
            CurrentCash -= amount;
            LifetimeSpent += amount;
            Changed?.Invoke();
            return true;
        }
    }
}
