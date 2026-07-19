namespace OpenPlan
{
    public sealed class FiringDirector : UnityEngine.MonoBehaviour
    {
        private OfficeDirector office;
        public void Initialize(OfficeDirector director) => office = director;
        public bool Fire(WorkerAgent worker, out string reason) => office.TryFire(worker, out reason);
    }
}
