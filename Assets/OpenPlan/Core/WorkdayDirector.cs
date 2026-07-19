using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenPlan
{
    public sealed class WorkdayDirector : MonoBehaviour
    {
        public event Action<float> Tick;
        public event Action<WorkdaySummary> Ended;
        public const float Duration = 300f;
        public float Elapsed { get; private set; }
        public bool IsEnded { get; private set; }
        public float Remaining => Mathf.Max(0f, Duration - Elapsed);
        public float Progress01 => Mathf.Clamp01(Elapsed / Duration);

        private OfficeDirector office;
        private EconomyDirector economy;
        private TaskQueue tasks;

        public void Initialize(OfficeDirector officeDirector, EconomyDirector economyDirector, TaskQueue taskQueue)
        {
            office = officeDirector;
            economy = economyDirector;
            tasks = taskQueue;
            Elapsed = 0f;
            IsEnded = false;
        }

        private void Update()
        {
            if (IsEnded || office == null) return;
            Elapsed += Time.deltaTime;
            Tick?.Invoke(Remaining);
            if (Elapsed >= Duration) Finish();
        }

        public WorkdaySummary BuildSummary()
        {
            IReadOnlyList<WorkerAgent> workers = office.Workers;
            float productivity = 0f;
            float social = 0f;
            float lowEnergy = 0f;
            WorkerAgent best = null;
            WorkerAgent least = null;
            for (int i = 0; i < workers.Count; i++)
            {
                WorkerAgent worker = workers[i];
                if (worker == null || worker.IsFired) continue;
                productivity += worker.Runtime.effectiveProductivity;
                social += worker.Runtime.socialSeconds;
                lowEnergy += worker.Runtime.lowEnergySeconds;
                if (best == null || worker.Runtime.workSeconds > best.Runtime.workSeconds) best = worker;
                if (least == null || worker.Runtime.workSeconds < least.Runtime.workSeconds) least = worker;
            }
            int active = Mathf.Max(1, office.ActiveWorkerCount);
            return new WorkdaySummary
            {
                revenue = economy.Revenue,
                payroll = economy.Payroll,
                hiringCosts = economy.HiringCosts,
                firingCosts = economy.FiringCosts,
                net = economy.Revenue - economy.Payroll - economy.HiringCosts - economy.FiringCosts,
                tasksCompleted = tasks.CompletedCount,
                averageProductivity = productivity / active,
                socialSeconds = social,
                lowEnergySeconds = lowEnergy,
                bestEmployee = best != null ? best.Definition.displayName : "—",
                leastProductiveEmployee = least != null ? least.Definition.displayName : "—",
                hires = office.Hires,
                firings = office.Firings,
                targetReached = economy.Revenue >= economy.DailyTarget
            };
        }

        public void Finish()
        {
            if (IsEnded) return;
            IsEnded = true;
            SimulationSpeedController.Instance?.SetSpeed(0f);
            Ended?.Invoke(BuildSummary());
        }
    }
}
