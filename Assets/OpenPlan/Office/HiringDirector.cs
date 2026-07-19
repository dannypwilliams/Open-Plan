namespace OpenPlan
{
    public sealed class HiringDirector : UnityEngine.MonoBehaviour
    {
        private OfficeDirector office;
        public void Initialize(OfficeDirector director) => office = director;
        public bool Hire(int candidateIndex, out string reason) => office.TryHire(candidateIndex, out reason);
    }
}
