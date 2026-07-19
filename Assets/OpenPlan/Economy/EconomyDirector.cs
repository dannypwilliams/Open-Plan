using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenPlan
{
    public sealed class EconomyDirector : MonoBehaviour
    {
        public event Action Changed;
        public int Cash { get; private set; } = 4000;
        public int Revenue { get; private set; }
        public int Payroll { get; private set; }
        public int HiringCosts { get; private set; }
        public int FiringCosts { get; private set; }
        public int DailyTarget { get; private set; } = 1500;

        public void Initialize(TaskQueue tasks)
        {
            Cash = 4000;
            Revenue = Payroll = HiringCosts = FiringCosts = 0;
            tasks.TaskCompleted += CompleteTask;
            Changed?.Invoke();
        }

        private void CompleteTask(TaskDefinition task)
        {
            Revenue += task.revenue;
            Cash += task.revenue;
            Changed?.Invoke();
        }

        public void RecalculatePayroll(IReadOnlyList<WorkerAgent> workers)
        {
            Payroll = 0;
            for (int i = 0; i < workers.Count; i++)
                if (workers[i] != null && !workers[i].IsFired) Payroll += workers[i].Definition.salary;
            Changed?.Invoke();
        }

        public bool CanAfford(int amount) => Cash >= amount;

        public bool PayHiring(int amount)
        {
            if (!CanAfford(amount)) return false;
            Cash -= amount;
            HiringCosts += amount;
            Changed?.Invoke();
            return true;
        }

        public void PayFiring(int amount)
        {
            Cash -= amount;
            FiringCosts += amount;
            Changed?.Invoke();
        }
    }
}
