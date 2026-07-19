using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenPlan
{
    public sealed class TaskQueue : MonoBehaviour
    {
        public event Action<TaskRuntime> TaskChanged;
        public event Action<TaskDefinition> TaskCompleted;
        public TaskRuntime Current { get; private set; }
        public int CompletedCount { get; private set; }

        private readonly Queue<TaskDefinition> queue = new Queue<TaskDefinition>();
        private SeededRandomService random;

        public void Initialize(SeededRandomService seeded)
        {
            random = seeded;
            CompletedCount = 0;
            queue.Clear();
            string[] names = { "Client report", "Data cleanup", "Budget review", "Customer proposal", "Quarterly analysis", "Team scheduling", "Presentation polish" };
            for (int i = 0; i < 8; i++)
            {
                queue.Enqueue(new TaskDefinition
                {
                    title = names[i % names.Length],
                    workRequired = random.Range(74f, 108f),
                    revenue = random.Range(180, 325),
                    preference = i % 3 == 0 ? "Deep focus" : i % 3 == 1 ? "Collaboration" : "General",
                    priority = 3 - (i % 3)
                });
            }
            Advance();
        }

        public void Contribute(float work)
        {
            if (Current == null || work <= 0f) return;
            Current.progress += work;
            if (Current.progress < Current.definition.workRequired) return;
            TaskDefinition finished = Current.definition;
            CompletedCount++;
            TaskCompleted?.Invoke(finished);
            if (queue.Count < 4)
            {
                queue.Enqueue(new TaskDefinition
                {
                    title = "Follow-up memo",
                    workRequired = random.Range(82f, 118f),
                    revenue = random.Range(195, 310),
                    preference = "General",
                    priority = 1
                });
            }
            Advance();
        }

        private void Advance()
        {
            Current = queue.Count > 0 ? new TaskRuntime { definition = queue.Dequeue(), progress = 0f } : null;
            TaskChanged?.Invoke(Current);
        }

        public float Progress01 => Current == null ? 0f : Mathf.Clamp01(Current.progress / Current.definition.workRequired);
    }
}
