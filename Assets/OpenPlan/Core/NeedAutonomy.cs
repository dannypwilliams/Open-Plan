using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenPlan
{
    public enum WorkerDecisionCategory
    {
        None, PlayerCommand, CriticalNeed, UrgentNeed, OrdinaryNeed, StressRecovery,
        Work, PhoneWork, Distraction, Social, Activity, Away, ReturningToWork,
        NavigationRecovery, Leaving
    }

    public enum ReservationStatus { None, Incoming, Arrived, Active, Suspended, Released, Expired }
    public enum DecisionFallbackLevel { None, AlternateStation, AlternateActivity, OffSite, SafeIdle, SafetyCorrection }

    [Serializable]
    public sealed class WorkerDecisionRuntime
    {
        public WorkerDecisionCategory category;
        public NeedKind need;
        public bool hasNeed;
        public PlacementActivity activity;
        public string destinationId = string.Empty;
        public float score;
        public string reason = "Choosing next task";
        public float startTime;
        public bool playerOrigin;
        public ReservationStatus reservationStatus;
        public int retryCount;
        public float lastProgressTime;
        public DecisionFallbackLevel fallbackLevel;
        public float authoritySecondsRemaining;
        public float criticalDeferralSeconds;

        public bool IsNeedRecovery => category == WorkerDecisionCategory.CriticalNeed ||
                                      category == WorkerDecisionCategory.UrgentNeed ||
                                      category == WorkerDecisionCategory.OrdinaryNeed ||
                                      category == WorkerDecisionCategory.StressRecovery;

        public void Begin(WorkerDecisionCategory nextCategory, NeedKind addressedNeed, bool addressesNeed,
            PlacementActivity selectedActivity, string selectedDestinationId, float decisionScore,
            string decisionReason, float simulationTime, bool fromPlayer,
            DecisionFallbackLevel fallback = DecisionFallbackLevel.None)
        {
            category = nextCategory;
            need = addressedNeed;
            hasNeed = addressesNeed;
            activity = selectedActivity;
            destinationId = selectedDestinationId ?? string.Empty;
            score = float.IsNaN(decisionScore) || float.IsInfinity(decisionScore) ? 0f : decisionScore;
            reason = string.IsNullOrWhiteSpace(decisionReason) ? "Choosing next task" : decisionReason;
            startTime = Mathf.Max(0f, simulationTime);
            playerOrigin = fromPlayer;
            reservationStatus = ReservationStatus.None;
            retryCount = 0;
            lastProgressTime = simulationTime;
            fallbackLevel = fallback;
            criticalDeferralSeconds = 0f;
        }

        public void Clear(string nextReason = "Choosing next task")
        {
            category = WorkerDecisionCategory.None;
            hasNeed = false;
            destinationId = string.Empty;
            score = 0f;
            reason = nextReason;
            playerOrigin = false;
            reservationStatus = ReservationStatus.None;
            retryCount = 0;
            fallbackLevel = DecisionFallbackLevel.None;
            authoritySecondsRemaining = 0f;
            criticalDeferralSeconds = 0f;
        }
    }

    [Serializable]
    public sealed class AutonomyInstrumentation
    {
        public int needEvaluations;
        public int autonomousRecoveryDecisions;
        public int playerNeedInterventions;
        public int criticalOverrides;
        public int destinationSelections;
        public int destinationRejections;
        public int reservationsCreated;
        public int reservationsReleased;
        public int reservationExpirations;
        public int repaths;
        public int alternateDestinationsSelected;
        public int offSiteFallbacks;
        public int stuckRecoveries;
        public int emergencySafetyCorrections;
        public string lastSafetyCorrection = string.Empty;
        public float urgentSeconds;
        public float criticalSeconds;
        public float workSecondsLostToRecovery;
        public float workOutput;
        public float phoneOutput;
        public float outputAfterIntervention;

        public void AccumulateNeedTime(WorkerRuntimeState state, float simulationSeconds)
        {
            if (state == null || simulationSeconds <= 0f) return;
            bool urgent = false;
            bool critical = false;
            for (int i = 0; i < NeedCatalog.All.Length; i++)
            {
                NeedDefinition definition = NeedCatalog.All[i];
                NeedStatus status = definition.Status(state.GetNeed(definition.Kind));
                if (status == NeedStatus.Critical) critical = true;
                else if (status == NeedStatus.Urgent) urgent = true;
            }
            if (critical) criticalSeconds += simulationSeconds;
            else if (urgent) urgentSeconds += simulationSeconds;
        }
    }

    public sealed class NeedDestinationCandidate
    {
        public PlacementZone Zone;
        public PlacementActivity Activity;
        public string StableId;
        public bool Enabled = true;
        public bool Reachable = true;
        public bool HasCapacity = true;
        public bool CooldownReady = true;
        public bool Affordable = true;
        public float PathCost;
        public float Duration;
        public float CashCost;
        public int ZonePriority;
        public int Reservations;
        public float PreferenceBonus;
        public Vector3[] Path;
        public DecisionFallbackLevel Fallback;
    }

    public sealed class NeedDecisionPlan
    {
        public NeedKind Need;
        public NeedStatus Status;
        public WorkerDecisionCategory Category;
        public NeedDestinationCandidate Candidate;
        public float Score;
        public string Reason;
        public Vector3[] Path;
    }

    /// <summary>Central deterministic timing, priority, mapping, scoring, and hysteresis rules.</summary>
    public static class NeedAutonomyRules
    {
        public const float HealthyEvaluationMin = 3f;
        public const float HealthyEvaluationMax = 6f;
        public const float CautionEvaluationMin = 2f;
        public const float CautionEvaluationMax = 4f;
        public const float UrgentEvaluationMin = 1f;
        public const float UrgentEvaluationMax = 2f;
        public const float CriticalEvaluationMax = 1f;
        public const float CriticalBathroomPlayerDeferral = 3f;
        public const float CriticalHungerPlayerDeferral = 5f;
        public const float NearCompletionProtection = 3f;
        public const float PlayerAuthorityDefault = 22f;
        public const float NeedExitUrgentMargin = .06f;
        public const int MaximumRepathAttempts = 3;
        public const float ReservationLifetime = 18f;
        public const float ProgressTimeout = 2f;

        public static float InitialEvaluationDelay(string stableWorkerId, int campaignSeed)
            => .15f + StableUnit(stableWorkerId, campaignSeed, 0) * 2.65f;

        public static float NextEvaluationDelay(WorkerRuntimeState state, string stableWorkerId,
            int campaignSeed, int evaluationIndex)
        {
            NeedStatus worst = WorstStatus(state);
            float min;
            float max;
            switch (worst)
            {
                case NeedStatus.Critical: min = .35f; max = CriticalEvaluationMax; break;
                case NeedStatus.Urgent: min = UrgentEvaluationMin; max = UrgentEvaluationMax; break;
                case NeedStatus.Caution: min = CautionEvaluationMin; max = CautionEvaluationMax; break;
                default: min = HealthyEvaluationMin; max = HealthyEvaluationMax; break;
            }
            return Mathf.Lerp(min, max, StableUnit(stableWorkerId, campaignSeed, evaluationIndex + 1));
        }

        public static NeedStatus WorstStatus(WorkerRuntimeState state)
        {
            NeedStatus worst = NeedStatus.Healthy;
            if (state == null) return worst;
            for (int i = 0; i < NeedCatalog.All.Length; i++)
            {
                NeedDefinition definition = NeedCatalog.All[i];
                NeedStatus status = definition.Status(state.GetNeed(definition.Kind));
                if (status > worst) worst = status;
            }
            return worst;
        }

        public static bool TrySelectPriorityNeed(WorkerRuntimeState state, out NeedKind need,
            out NeedStatus status, out WorkerDecisionCategory category, out float priority)
        {
            need = NeedKind.Happiness;
            status = NeedStatus.Healthy;
            category = WorkerDecisionCategory.None;
            priority = float.MinValue;
            if (state == null) return false;
            for (int i = 0; i < NeedCatalog.All.Length; i++)
            {
                NeedDefinition definition = NeedCatalog.All[i];
                NeedStatus candidateStatus = definition.Status(state.GetNeed(definition.Kind));
                if (candidateStatus == NeedStatus.Healthy) continue;
                float candidatePriority = Priority(definition.Kind, candidateStatus) +
                                          Severity01(state, definition.Kind) * 10f;
                if (candidatePriority <= priority) continue;
                need = definition.Kind;
                status = candidateStatus;
                priority = candidatePriority;
            }
            if (priority > float.MinValue)
            {
                category = status == NeedStatus.Critical ? WorkerDecisionCategory.CriticalNeed :
                    status == NeedStatus.Urgent ? WorkerDecisionCategory.UrgentNeed : WorkerDecisionCategory.OrdinaryNeed;
                return true;
            }
            if (state.stress > .70f)
            {
                need = NeedKind.Happiness;
                status = NeedStatus.Caution;
                category = WorkerDecisionCategory.StressRecovery;
                priority = 300f + state.stress * 10f;
                return true;
            }
            return false;
        }

        public static bool IsEmergency(NeedKind need, NeedStatus status)
            => status == NeedStatus.Critical || status == NeedStatus.Urgent &&
               (need == NeedKind.Bathroom || need == NeedKind.Hunger);

        public static bool CanOverridePlayerAuthority(NeedKind need, NeedStatus status, float deferredSeconds)
        {
            if (status != NeedStatus.Critical) return false;
            if (need == NeedKind.Bathroom) return deferredSeconds >= CriticalBathroomPlayerDeferral;
            if (need == NeedKind.Hunger) return deferredSeconds >= CriticalHungerPlayerDeferral;
            return deferredSeconds >= CriticalHungerPlayerDeferral;
        }

        public static bool NeedExitedUrgentRange(WorkerRuntimeState state, NeedKind need)
        {
            if (state == null) return true;
            NeedDefinition definition = NeedCatalog.Get(need);
            float value = state.GetNeed(need);
            return definition.HighIsGood ? value >= definition.UrgentThreshold + NeedExitUrgentMargin :
                value <= definition.UrgentThreshold - NeedExitUrgentMargin;
        }

        public static float Score(WorkerRuntimeState state, NeedKind need, NeedStatus status,
            NeedDestinationCandidate candidate, bool caffeinated = false)
        {
            if (state == null || candidate == null || !candidate.Enabled || !candidate.Reachable ||
                !candidate.HasCapacity || !candidate.CooldownReady || !candidate.Affordable) return float.NegativeInfinity;
            float addressedBenefit = ActivityBenefit(candidate.Activity, need, caffeinated);
            if (addressedBenefit <= 0f) return float.NegativeInfinity;
            float score = Priority(need, status) + Severity01(state, need) * 90f + addressedBenefit * 230f;
            float multi = 0f;
            for (int i = 0; i < NeedCatalog.All.Length; i++)
            {
                NeedKind other = NeedCatalog.All[i].Kind;
                if (other == need) continue;
                float benefit = ActivityBenefit(candidate.Activity, other, caffeinated);
                if (benefit > 0f) multi += benefit * Severity01(state, other);
            }
            score += multi * 72f;
            if (candidate.Activity == PlacementActivity.Rest) score += Mathf.Max(0f, state.stress - .45f) * 95f;
            if (candidate.Activity == PlacementActivity.GetCoffee && state.bathroom > NeedCatalog.UrgencyCaution)
                score -= Mathf.Lerp(18f, 105f, state.bathroom);
            score -= Mathf.Max(0f, candidate.PathCost) * 2.2f;
            score -= Mathf.Max(0f, candidate.Duration) * .32f;
            score -= Mathf.Max(0f, candidate.CashCost) * .18f;
            score -= Mathf.Max(0, candidate.Reservations) * 24f;
            score += candidate.ZonePriority * .01f;
            score += candidate.PreferenceBonus;
            if (candidate.Fallback == DecisionFallbackLevel.OffSite) score -= 75f;
            return float.IsNaN(score) || float.IsInfinity(score) ? float.NegativeInfinity : score;
        }

        public static NeedDestinationCandidate SelectBest(WorkerRuntimeState state, NeedKind need,
            NeedStatus status, IList<NeedDestinationCandidate> candidates, bool caffeinated = false)
        {
            NeedDestinationCandidate best = null;
            float bestScore = float.NegativeInfinity;
            if (candidates == null) return null;
            for (int i = 0; i < candidates.Count; i++)
            {
                NeedDestinationCandidate candidate = candidates[i];
                float score = Score(state, need, status, candidate, caffeinated);
                if (score > bestScore + .0001f || Mathf.Abs(score - bestScore) <= .0001f &&
                    best != null && string.CompareOrdinal(candidate.StableId, best.StableId) < 0)
                {
                    best = candidate;
                    bestScore = score;
                }
            }
            return best;
        }

        public static float ActivityBenefit(PlacementActivity activity, NeedKind need, bool caffeinated = false)
        {
            switch (activity)
            {
                case PlacementActivity.Rest:
                    if (need == NeedKind.Energy) return .32f;
                    if (need == NeedKind.Happiness) return .14f;
                    if (need == NeedKind.Inspiration) return .12f;
                    return 0f;
                case PlacementActivity.GetWater:
                    if (need == NeedKind.Energy) return .06f;
                    if (need == NeedKind.Happiness) return .04f;
                    if (need == NeedKind.Inspiration) return .03f;
                    return 0f;
                case PlacementActivity.BuySnack:
                    if (need == NeedKind.Hunger) return .72f;
                    if (need == NeedKind.Happiness) return .08f;
                    if (need == NeedKind.Energy) return .06f;
                    return 0f;
                case PlacementActivity.GetCoffee:
                    if (need == NeedKind.Energy) return caffeinated ? .50f : .34f;
                    if (need == NeedKind.Inspiration) return .12f;
                    if (need == NeedKind.Happiness) return .04f;
                    return 0f;
                case PlacementActivity.Smoke:
                    if (need == NeedKind.Happiness) return .07f;
                    if (need == NeedKind.Inspiration) return .06f;
                    return 0f;
                case PlacementActivity.UseRestroom:
                    return need == NeedKind.Bathroom ? .78f : 0f;
                case PlacementActivity.LeaveOffice:
                    if (need == NeedKind.Energy) return .38f;
                    if (need == NeedKind.Happiness) return .15f;
                    if (need == NeedKind.Inspiration) return .16f;
                    if (need == NeedKind.Hunger) return .35f;
                    if (need == NeedKind.Bathroom) return .40f;
                    return 0f;
                default: return 0f;
            }
        }

        public static bool ActivityImproves(PlacementActivity activity, NeedKind need)
            => ActivityBenefit(activity, need) > 0f;

        public static float ActivityDuration(PlacementActivity activity)
        {
            switch (activity)
            {
                case PlacementActivity.Rest: return ActivityRules.RestDuration;
                case PlacementActivity.GetWater: return ActivityRules.WaterDuration;
                case PlacementActivity.BuySnack: return ActivityRules.VendingDuration;
                case PlacementActivity.GetCoffee: return ActivityRules.CoffeeDuration;
                case PlacementActivity.Smoke: return ActivityRules.SmokingDuration;
                case PlacementActivity.UseRestroom: return ActivityRules.RestroomDuration;
                case PlacementActivity.LeaveOffice: return ActivityRules.AwayDuration;
                default: return 0f;
            }
        }

        public static float Priority(NeedKind need, NeedStatus status)
        {
            if (status == NeedStatus.Critical)
            {
                if (need == NeedKind.Bathroom) return 1100f;
                if (need == NeedKind.Hunger) return 1000f;
                if (need == NeedKind.Energy) return 900f;
                return 800f;
            }
            if (status == NeedStatus.Urgent)
            {
                if (need == NeedKind.Bathroom) return 700f;
                if (need == NeedKind.Hunger) return 600f;
                if (need == NeedKind.Energy) return 500f;
                return 400f;
            }
            return status == NeedStatus.Caution ? 200f : 0f;
        }

        public static float Severity01(WorkerRuntimeState state, NeedKind need)
        {
            if (state == null) return 0f;
            NeedDefinition definition = NeedCatalog.Get(need);
            float value = Mathf.Clamp01(state.GetNeed(need));
            return definition.HighIsGood ? 1f - value : value;
        }

        private static float StableUnit(string value, int seed, int sequence)
        {
            unchecked
            {
                uint hash = 2166136261u ^ (uint)seed;
                string text = value ?? string.Empty;
                for (int i = 0; i < text.Length; i++) { hash ^= text[i]; hash *= 16777619u; }
                hash ^= (uint)sequence * 2654435761u;
                hash = hash * 1664525u + 1013904223u;
                return (hash & 0x00ffffffu) / 16777215f;
            }
        }
    }

    public sealed class ActivityReservation
    {
        public WorkerAgent Worker { get; internal set; }
        public PlacementZone Zone { get; internal set; }
        public PlacementActivity Activity { get; internal set; }
        public float StartTime { get; internal set; }
        public float ExpirationTime { get; internal set; }
        public bool Arrived { get; internal set; }
        public bool Started { get; internal set; }
        public string ReleaseReason { get; internal set; }
        public ReservationStatus Status { get; internal set; }
    }

    /// <summary>Scene-owned reservation lifecycle. It never survives an OfficeDirector.</summary>
    public sealed class ActivityReservationService
    {
        private readonly Dictionary<WorkerAgent, ActivityReservation> byWorker =
            new Dictionary<WorkerAgent, ActivityReservation>();
        private readonly List<ActivityReservation> scratch = new List<ActivityReservation>();
        private readonly AutonomyInstrumentation counters;

        public ActivityReservationService(AutonomyInstrumentation instrumentation)
        {
            counters = instrumentation;
        }

        public int Count => byWorker.Count;
        public IEnumerable<ActivityReservation> Active => byWorker.Values;

        public ActivityReservation Get(WorkerAgent worker)
            => !ReferenceEquals(worker, null) && byWorker.TryGetValue(worker, out ActivityReservation value) ? value : null;

        public bool TryCreate(WorkerAgent worker, PlacementZone zone, PlacementActivity activity,
            float simulationTime, out ActivityReservation reservation, out string reason)
        {
            reservation = null;
            if (worker == null || zone == null) { reason = "Reservation requires a worker and destination."; return false; }
            ActivityReservation existing = Get(worker);
            if (existing != null)
            {
                if (existing.Zone == zone && existing.Activity == activity) { reservation = existing; reason = null; return true; }
                Release(worker, "Rerouted to another destination");
            }
            if (!zone.TryReserveIncoming(worker, out reason)) return false;
            reservation = new ActivityReservation
            {
                Worker = worker,
                Zone = zone,
                Activity = activity,
                StartTime = simulationTime,
                ExpirationTime = simulationTime + NeedAutonomyRules.ReservationLifetime,
                Status = ReservationStatus.Incoming
            };
            byWorker.Add(worker, reservation);
            counters.reservationsCreated++;
            if (worker.Runtime != null) worker.Runtime.decision.reservationStatus = ReservationStatus.Incoming;
            return true;
        }

        public bool MarkArrived(WorkerAgent worker, out string reason)
        {
            ActivityReservation reservation = Get(worker);
            if (reservation == null) { reason = "Reservation expired before arrival."; return false; }
            if (!reservation.Zone.TryOccupy(worker, out reason)) { Release(worker, reason); return false; }
            reservation.Arrived = true;
            reservation.Status = ReservationStatus.Arrived;
            reservation.ExpirationTime += NeedAutonomyRules.ReservationLifetime;
            if (worker.Runtime != null) worker.Runtime.decision.reservationStatus = ReservationStatus.Arrived;
            return true;
        }

        public void MarkStarted(WorkerAgent worker)
        {
            ActivityReservation reservation = Get(worker);
            if (reservation == null) return;
            reservation.Started = true;
            reservation.Status = ReservationStatus.Active;
            if (worker.Runtime != null) worker.Runtime.decision.reservationStatus = ReservationStatus.Active;
        }

        public void Suspend(WorkerAgent worker)
        {
            ActivityReservation reservation = Get(worker);
            if (reservation == null) return;
            reservation.Status = ReservationStatus.Suspended;
            if (worker.Runtime != null) worker.Runtime.decision.reservationStatus = ReservationStatus.Suspended;
        }

        public void Resume(WorkerAgent worker)
        {
            ActivityReservation reservation = Get(worker);
            if (reservation == null) return;
            reservation.Status = reservation.Started ? ReservationStatus.Active :
                reservation.Arrived ? ReservationStatus.Arrived : ReservationStatus.Incoming;
            if (worker.Runtime != null) worker.Runtime.decision.reservationStatus = reservation.Status;
        }

        public bool Release(WorkerAgent worker, string reason)
        {
            if (ReferenceEquals(worker, null) ||
                !byWorker.TryGetValue(worker, out ActivityReservation reservation)) return false;
            return ReleaseReservation(reservation, reason);
        }

        public void Tick(float simulationTime)
        {
            scratch.Clear();
            foreach (ActivityReservation reservation in byWorker.Values)
                if (reservation == null || reservation.Worker == null || reservation.Zone == null ||
                    !reservation.Zone.IsZoneEnabled || reservation.Status != ReservationStatus.Suspended &&
                    simulationTime >= reservation.ExpirationTime) scratch.Add(reservation);
            for (int i = 0; i < scratch.Count; i++)
            {
                ActivityReservation reservation = scratch[i];
                if (reservation == null) continue;
                counters.reservationExpirations++;
                reservation.Status = ReservationStatus.Expired;
                WorkerAgent worker = reservation.Worker;
                ReleaseReservation(reservation, "Reservation expired");
                if (worker != null) worker.NotifyReservationLost("Destination became unavailable");
            }
        }

        public void ReleaseForZone(PlacementZone zone, string reason)
        {
            scratch.Clear();
            foreach (ActivityReservation reservation in byWorker.Values)
                if (reservation != null && reservation.Zone == zone) scratch.Add(reservation);
            for (int i = 0; i < scratch.Count; i++)
            {
                WorkerAgent worker = scratch[i].Worker;
                ReleaseReservation(scratch[i], reason);
                if (worker != null) worker.NotifyReservationLost(reason);
            }
        }

        public void Clear(string reason)
        {
            scratch.Clear();
            foreach (ActivityReservation reservation in byWorker.Values) scratch.Add(reservation);
            for (int i = 0; i < scratch.Count; i++) ReleaseReservation(scratch[i], reason);
        }

        private bool ReleaseReservation(ActivityReservation reservation, string reason)
        {
            if (reservation == null) return false;
            WorkerAgent worker = reservation.Worker;
            if (!ReferenceEquals(worker, null)) byWorker.Remove(worker);
            if (reservation.Zone != null) reservation.Zone.ReleaseIncoming(worker);
            reservation.ReleaseReason = reason ?? "Released";
            reservation.Status = ReservationStatus.Released;
            if (worker != null && worker.Runtime != null)
                worker.Runtime.decision.reservationStatus = ReservationStatus.Released;
            counters.reservationsReleased++;
            return true;
        }
    }
}
