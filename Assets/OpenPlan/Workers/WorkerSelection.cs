using System;

namespace OpenPlan
{
    public static class WorkerSelection
    {
        public static event Action<WorkerAgent> Changed;
        public static WorkerAgent Selected { get; private set; }

        public static void Select(WorkerAgent worker)
        {
            if (Selected == worker) return;
            if (Selected != null) Selected.Visuals?.SetSelected(false);
            Selected = worker;
            if (Selected != null) Selected.Visuals?.SetSelected(true);
            Changed?.Invoke(Selected);
        }

        public static void Clear() => Select(null);
    }
}
