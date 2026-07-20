using System.Collections.Generic;
using UnityEngine;

namespace OpenPlan
{
    [RequireComponent(typeof(CapsuleCollider))]
    public sealed class WorkerAgent : MonoBehaviour
    {
        public WorkerDefinition Definition { get; private set; }
        public WorkerRuntimeState Runtime { get; private set; }
        public Workstation Desk { get; private set; }
        public WorkerVisuals Visuals { get; private set; }
        public bool IsFired { get; private set; }
        public bool IsMoving { get; private set; }
        public bool IsPlayerCarried { get; private set; }
        public bool IsLeavingCompany => IsFired || (Runtime != null &&
            (Runtime.behavior == WorkerState.FiredReaction || Runtime.behavior == WorkerState.PackDesk ||
             Runtime.behavior == WorkerState.CarryBox || Runtime.behavior == WorkerState.ExitOffice));
        public bool IsAway => Runtime != null && (Runtime.behavior == WorkerState.EnterOffice ||
            Runtime.behavior == WorkerState.WalkOutForAway ||
            Runtime.behavior == WorkerState.Away || Runtime.behavior == WorkerState.ReturnFromAway);
        public bool IsVisibleInOffice { get; private set; } = true;
        public bool HasSmokingProp => cigaretteProp != null;
        public bool HasSmokeParticles => smokeParticles != null;
        public bool LastVendingMalfunction { get; private set; }
        public int VendingCharges { get; private set; }
        public bool HadWaterSocialOpportunity { get; private set; }
        public string AwayReasonLabel => Runtime == null ? string.Empty : PrettyAwayReason(Runtime.awayReason);
        public float ActivitySecondsRemaining => Mathf.Max(0f, stateLimit - stateTime);
        public Vector3 PreCarryPosition => carrySnapshotValid ? preCarryPosition : transform.position;
        public PlacementZone ActivePlacementZone { get; private set; }
        public WorkerCommand LastPlayerCommand { get; private set; }
        public GroundPlacementCommand LastGroundPlacementCommand { get; private set; }
        public WorkerState LastRestoredCarryState { get; private set; }
        public float Productivity => Runtime.effectiveProductivity;
        public string PersonalityLabel => Definition == null ? string.Empty : PersonalityRules.For(Definition.trait).Label;
        public bool IsPhoneWorking => Runtime != null && Runtime.behavior == WorkerState.Unassigned && Desk == null &&
                                      !IsPlayerCarried && !IsFired && !IsAway;
        public string CurrentActivityLabel => Runtime == null ? string.Empty :
            IsPhoneWorking ? "Working from phone" :
            Runtime.behavior == WorkerState.WalkToPlacement && commandedZone != null ?
                "Going to " + commandedZone.ActivityLabel :
            Runtime.behavior == WorkerState.Unassigned && Desk != null ? "Choosing next task" : PrettyState(Runtime.behavior);
        public string CurrentDestinationLabel => DestinationLabel();
        public WorkerDecisionRuntime Decision => Runtime?.decision;
        public string DecisionReasonLabel => Runtime?.decision?.reason ?? "Choosing next task";
        public string DecisionOwnerLabel => Runtime?.decision == null ? "Autonomous" :
            Runtime.decision.playerOrigin ? "Player instruction" : "Autonomous";
        public string AddressedNeedLabel => Runtime?.decision != null && Runtime.decision.hasNeed ?
            NeedCatalog.Get(Runtime.decision.need).DisplayName : "None";
        public string ReservationLabel => Runtime?.decision == null ||
            Runtime.decision.reservationStatus == ReservationStatus.None ? "No reservation" :
            Runtime.decision.reservationStatus.ToString();
        public string LastNavigationRecoveryReason { get; private set; }
        public DistractionKind CurrentDistraction { get; private set; }
        public bool HasPlayerCommandAuthority { get; private set; }
        public float CoffeeCooldownRemaining => coffeeCooldown;
        public float StateAge => stateTime;
        public IReadOnlyCollection<WorkerState> ObservedStates => observedStates;
        public IReadOnlyDictionary<DistractionKind, int> DistractionCounts => distractionCounts;

        private OfficeDirector office;
        private TaskQueue tasks;
        private SeededRandomService random;
        private Vector3 target;
        private WorkerAgent socialPartner;
        private float stateTime;
        private float stateLimit;
        private float decisionTime;
        private float coffeeCooldown;
        private float socialCooldown;
        private float earningsEmoteCooldown;
        private float autonomousActivityDuration;
        private float stuckTime;
        private Vector3 previousPosition;
        private Vector3 lastConfirmedReachablePosition;
        private Vector3 navigationDestination;
        private Vector3[] navigationPath;
        private int navigationPathIndex;
        private int navigationVersion;
        private int evaluationIndex;
        private float needEvaluationTimer;
        private GameObject carriedBox;
        private PlacementZone commandedZone;
        private PlacementActivity? activeActivity;
        private PlacementActivity? pendingPlacementActivity;
        private PlacementZone lastAutonomousZone;
        private bool activityEffectApplied;
        private bool vendingChargedForCurrentUse;
        private bool? nextVendingMalfunctionOverride;
        private Transform shakingMachine;
        private Vector3 shakingMachineBasePosition;
        private GameObject cigaretteProp;
        private ParticleSystem smokeParticles;
        private bool carrySnapshotValid;
        private Vector3 preCarryPosition;
        private Quaternion preCarryRotation;
        private WorkerState preCarryState;
        private Vector3 preCarryTarget;
        private WorkerAgent preCarrySocialPartner;
        private float preCarryStateTime;
        private float preCarryStateLimit;
        private float preCarryDecisionTime;
        private bool preCarryWasMoving;
        private bool preCarryCommandAuthority;
        private DistractionKind preCarryDistraction;
        private readonly HashSet<WorkerState> observedStates = new HashSet<WorkerState>();
        private readonly Dictionary<DistractionKind, int> distractionCounts = new Dictionary<DistractionKind, int>();

        public void Initialize(OfficeDirector director, WorkerDefinition definition, Workstation desk, Vector3 spawn)
        {
            office = director;
            tasks = director.Tasks;
            random = director.Random;
            Definition = definition.Clone();
            Runtime = new WorkerRuntimeState();
            NeedCatalog.Initialize(Runtime, Definition.displayName, random.Seed);
            Runtime.socialNeed = random.Range(.05f, .32f);
            transform.position = spawn;
            previousPosition = spawn;
            lastConfirmedReachablePosition = spawn;
            needEvaluationTimer = NeedAutonomyRules.InitialEvaluationDelay(Definition.displayName, random.Seed);
            Visuals = gameObject.AddComponent<WorkerVisuals>();
            Visuals.Initialize(Definition.displayName, Definition.clothing, director.Catalog.GetMaterial("cyan"));
            CapsuleCollider capsule = GetComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, .85f, 0f);
            capsule.height = 1.8f;
            capsule.radius = .42f;
            SetDesk(desk);
            ActivePlacementZone = desk;
            SetState(WorkerState.EnterOffice, .75f);
        }

        public void SetDesk(Workstation desk)
        {
            Desk = desk;
            if (desk != null && Runtime != null && Runtime.behavior == WorkerState.Unassigned && !IsPlayerCarried)
                ReturnToDesk();
        }

        private void Update()
        {
            if (Runtime == null || office == null) return;
            if (IsPlayerCarried)
            {
                IsMoving = false;
                Visuals?.Tick(PresentationState(), false, Productivity);
                return;
            }

            float dt = Time.deltaTime;
            if (dt <= 0f)
            {
                Visuals?.Tick(PresentationState(), IsMoving, Productivity);
                return;
            }

            stateTime += dt;
            decisionTime -= dt;
            needEvaluationTimer -= dt;
            if (Runtime.decision.authoritySecondsRemaining > 0f)
                Runtime.decision.authoritySecondsRemaining = Mathf.Max(0f, Runtime.decision.authoritySecondsRemaining - dt);
            coffeeCooldown = Mathf.Max(0f, coffeeCooldown - dt);
            socialCooldown = Mathf.Max(0f, socialCooldown - dt);
            earningsEmoteCooldown = Mathf.Max(0f, earningsEmoteCooldown - dt);
            Runtime.waterCooldown = Mathf.Max(0f, Runtime.waterCooldown - dt);
            Runtime.vendingCooldown = Mathf.Max(0f, Runtime.vendingCooldown - dt);
            Runtime.smokingCooldown = Mathf.Max(0f, Runtime.smokingCooldown - dt);
            Runtime.focusedWorkSecondsRemaining = Mathf.Max(0f, Runtime.focusedWorkSecondsRemaining - dt);
            Runtime.socialNeed = Mathf.Clamp01(Runtime.socialNeed + dt *
                (Definition.trait == WorkerTrait.Social ? .010f : .006f));
            if (Runtime.energy < .34f) Runtime.lowEnergySeconds += dt;
            if (CurrentDistraction != DistractionKind.None)
            {
                Runtime.distractionSeconds += dt;
                Runtime.stress = Mathf.Clamp01(Runtime.stress - dt * .0015f *
                    PersonalityRules.For(Definition.trait).AvoidanceStressRecovery);
            }

            float needDelta = Runtime.behavior == WorkerState.Away ? Mathf.Min(dt, Runtime.awaySecondsRemaining) : dt;
            float workStressMultiplier = Runtime.behavior == WorkerState.Unassigned && Desk == null ? .75f :
                Desk == null ? 1f : Mathf.Lerp(.75f, 1.45f, Desk.Noise) *
                PersonalityRules.For(Definition.trait).NoiseStressMultiplier;
            NeedSimulation.Tick(Runtime, Runtime.behavior, needDelta, workStressMultiplier);
            office.AutonomyCounters.AccumulateNeedTime(Runtime, dt);
            UpdateProductivity();
            TickNeedAutonomy(dt);
            TickState(dt);
            Visuals?.Tick(PresentationState(), IsMoving, Productivity);
        }

        private WorkerState PresentationState()
            => IsPhoneWorking ? WorkerState.LookAtPhone : Runtime.behavior;

        private void TickNeedAutonomy(float dt)
        {
            NeedStatus worst = NeedAutonomyRules.WorstStatus(Runtime);
            if (worst == NeedStatus.Critical && needEvaluationTimer > NeedAutonomyRules.CriticalEvaluationMax)
                needEvaluationTimer = NeedAutonomyRules.CriticalEvaluationMax;
            if (needEvaluationTimer > 0f || IsPlayerCarried || IsLeavingCompany || IsAway) return;
            needEvaluationTimer = NeedAutonomyRules.NextEvaluationDelay(Runtime, Definition.displayName,
                random.Seed, evaluationIndex++);
            if (!office.TrySelectNeedRecovery(this, out NeedDecisionPlan plan)) return;

            if (Runtime.decision.IsNeedRecovery && commandedZone != null)
            {
                NeedStatus currentStatus = NeedCatalog.Status(Runtime, Runtime.decision.need);
                float currentPriority = NeedAutonomyRules.Priority(Runtime.decision.need, currentStatus);
                float nextPriority = NeedAutonomyRules.Priority(plan.Need, plan.Status);
                if (Runtime.decision.need == plan.Need || nextPriority <= currentPriority + .01f) return;
            }

            bool nearlyComplete = IsRecoveryActivityState(Runtime.behavior) &&
                                  ActivitySecondsRemaining <= NeedAutonomyRules.NearCompletionProtection;
            if (nearlyComplete) return;
            if (HasPlayerCommandAuthority && activeActivity.HasValue &&
                NeedAutonomyRules.ActivityImproves(activeActivity.Value, plan.Need)) return;
            if (HasPlayerCommandAuthority && Runtime.decision.authoritySecondsRemaining > 0f)
            {
                Runtime.decision.criticalDeferralSeconds += Mathf.Min(dt + needEvaluationTimer,
                    NeedAutonomyRules.CriticalEvaluationMax);
                if (!NeedAutonomyRules.CanOverridePlayerAuthority(plan.Need, plan.Status,
                        Runtime.decision.criticalDeferralSeconds)) return;
                office.AutonomyCounters.criticalOverrides++;
                office.ShowNotice(Definition.displayName + " abandoned the instruction - " +
                                  NeedCatalog.Get(plan.Need).DisplayName.ToLowerInvariant() + " could not wait.");
            }
            BeginNeedRecovery(plan);
        }

        private bool BeginNeedRecovery(NeedDecisionPlan plan)
        {
            if (plan?.Candidate?.Zone == null) return false;
            bool rerouting = Runtime.decision.category == WorkerDecisionCategory.NavigationRecovery;
            ReleaseSocialForInterruption();
            InterruptCurrentActivity();
            office.ReleaseTransientPlacement(this);
            if (!office.TryReserveNeedRecovery(this, plan, out string reason))
            {
                Runtime.decision.reason = "Rerouting: " + (reason ?? "destination unavailable");
                Runtime.decision.fallbackLevel = DecisionFallbackLevel.AlternateStation;
                office.AutonomyCounters.alternateDestinationsSelected++;
                return false;
            }
            commandedZone = plan.Candidate.Zone;
            pendingPlacementActivity = plan.Candidate.Activity;
            activeActivity = null;
            activityEffectApplied = false;
            vendingChargedForCurrentUse = false;
            autonomousActivityDuration = NeedAutonomyRules.ActivityDuration(plan.Candidate.Activity);
            SetActivePlacementZone(commandedZone);
            Runtime.decision.Begin(plan.Category, plan.Need, true, plan.Candidate.Activity,
                plan.Candidate.StableId, plan.Score, rerouting ? "Rerouting: " + plan.Reason : plan.Reason,
                office.SimulationTime, false, rerouting && plan.Candidate.Fallback == DecisionFallbackLevel.None ?
                    DecisionFallbackLevel.AlternateStation : plan.Candidate.Fallback);
            Runtime.decision.reservationStatus = plan.Candidate.Activity == PlacementActivity.LeaveOffice ?
                ReservationStatus.None : ReservationStatus.Incoming;
            Runtime.autonomyDecisions++;
            navigationPath = plan.Path;
            navigationPathIndex = 0;
            navigationDestination = commandedZone.PositionFor(this);
            navigationVersion = office.Navigation.Version;
            ShowNeedDecisionEmote(plan.Need, plan.Candidate.Activity);
            SetState(plan.Candidate.Activity == PlacementActivity.GetCoffee ?
                WorkerState.SeekCoffee : WorkerState.WalkToPlacement, 30f);
            return true;
        }

        private void ShowNeedDecisionEmote(NeedKind need, PlacementActivity activity)
        {
            StatusEmote emote = need == NeedKind.Bathroom ? StatusEmote.Restroom :
                need == NeedKind.Hunger ? StatusEmote.Snack :
                need == NeedKind.Energy ? StatusEmote.Tired :
                need == NeedKind.Happiness ? StatusEmote.Sad : StatusEmote.Focus;
            if (activity == PlacementActivity.GetWater) emote = StatusEmote.Water;
            else if (activity == PlacementActivity.Smoke) emote = StatusEmote.Cigarette;
            Visuals?.ShowEmote(emote, 2.2f);
        }

        private static bool IsRecoveryActivityState(WorkerState state)
            => state == WorkerState.TakeBreak || state == WorkerState.UseWaterCooler ||
               state == WorkerState.UseCoffeeMachine || state == WorkerState.BuySnack ||
               state == WorkerState.Smoke || state == WorkerState.UseRestroom || state == WorkerState.Away;

        private void ReleaseSocialForInterruption()
        {
            WorkerAgent partner = socialPartner;
            socialPartner = null;
            if (partner != null) partner.OnSocialInterrupted(this);
        }

        private void OnSocialInterrupted(WorkerAgent partner)
        {
            if (socialPartner != partner) return;
            socialPartner = null;
            if (Runtime != null && (Runtime.behavior == WorkerState.Socialize ||
                                    Runtime.behavior == WorkerState.SeekCoworker)) ReturnToDesk();
        }

        private void TickState(float dt)
        {
            switch (Runtime.behavior)
            {
                case WorkerState.EnterOffice:
                    if (stateTime >= stateLimit) GoToDesk();
                    break;
                case WorkerState.Unassigned:
                    IsMoving = false;
                    if (Desk == null)
                    {
                        TickPhoneWork(dt);
                        if (stateTime >= stateLimit) DecidePhoneWork();
                    }
                    if (stateTime >= stateLimit)
                    {
                        if (Desk != null) ReturnToDesk();
                    }
                    break;
                case WorkerState.WalkToDesk:
                case WorkerState.ReturnToDesk:
                    MoveTowards(Desk != null ? Desk.WorkPoint.position : transform.position, dt, WorkerState.Work);
                    break;
                case WorkerState.WalkToPlacement:
                    if (commandedZone == null) ReturnToDesk();
                    else MoveTowards(commandedZone.PositionFor(this), dt, WorkerState.WalkToPlacement);
                    break;
                case WorkerState.Work:
                    TickWork(dt);
                    break;
                case WorkerState.IdleAtDesk:
                    Runtime.stress = Mathf.Clamp01(Runtime.stress - dt * .012f);
                    if (stateTime >= stateLimit) Decide();
                    break;
                case WorkerState.SeekCoffee:
                    MoveTowards(office.Coffee.UsePoint.position, dt, WorkerState.UseCoffeeMachine);
                    break;
                case WorkerState.UseCoffeeMachine:
                    if (stateTime >= stateLimit)
                    {
                        if (!activityEffectApplied)
                        {
                            ActivityRules.ApplyCoffee(Runtime, Definition.trait == WorkerTrait.Caffeinated);
                            activityEffectApplied = true;
                        }
                        coffeeCooldown = Definition.trait == WorkerTrait.Caffeinated ? 34f : 52f;
                        ReturnToDesk();
                    }
                    break;
                case WorkerState.SeekWater:
                    MoveTowards(office.Water.UsePoint.position, dt, WorkerState.UseWaterCooler);
                    break;
                case WorkerState.UseWaterCooler:
                    if (stateTime >= stateLimit) CompleteWater();
                    break;
                case WorkerState.SeekCoworker:
                    if (socialPartner == null || socialPartner.IsFired || socialPartner.IsAway) ReturnToDesk();
                    else MoveTowards(socialPartner.transform.position, dt, WorkerState.Socialize);
                    break;
                case WorkerState.Socialize:
                    Runtime.socialSeconds += dt;
                    ActivityRules.ApplySocialStep(Runtime, dt);
                    if (stateTime >= stateLimit)
                    {
                        Runtime.socialNeed = .06f;
                        socialCooldown = 36f;
                        socialPartner = null;
                        ReturnToDesk();
                    }
                    break;
                case WorkerState.TakeBreak:
                    if (stateTime >= stateLimit) CompleteRest();
                    break;
                case WorkerState.BuySnack:
                    TickVendingReaction();
                    if (stateTime >= stateLimit) CompleteVending();
                    break;
                case WorkerState.Smoke:
                    if (stateTime >= stateLimit) CompleteSmoking();
                    break;
                case WorkerState.UseRestroom:
                    if (stateTime >= stateLimit) CompleteRestroom();
                    break;
                case WorkerState.LookAtPhone:
                case WorkerState.StandConfused:
                case WorkerState.Sleep:
                    if (stateTime >= stateLimit) CompleteDistraction();
                    break;
                case WorkerState.Wander:
                    TickWander(dt);
                    break;
                case WorkerState.WalkOutForAway:
                    MoveTowards(office.ExitOutsidePoint, dt, WorkerState.Away);
                    break;
                case WorkerState.Away:
                    TickAway(dt);
                    break;
                case WorkerState.ReturnFromAway:
                    MoveTowards(office.EntranceInsidePoint, dt, WorkerState.ReturnFromAway);
                    break;
                case WorkerState.FiredReaction:
                    if (stateTime >= stateLimit) SetState(WorkerState.PackDesk, 2.6f);
                    break;
                case WorkerState.PackDesk:
                    if (stateTime >= stateLimit) BeginCarryBox();
                    break;
                case WorkerState.CarryBox:
                    MoveTowards(office.Elevator.UsePoint.position, dt, WorkerState.ExitOffice);
                    break;
                case WorkerState.ExitOffice:
                    if (stateTime >= stateLimit) office.CompleteFiring(this);
                    break;
                case WorkerState.React:
                    if (stateTime >= stateLimit) ReturnToDesk();
                    break;
                case WorkerState.RecoverFromStuck:
                    if (stateTime >= .5f) ReturnToDesk();
                    break;
                default:
                    if (stateTime >= stateLimit) ReturnToDesk();
                    break;
            }
        }

        private void TickWork(float dt)
        {
            IsMoving = false;
            Runtime.workSeconds += dt * Runtime.effectiveProductivity;
            office.Cash.AccrueDeskIncome(Runtime.effectiveProductivity, dt);
            tasks.Contribute(Runtime.effectiveProductivity * dt * .38f);
            if (earningsEmoteCooldown <= 0f && Runtime.effectiveProductivity > 1.05f &&
                Runtime.focusedWorkSecondsRemaining <= 0f)
            {
                earningsEmoteCooldown = 18f;
                Visuals?.ShowEmote(StatusEmote.Money, 1.4f);
            }
            if (HasPlayerCommandAuthority && Runtime.focusedWorkSecondsRemaining > 0f) return;
            if (HasPlayerCommandAuthority && Runtime.focusedWorkSecondsRemaining <= 0f)
                HasPlayerCommandAuthority = false;
            if (decisionTime <= 0f || stateTime >= stateLimit) Decide();
        }

        private void TickPhoneWork(float dt)
        {
            float output = Runtime.effectiveProductivity;
            Runtime.workSeconds += dt * output;
            office.Cash.AccrueDeskIncome(output, dt);
            tasks.Contribute(output * dt * .38f);
            Runtime.positiveInfluence = "Phone work - 50% workstation efficiency";
            Runtime.negativeInfluence = "Needs a desk";
        }

        private void DecidePhoneWork()
        {
            PersonalityProfile profile = PersonalityRules.For(Definition.trait);
            decisionTime = random.Range(profile.DecisionMin, profile.DecisionMax);
            Runtime.autonomyDecisions++;

            if (Runtime.waterCooldown <= 0f && random.Chance(.10f + (1f - Runtime.mood) * .12f) &&
                TryStartAutonomousActivity(PlacementActivity.GetWater, ActivityRules.WaterDuration,
                    DistractionKind.None)) return;
            if (random.Chance(profile.DistractionChance))
            {
                StartDistraction(PersonalityRules.ChooseDistraction(Definition.trait, random));
                return;
            }
            BeginPhoneWorkInterval();
        }

        private void BeginPhoneWorkInterval()
        {
            PersonalityProfile profile = PersonalityRules.For(Definition.trait);
            Runtime.decision.Begin(WorkerDecisionCategory.PhoneWork, NeedKind.Happiness, false,
                PlacementActivity.Work, "phone-work", 0f, "Working by phone", office.SimulationTime, false);
            SetState(WorkerState.Unassigned, random.Range(profile.DecisionMin, profile.DecisionMax));
        }

        private void Decide()
        {
            if (IsFired || IsAway) return;
            PersonalityProfile profile = PersonalityRules.For(Definition.trait);
            decisionTime = random.Range(profile.DecisionMin, profile.DecisionMax);
            Runtime.autonomyDecisions++;

            float socialThreshold = Definition.trait == WorkerTrait.Social ? .52f : .78f;
            if (Runtime.socialNeed > socialThreshold && socialCooldown <= 0f)
            {
                WorkerAgent partner = office.FindSocialPartner(this);
                if (partner != null)
                {
                    socialPartner = partner;
                    partner.AcceptSocial(this);
                    SetTargetState(WorkerState.SeekCoworker, partner.transform.position, 10f);
                    Visuals?.ShowEmote(StatusEmote.Social, 2f);
                    return;
                }
            }

            if (Runtime.waterCooldown <= 0f && random.Chance(.10f + (1f - Runtime.mood) * .12f) &&
                TryStartAutonomousActivity(PlacementActivity.GetWater, ActivityRules.WaterDuration, DistractionKind.None)) return;

            if (random.Chance(profile.DistractionChance))
            {
                StartDistraction(PersonalityRules.ChooseDistraction(Definition.trait, random));
                return;
            }

            if (!random.Chance(profile.WorkPreference) && random.Chance(.24f))
            {
                SetState(WorkerState.IdleAtDesk, random.Range(2.5f, 4.5f));
                return;
            }
            SetState(WorkerState.Work, random.Range(8f,
                Definition.trait == WorkerTrait.Hardworking || Definition.trait == WorkerTrait.Focused ? 16f : 12f));
        }

        private bool TryStartAutonomousActivity(PlacementActivity activity, float duration, DistractionKind distraction)
        {
            if (!office.TryReserveAutonomousZone(this, activity, lastAutonomousZone, out PlacementZone zone)) return false;
            lastAutonomousZone = zone;
            commandedZone = zone;
            pendingPlacementActivity = activity;
            activeActivity = null;
            activityEffectApplied = false;
            vendingChargedForCurrentUse = false;
            HadWaterSocialOpportunity = false;
            SetActivePlacementZone(zone);
            if (distraction != DistractionKind.None) RegisterDistraction(distraction);
            autonomousActivityDuration = Mathf.Max(.25f, duration);
            SetTargetState(WorkerState.WalkToPlacement, zone.PositionFor(this), 20f);
            return true;
        }

        private void StartDistraction(DistractionKind kind)
        {
            float duration = PersonalityRules.DistractionDuration(Definition.trait, kind);
            if (kind == DistractionKind.ExtendedWater && Runtime.waterCooldown <= 0f &&
                TryStartAutonomousActivity(PlacementActivity.GetWater, duration, kind)) return;
            if (kind == DistractionKind.ExtendedBreak &&
                TryStartAutonomousActivity(PlacementActivity.Rest, duration, kind)) return;
            if (kind == DistractionKind.ExtendedSmoke && Runtime.smokingCooldown <= 0f &&
                TryStartAutonomousActivity(PlacementActivity.Smoke, duration, kind)) return;
            if (kind == DistractionKind.VendingInterest && Runtime.vendingCooldown <= 0f &&
                office.Cash.CanAfford(ActivityRules.SnackCost) &&
                TryStartAutonomousActivity(PlacementActivity.BuySnack, duration, kind)) return;

            RegisterDistraction(kind);
            switch (kind)
            {
                case DistractionKind.Wander:
                    target = office.GetWanderPoint(this);
                    Visuals?.ShowEmote(StatusEmote.Question, 2.2f);
                    SetState(WorkerState.Wander, duration);
                    break;
                case DistractionKind.Sleep:
                    Visuals?.ShowEmote(StatusEmote.Tired, 3f);
                    SetState(WorkerState.Sleep, duration);
                    break;
                case DistractionKind.Confused:
                case DistractionKind.VendingInterest:
                    Visuals?.ShowEmote(StatusEmote.Question, 2.5f);
                    SetState(WorkerState.StandConfused, duration);
                    break;
                default:
                    Visuals?.ShowEmote(StatusEmote.Exclamation, 2.2f);
                    SetState(WorkerState.LookAtPhone, duration);
                    break;
            }
        }

        private void RegisterDistraction(DistractionKind kind)
        {
            CurrentDistraction = kind;
            Runtime.distractionsStarted++;
            if (!distractionCounts.ContainsKey(kind)) distractionCounts[kind] = 0;
            distractionCounts[kind]++;
        }

        private void TickWander(float dt)
        {
            if (stateTime >= stateLimit)
            {
                CompleteDistraction();
                return;
            }
            Vector3 flat = target - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude < .12f) target = office.GetWanderPoint(this);
            else MoveTowards(target, dt * .60f, WorkerState.Wander);
        }

        private void CompleteDistraction()
        {
            FinishDistractionCounters();
            ReturnToDesk();
        }

        private void FinishDistractionCounters()
        {
            if (CurrentDistraction == DistractionKind.None) return;
            Runtime.distractionsCompleted++;
            CurrentDistraction = DistractionKind.None;
        }

        private void AcceptSocial(WorkerAgent initiator)
        {
            if (IsFired || IsAway || IsPlayerCarried || HasPlayerCommandAuthority ||
                Runtime.behavior == WorkerState.Socialize) return;
            socialPartner = initiator;
            SetState(WorkerState.Socialize, Definition.trait == WorkerTrait.Social ?
                random.Range(8f, 13f) : random.Range(4.5f, 7.5f));
            Visuals?.ShowEmote(StatusEmote.Social, 2f);
        }

        private void UpdateProductivity()
        {
            float nearby = office.ComputeNearbyModifier(this, out string positive, out string negative);
            bool phoneWorking = Runtime.behavior == WorkerState.Unassigned && Desk == null;
            if (IsFired || IsAway || (Runtime.behavior != WorkerState.Work && !phoneWorking))
            {
                Runtime.effectiveProductivity = 0f;
                Runtime.negativeInfluence = Runtime.behavior == WorkerState.Unassigned && Desk != null
                    ? "Resuming autonomy after ground placement"
                    : StateReason(Runtime.behavior);
                return;
            }

            float noise = Desk != null ? Desk.Noise : .5f;
            float workstation = Desk != null ? Desk.Modifier : ProductivityModel.PhoneWorkstationModifier;
            float trait = ProductivityModel.TraitModifier(Definition.trait, noise,
                office.Workday.Progress01, Runtime.energy);
            float focused = ProductivityModel.FocusedWorkModifier(Runtime.focusedWorkSecondsRemaining);
            Runtime.effectiveProductivity = ProductivityModel.Evaluate(Definition.skill, Runtime,
                workstation * nearby, trait, focused);
            Runtime.positiveInfluence = Runtime.focusedWorkSecondsRemaining > 0f ?
                $"Focused Work {Runtime.focusedWorkSecondsRemaining:0}s" :
                positive ?? StrongestPositiveInfluence();
            Runtime.negativeInfluence = negative ?? StrongestNegativeInfluence(noise);
        }

        private string StrongestPositiveInfluence()
        {
            NeedKind best = NeedKind.Happiness;
            float bestBenefit = -1f;
            foreach (NeedDefinition definition in NeedCatalog.All)
            {
                float benefit = NeedCatalog.Benefit01(Runtime, definition.Kind);
                if (benefit > bestBenefit) { bestBenefit = benefit; best = definition.Kind; }
            }
            NeedDefinition bestDefinition = NeedCatalog.Get(best);
            return bestDefinition.DisplayName + ": " + bestDefinition.StatusText(Runtime.GetNeed(best));
        }

        private string StrongestNegativeInfluence(float noise)
        {
            NeedKind worst = NeedKind.Happiness;
            float worstBenefit = 2f;
            foreach (NeedDefinition definition in NeedCatalog.All)
            {
                float benefit = NeedCatalog.Benefit01(Runtime, definition.Kind);
                if (benefit < worstBenefit) { worstBenefit = benefit; worst = definition.Kind; }
            }
            if (Runtime.stress > .70f) return "High stress";
            if (worstBenefit < .55f)
            {
                NeedDefinition worstDefinition = NeedCatalog.Get(worst);
                return worstDefinition.DisplayName + ": " + worstDefinition.StatusText(Runtime.GetNeed(worst));
            }
            if (Desk == null) return "Needs a desk";
            return noise > .62f ? "Noisy workstation" : "No major blocker";
        }

        private void GoToDesk()
        {
            if (Desk == null) { BeginPhoneWorkInterval(); return; }
            Runtime.decision.Begin(WorkerDecisionCategory.ReturningToWork, NeedKind.Happiness, false,
                PlacementActivity.Work, Desk.StableIdentifier, 0f, "Returning to desk", office.SimulationTime, false);
            SetTargetState(WorkerState.WalkToDesk, Desk.WorkPoint.position, 16f);
        }

        private void ReturnToDesk()
        {
            if (IsFired) return;
            FinishDistractionCounters();
            InterruptCurrentActivity();
            office.ReleaseTransientPlacement(this);
            commandedZone = null;
            pendingPlacementActivity = null;
            autonomousActivityDuration = 0f;
            HasPlayerCommandAuthority = false;
            if (Desk == null) { BeginPhoneWorkInterval(); return; }
            Runtime.decision.Begin(WorkerDecisionCategory.ReturningToWork, NeedKind.Happiness, false,
                PlacementActivity.Work, Desk.StableIdentifier, 0f, "Returning to desk", office.SimulationTime, false);
            SetTargetState(WorkerState.ReturnToDesk, Desk.WorkPoint.position, 16f);
        }

        private void MoveTowards(Vector3 destination, float dt, WorkerState arrival)
        {
            if (office.Navigation != null && (navigationPath == null || navigationPath.Length == 0 ||
                navigationVersion != office.Navigation.Version ||
                (new Vector2(navigationDestination.x, navigationDestination.z) -
                 new Vector2(destination.x, destination.z)).sqrMagnitude > .04f))
            {
                if (!PrepareNavigation(destination))
                {
                    HandleNavigationFailure("No valid path to " + DestinationLabel());
                    return;
                }
            }
            IsMoving = true;
            target = destination;
            Vector3 waypoint = navigationPath != null && navigationPathIndex < navigationPath.Length ?
                navigationPath[navigationPathIndex] : destination;
            Vector3 flat = waypoint - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude < .08f && navigationPath != null && navigationPathIndex < navigationPath.Length - 1)
            {
                navigationPathIndex++;
                Runtime.decision.lastProgressTime = office.SimulationTime;
                return;
            }
            bool finalWaypoint = navigationPath == null || navigationPathIndex >= navigationPath.Length - 1;
            Vector3 finalFlat = destination - transform.position;
            finalFlat.y = 0f;
            if (finalWaypoint && finalFlat.sqrMagnitude < .08f)
            {
                IsMoving = false;
                Vector3 exactDestination = new Vector3(destination.x, transform.position.y, destination.z);
                if (office.Navigation == null || office.Navigation.IsValidPoint(exactDestination))
                    transform.position = exactDestination;
                if (office.Navigation == null || office.Navigation.IsValidPoint(transform.position))
                    lastConfirmedReachablePosition = transform.position;
                navigationPath = null;
                navigationPathIndex = 0;
                if (arrival == WorkerState.Work)
                {
                    if (Desk != null) transform.rotation = Desk.WorkPoint.rotation;
                    activeActivity = PlacementActivity.Work;
                    Runtime.decision.Begin(WorkerDecisionCategory.Work, NeedKind.Happiness, false,
                        PlacementActivity.Work, Desk == null ? "work" : Desk.StableIdentifier, 0f,
                        "Working at assigned desk", office.SimulationTime, false);
                    SetState(WorkerState.Work, random.Range(7f, 12f));
                }
                else if (arrival == WorkerState.WalkToPlacement) ArriveAtPlayerPlacement();
                else if (arrival == WorkerState.UseCoffeeMachine)
                {
                    if (commandedZone != null && pendingPlacementActivity == PlacementActivity.GetCoffee)
                    {
                        if (!HasPlayerCommandAuthority && !office.Reservations.MarkArrived(this, out string reason))
                        {
                            NotifyReservationLost(reason);
                            return;
                        }
                        activeActivity = PlacementActivity.GetCoffee;
                        activityEffectApplied = false;
                        if (!HasPlayerCommandAuthority) office.Reservations.MarkStarted(this);
                    }
                    SetState(arrival, ActivityRules.CoffeeDuration);
                }
                else if (arrival == WorkerState.UseWaterCooler)
                {
                    activeActivity = PlacementActivity.GetWater;
                    activityEffectApplied = false;
                    SetState(arrival, ActivityRules.WaterDuration);
                }
                else if (arrival == WorkerState.Socialize) SetState(arrival, random.Range(4.5f, 7.5f));
                else if (arrival == WorkerState.ExitOffice) SetState(arrival, 1.3f);
                else if (arrival == WorkerState.Away) BeginAwayVisit();
                else if (arrival == WorkerState.ReturnFromAway) FinishAwayReturn();
                return;
            }

            Vector3 direction = flat.normalized;
            transform.position += direction * (2.25f * dt);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), dt * 9f);
            float progress = (transform.position - previousPosition).sqrMagnitude;
            if (progress < .00005f) stuckTime += dt;
            else
            {
                stuckTime = 0f;
                previousPosition = transform.position;
                lastConfirmedReachablePosition = transform.position;
                Runtime.decision.lastProgressTime = office.SimulationTime;
            }
            if (stuckTime > NeedAutonomyRules.ProgressTimeout)
                HandleNavigationFailure("No movement progress");
        }

        private void SetTargetState(WorkerState state, Vector3 destination, float limit)
        {
            target = destination;
            if (state == WorkerState.WalkOutForAway)
            {
                navigationDestination = destination;
                navigationPath = new[] { destination };
                navigationPathIndex = 0;
                navigationVersion = office.Navigation == null ? 0 : office.Navigation.Version;
            }
            else PrepareNavigation(destination);
            SetState(state, limit);
        }

        private bool PrepareNavigation(Vector3 destination)
        {
            navigationDestination = destination;
            navigationVersion = office.Navigation == null ? 0 : office.Navigation.Version;
            navigationPathIndex = 0;
            if (office.Navigation == null)
            {
                navigationPath = new[] { destination };
                return true;
            }
            if (!office.Navigation.TryFindPath(transform.position, destination, out navigationPath, out _))
            {
                navigationPath = null;
                return false;
            }
            return true;
        }

        private void HandleNavigationFailure(string reason)
        {
            Runtime.decision.retryCount++;
            LastNavigationRecoveryReason = reason;
            if (Runtime.decision.retryCount <= NeedAutonomyRules.MaximumRepathAttempts)
            {
                office.AutonomyCounters.repaths++;
                navigationPath = null;
                stuckTime = 0f;
                Runtime.decision.category = WorkerDecisionCategory.NavigationRecovery;
                Runtime.decision.reason = "Rerouting: " + reason;
                Visuals?.ShowEmote(StatusEmote.Question, 1.5f);
                if (PrepareNavigation(navigationDestination)) return;
            }

            office.AutonomyCounters.stuckRecoveries++;
            office.ReleaseTransientPlacement(this);
            commandedZone = null;
            pendingPlacementActivity = null;
            navigationPath = null;
            if (office.Navigation != null && !office.Navigation.IsValidPoint(transform.position) &&
                office.Navigation.IsValidPoint(lastConfirmedReachablePosition))
            {
                Vector3 invalidPosition = transform.position;
                transform.position = lastConfirmedReachablePosition;
                office.AutonomyCounters.emergencySafetyCorrections++;
                office.AutonomyCounters.lastSafetyCorrection = Definition.displayName + ": " + reason +
                    " from " + invalidPosition.ToString("F2") + " to " +
                    lastConfirmedReachablePosition.ToString("F2");
                Debug.LogWarning("NEED AUTONOMY SAFETY CORRECTION: " +
                                 office.AutonomyCounters.lastSafetyCorrection);
                Runtime.decision.fallbackLevel = DecisionFallbackLevel.SafetyCorrection;
            }
            Runtime.decision.category = WorkerDecisionCategory.NavigationRecovery;
            Runtime.decision.reason = "Recovered from route failure";
            needEvaluationTimer = .25f;
            SetState(WorkerState.RecoverFromStuck, .5f);
            stuckTime = 0f;
        }

        private void SetState(WorkerState state, float limit)
        {
            Runtime.behavior = state;
            observedStates.Add(state);
            stateTime = 0f;
            stateLimit = Mathf.Max(.25f, limit);
            IsMoving = state == WorkerState.WalkToDesk || state == WorkerState.ReturnToDesk ||
                       state == WorkerState.SeekCoffee || state == WorkerState.SeekWater ||
                       state == WorkerState.SeekCoworker || state == WorkerState.CarryBox ||
                       state == WorkerState.WalkToPlacement || state == WorkerState.WalkOutForAway ||
                       state == WorkerState.ReturnFromAway || state == WorkerState.Wander;
        }

        public bool CanAcceptPlacementActivity(PlacementActivity activity, out string reason)
        {
            if (Runtime == null) { reason = null; return true; }
            if (IsLeavingCompany) { reason = "Worker is leaving the company."; return false; }
            if (IsAway && !(activity == PlacementActivity.Work && Runtime.behavior == WorkerState.EnterOffice))
            {
                reason = "Worker is away from the office.";
                return false;
            }
            if (activeActivity == activity && activity != PlacementActivity.Work &&
                (Runtime.behavior == WorkerState.TakeBreak || Runtime.behavior == WorkerState.UseWaterCooler ||
                 Runtime.behavior == WorkerState.BuySnack || Runtime.behavior == WorkerState.Smoke ||
                 Runtime.behavior == WorkerState.UseRestroom ||
                 Runtime.behavior == WorkerState.WalkOutForAway))
            {
                reason = "Worker is already doing that activity.";
                return false;
            }
            if (activity == PlacementActivity.GetWater && Runtime.waterCooldown > 0f)
            {
                reason = $"Water cooldown: {Mathf.CeilToInt(Runtime.waterCooldown)}s.";
                return false;
            }
            if (activity == PlacementActivity.BuySnack)
            {
                if (Runtime.vendingCooldown > 0f)
                {
                    reason = $"Vending cooldown: {Mathf.CeilToInt(Runtime.vendingCooldown)}s.";
                    return false;
                }
                if (office != null && !office.Cash.CanAfford(ActivityRules.SnackCost))
                {
                    reason = "Need $15 cash for a snack.";
                    return false;
                }
            }
            if (activity == PlacementActivity.Smoke && Runtime.smokingCooldown > 0f)
            {
                reason = $"Smoking cooldown: {Mathf.CeilToInt(Runtime.smokingCooldown)}s.";
                return false;
            }
            if (activity == PlacementActivity.GetCoffee && coffeeCooldown > 0f)
            {
                reason = $"Coffee cooldown: {Mathf.CeilToInt(coffeeCooldown)}s.";
                return false;
            }
            reason = null;
            return true;
        }

        public bool CanBeginPlayerCarry(out string reason)
        {
            if (Runtime == null) { reason = "Worker is not ready."; return false; }
            if (IsPlayerCarried) { reason = "Worker is already being carried."; return false; }
            if (IsLeavingCompany || carriedBox != null) { reason = "Worker is leaving the company."; return false; }
            if (IsAway) { reason = "Worker is away from the office."; return false; }
            reason = null;
            return true;
        }

        public bool BeginPlayerCarry(out string reason)
        {
            if (!CanBeginPlayerCarry(out reason)) return false;
            carrySnapshotValid = true;
            preCarryPosition = transform.position;
            preCarryRotation = transform.rotation;
            preCarryState = Runtime.behavior;
            preCarryTarget = target;
            preCarrySocialPartner = socialPartner;
            preCarryStateTime = stateTime;
            preCarryStateLimit = stateLimit;
            preCarryDecisionTime = decisionTime;
            preCarryWasMoving = IsMoving;
            preCarryCommandAuthority = HasPlayerCommandAuthority;
            preCarryDistraction = CurrentDistraction;
            IsPlayerCarried = true;
            IsMoving = false;
            office.Reservations?.Suspend(this);
            Visuals?.SetCarried(true);
            return true;
        }

        public void SetPlayerCarryPosition(Vector3 position)
        {
            if (IsPlayerCarried) transform.position = position;
        }

        public void CancelPlayerCarryImmediate()
        {
            if (!IsPlayerCarried || !carrySnapshotValid) return;
            transform.position = preCarryPosition;
            transform.rotation = preCarryRotation;
            Runtime.behavior = preCarryState;
            LastRestoredCarryState = preCarryState;
            target = preCarryTarget;
            socialPartner = preCarrySocialPartner;
            stateTime = preCarryStateTime;
            stateLimit = preCarryStateLimit;
            decisionTime = preCarryDecisionTime;
            IsMoving = preCarryWasMoving;
            HasPlayerCommandAuthority = preCarryCommandAuthority;
            CurrentDistraction = preCarryDistraction;
            IsPlayerCarried = false;
            carrySnapshotValid = false;
            office.Reservations?.Resume(this);
            Visuals?.SetCarried(false);
        }

        public bool CommitPlayerCommand(WorkerCommand command)
        {
            if (!IsPlayerCarried || !carrySnapshotValid || command == null || command.destinationZone == null) return false;
            InterruptCurrentActivity();
            LastPlayerCommand = command;
            commandedZone = command.destinationZone;
            pendingPlacementActivity = command.requestedActivity;
            HasPlayerCommandAuthority = true;
            NeedKind addressed = BestNeedForActivity(command.requestedActivity);
            Runtime.decision.Begin(WorkerDecisionCategory.PlayerCommand, addressed,
                NeedAutonomyRules.ActivityImproves(command.requestedActivity, addressed), command.requestedActivity,
                command.destinationZone.StableIdentifier, 0f,
                "Following your instruction: " + command.destinationZone.ActivityLabel,
                office.SimulationTime, true);
            Runtime.decision.authoritySecondsRemaining = NeedAutonomyRules.PlayerAuthorityDefault;
            if (Runtime.decision.hasNeed) office.AutonomyCounters.playerNeedInterventions++;
            activeActivity = null;
            activityEffectApplied = false;
            vendingChargedForCurrentUse = false;
            HadWaterSocialOpportunity = false;
            IsPlayerCarried = false;
            carrySnapshotValid = false;
            Visuals?.SetCarried(false);
            SetTargetState(WorkerState.WalkToPlacement, commandedZone.PositionFor(this), 20f);
            return true;
        }

        public bool CommitGroundPlacement(GroundPlacementCommand command)
        {
            if (!IsPlayerCarried || !carrySnapshotValid || command == null) return false;
            InterruptCurrentActivity(false);
            LastGroundPlacementCommand = command;
            commandedZone = null;
            pendingPlacementActivity = null;
            HasPlayerCommandAuthority = false;
            activeActivity = null;
            IsPlayerCarried = false;
            carrySnapshotValid = false;
            Visuals?.SetCarried(false);
            transform.position = new Vector3(command.groundPoint.x, transform.position.y, command.groundPoint.z);
            lastConfirmedReachablePosition = transform.position;
            Runtime.decision.Begin(WorkerDecisionCategory.ReturningToWork, NeedKind.Happiness, false,
                PlacementActivity.Work, "ordinary-ground", 0f, "Settling after ground placement",
                office.SimulationTime, true);
            SetState(WorkerState.Unassigned, random.Range(2.5f, 4.5f));
            return true;
        }

        private NeedKind BestNeedForActivity(PlacementActivity activity)
        {
            NeedKind best = NeedKind.Happiness;
            float bestValue = float.MinValue;
            for (int i = 0; i < NeedCatalog.All.Length; i++)
            {
                NeedKind need = NeedCatalog.All[i].Kind;
                float value = NeedAutonomyRules.ActivityBenefit(activity, need,
                    Definition.trait == WorkerTrait.Caffeinated) * NeedAutonomyRules.Severity01(Runtime, need);
                if (value > bestValue) { bestValue = value; best = need; }
            }
            return best;
        }

        public void SetActivePlacementZone(PlacementZone zone) => ActivePlacementZone = zone;

        private void ArriveAtPlayerPlacement()
        {
            if (commandedZone == null || !pendingPlacementActivity.HasValue) { ReturnToDesk(); return; }
            if (!commandedZone.IsZoneEnabled)
            {
                NotifyReservationLost("Destination was disabled before arrival");
                return;
            }
            if (!HasPlayerCommandAuthority && pendingPlacementActivity.Value != PlacementActivity.LeaveOffice &&
                !office.Reservations.MarkArrived(this, out string reason))
            {
                NotifyReservationLost(reason);
                return;
            }
            transform.rotation = commandedZone.PlacementPoint.rotation;
            BeginPlacementActivity(pendingPlacementActivity.Value);
        }

        private void BeginPlacementActivity(PlacementActivity activity)
        {
            activeActivity = activity;
            activityEffectApplied = false;
            if (!HasPlayerCommandAuthority) office.Reservations?.MarkStarted(this);
            switch (activity)
            {
                case PlacementActivity.Work:
                    if (HasPlayerCommandAuthority)
                        Runtime.focusedWorkSecondsRemaining = ActivityRules.FocusedWorkDuration;
                    Visuals?.ShowEmote(StatusEmote.Focus, 2.4f);
                    commandedZone = null;
                    SetState(WorkerState.Work, 9999f);
                    break;
                case PlacementActivity.Rest:
                    Visuals?.ShowEmote(StatusEmote.Tired, 2.4f);
                    SetState(WorkerState.TakeBreak, HasPlayerCommandAuthority ? ActivityRules.RestDuration :
                        Mathf.Max(.25f, autonomousActivityDuration));
                    break;
                case PlacementActivity.GetWater:
                    socialPartner = office.FindWorkerNear(this, transform.position, 2.6f);
                    HadWaterSocialOpportunity = socialPartner != null;
                    Visuals?.ShowEmote(HadWaterSocialOpportunity ? StatusEmote.Social : StatusEmote.Water, 2.0f);
                    SetState(WorkerState.UseWaterCooler, HasPlayerCommandAuthority ? ActivityRules.WaterDuration :
                        Mathf.Max(.25f, autonomousActivityDuration));
                    break;
                case PlacementActivity.GetCoffee:
                    Visuals?.ShowEmote(StatusEmote.Tired, 2.0f);
                    SetState(WorkerState.UseCoffeeMachine, ActivityRules.CoffeeDuration);
                    break;
                case PlacementActivity.BuySnack:
                    BeginVending();
                    break;
                case PlacementActivity.Smoke:
                    BuildSmokingEffects();
                    Visuals?.ShowEmote(StatusEmote.Cigarette, 2.2f);
                    SetState(WorkerState.Smoke, HasPlayerCommandAuthority ? ActivityRules.SmokingDuration :
                        Mathf.Max(.25f, autonomousActivityDuration));
                    break;
                case PlacementActivity.UseRestroom:
                    Visuals?.ShowEmote(StatusEmote.Restroom, 2.2f);
                    SetState(WorkerState.UseRestroom, ActivityRules.RestroomDuration);
                    break;
                case PlacementActivity.LeaveOffice:
                    Runtime.awayReason = (AwayReason)random.Range(0, 4);
                    SetTargetState(WorkerState.WalkOutForAway, office.ExitOutsidePoint, 20f);
                    break;
            }
        }

        private void CompleteRest()
        {
            if (!activityEffectApplied)
            {
                ActivityRules.ApplyRest(Runtime);
                activityEffectApplied = true;
                Visuals?.ShowEmote(StatusEmote.Happy, 1.8f);
            }
            ReturnToDesk();
        }

        private void CompleteWater()
        {
            if (!activityEffectApplied)
            {
                ActivityRules.ApplyWater(Runtime);
                activityEffectApplied = true;
                Visuals?.ShowEmote(StatusEmote.Water, 1.8f);
            }
            Runtime.waterCooldown = ActivityRules.WaterCooldown;
            if (HadWaterSocialOpportunity)
            {
                Runtime.socialSeconds += 2f;
                Runtime.socialNeed = Mathf.Max(.05f, Runtime.socialNeed - .08f);
            }
            socialPartner = null;
            ReturnToDesk();
        }

        private void BeginVending()
        {
            if (!vendingChargedForCurrentUse)
            {
                if (!office.Cash.TrySpend(ActivityRules.SnackCost))
                {
                    office.ShowNotice("Snack cancelled: company cash fell below $15.");
                    ReturnToDesk();
                    return;
                }
                vendingChargedForCurrentUse = true;
                VendingCharges++;
            }
            LastVendingMalfunction = nextVendingMalfunctionOverride ?? random.Chance(ActivityRules.VendingMalfunctionChance);
            nextVendingMalfunctionOverride = null;
            if (LastVendingMalfunction)
            {
                Visuals?.ShowEmote(StatusEmote.Frustrated, 3f);
                shakingMachine = commandedZone != null ? commandedZone.transform : null;
                if (shakingMachine != null) shakingMachineBasePosition = shakingMachine.localPosition;
            }
            else Visuals?.ShowEmote(StatusEmote.Snack, 2f);
            SetState(WorkerState.BuySnack, HasPlayerCommandAuthority ? ActivityRules.VendingDuration :
                Mathf.Max(.25f, autonomousActivityDuration));
        }

        private void TickVendingReaction()
        {
            if (!LastVendingMalfunction || shakingMachine == null) return;
            float strength = stateTime > ActivityRules.VendingDuration - 2.2f ? .055f : .012f;
            shakingMachine.localPosition = shakingMachineBasePosition + Vector3.right * Mathf.Sin(stateTime * 34f) * strength;
        }

        private void CompleteVending()
        {
            if (!activityEffectApplied)
            {
                ActivityRules.ApplySnack(Runtime, LastVendingMalfunction);
                activityEffectApplied = true;
                Visuals?.ShowEmote(LastVendingMalfunction ? StatusEmote.Sad : StatusEmote.Happy, 1.8f);
            }
            Runtime.vendingCooldown = ActivityRules.VendingCooldown;
            RestoreVendingMachine();
            ReturnToDesk();
        }

        private void BuildSmokingEffects()
        {
            CleanupSmokingEffects();
            cigaretteProp = office.Catalog.Spawn("Cigarette", transform, Vector3.zero,
                Quaternion.Euler(0f, 20f, 90f), Vector3.one * .68f);
            cigaretteProp.name = "Worker Cigarette";
            cigaretteProp.transform.localPosition = new Vector3(.42f, 1.18f, .18f);
            cigaretteProp.transform.localRotation = Quaternion.Euler(0f, 20f, 90f);

            GameObject smoke = new GameObject("Restrained Stylized Smoke");
            smoke.transform.SetParent(transform, false);
            smoke.transform.localPosition = new Vector3(.45f, 1.25f, .18f);
            smokeParticles = smoke.AddComponent<ParticleSystem>();
            smokeParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = smokeParticles.main;
            main.loop = true;
            main.duration = 1f;
            main.startLifetime = 1.8f;
            main.startSpeed = .15f;
            main.startSize = .12f;
            main.startColor = new Color(.72f, .78f, .80f, .42f);
            main.maxParticles = 24;
            ParticleSystem.EmissionModule emission = smokeParticles.emission;
            emission.rateOverTime = 3.2f;
            ParticleSystem.ShapeModule shape = smokeParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 8f;
            shape.radius = .015f;
            smokeParticles.Play();
        }

        private void CompleteSmoking()
        {
            if (!activityEffectApplied)
            {
                ActivityRules.ApplySmoke(Runtime);
                activityEffectApplied = true;
                Visuals?.ShowEmote(StatusEmote.Happy, 1.6f);
            }
            Runtime.smokingCooldown = ActivityRules.SmokingCooldown;
            CleanupSmokingEffects();
            ReturnToDesk();
        }

        private void CompleteRestroom()
        {
            if (!activityEffectApplied)
            {
                ActivityRules.ApplyRestroom(Runtime);
                activityEffectApplied = true;
                Visuals?.ShowEmote(StatusEmote.Happy, 1.6f);
            }
            ReturnToDesk();
        }

        private void BeginAwayVisit()
        {
            office.ReleaseTransientPlacement(this);
            commandedZone = null;
            activeActivity = PlacementActivity.LeaveOffice;
            Runtime.awaySecondsRemaining = ActivityRules.AwayDuration;
            SetWorkerVisible(false);
            SetState(WorkerState.Away, ActivityRules.AwayDuration);
        }

        private void TickAway(float dt)
        {
            float step = Mathf.Min(dt, Runtime.awaySecondsRemaining);
            Runtime.awaySecondsRemaining = Mathf.Max(0f, Runtime.awaySecondsRemaining - step);
            if (Runtime.awaySecondsRemaining <= 0f)
            {
                transform.position = office.ExitOutsidePoint;
                SetWorkerVisible(true);
                SetTargetState(WorkerState.ReturnFromAway, office.EntranceInsidePoint, 12f);
            }
        }

        private void FinishAwayReturn()
        {
            Runtime.awaySecondsRemaining = 0f;
            activeActivity = null;
            ReturnToDesk();
        }

        private void InterruptCurrentActivity(bool applyInterruptionCooldowns = true)
        {
            if (HasPlayerCommandAuthority && Runtime != null)
                Runtime.focusedWorkSecondsRemaining = 0f;
            if (applyInterruptionCooldowns && Runtime != null && !activityEffectApplied)
            {
                if (Runtime.behavior == WorkerState.UseWaterCooler)
                    Runtime.waterCooldown = ActivityRules.WaterCooldown;
                else if (Runtime.behavior == WorkerState.BuySnack && vendingChargedForCurrentUse)
                    Runtime.vendingCooldown = ActivityRules.VendingCooldown;
                else if (Runtime.behavior == WorkerState.Smoke)
                    Runtime.smokingCooldown = ActivityRules.SmokingCooldown;
            }
            RestoreVendingMachine();
            CleanupSmokingEffects();
            if (Runtime != null && Runtime.behavior == WorkerState.Away)
                SetWorkerVisible(true);
            activeActivity = null;
            pendingPlacementActivity = null;
            autonomousActivityDuration = 0f;
            CurrentDistraction = DistractionKind.None;
            HasPlayerCommandAuthority = false;
        }

        private void RestoreVendingMachine()
        {
            if (shakingMachine != null) shakingMachine.localPosition = shakingMachineBasePosition;
            shakingMachine = null;
        }

        private void CleanupSmokingEffects()
        {
            if (smokeParticles != null)
            {
                smokeParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                Destroy(smokeParticles.gameObject);
                smokeParticles = null;
            }
            if (cigaretteProp != null)
            {
                Destroy(cigaretteProp);
                cigaretteProp = null;
            }
        }

        private void SetWorkerVisible(bool visible)
        {
            IsVisibleInOffice = visible;
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true)) renderer.enabled = visible;
            CapsuleCollider capsule = GetComponent<CapsuleCollider>();
            if (capsule != null) capsule.enabled = visible;
        }

        public void QueueVendingOutcome(bool malfunction) => nextVendingMalfunctionOverride = malfunction;

        public void NotifyReservationLost(string reason)
        {
            if (Runtime == null || IsFired) return;
            commandedZone = null;
            pendingPlacementActivity = null;
            navigationPath = null;
            Runtime.decision.category = WorkerDecisionCategory.NavigationRecovery;
            Runtime.decision.reason = "Rerouting: " + (reason ?? "destination unavailable");
            Runtime.decision.fallbackLevel = DecisionFallbackLevel.AlternateStation;
            Runtime.decision.reservationStatus = ReservationStatus.Released;
            office.AutonomyCounters.alternateDestinationsSelected++;
            needEvaluationTimer = .25f;
            SetState(WorkerState.RecoverFromStuck, .5f);
        }

        public void RequestImmediateNeedEvaluation() => needEvaluationTimer = 0f;

        public void ForceNavigationFailureForTesting()
        {
            if (!Application.isEditor || Runtime == null) return;
            navigationPath = null;
            navigationDestination = new Vector3(10000f, transform.position.y, 10000f);
            HandleNavigationFailure("Injected route failure");
        }

        public void BeginDistractionForTesting(DistractionKind kind)
        {
            if (Runtime == null || IsFired || IsAway || kind == DistractionKind.None) return;
            InterruptCurrentActivity();
            StartDistraction(kind);
        }

        public void Fire()
        {
            if (IsFired) return;
            IsFired = true;
            socialPartner = null;
            InterruptCurrentActivity();
            SetWorkerVisible(true);
            if (IsAway) transform.position = office.EntranceInsidePoint;
            Runtime.awaySecondsRemaining = 0f;
            office.ReleaseTransientPlacement(this);
            Desk?.Release(this);
            ActivityRules.ChangeNeeds(Runtime, 0f, -.18f, .08f);
            SetState(WorkerState.FiredReaction, 1.35f);
        }

        private void BeginCarryBox()
        {
            carriedBox = office.Catalog.Spawn("CardboardBox", transform, Vector3.zero,
                Quaternion.identity, Vector3.one * .82f);
            carriedBox.transform.localPosition = new Vector3(0f, .72f, -.48f);
            SetTargetState(WorkerState.CarryBox, office.Elevator.UsePoint.position, 18f);
        }

        public void ReactToFiring(bool relief)
        {
            if (IsFired || IsAway || IsPlayerCarried || HasPlayerCommandAuthority ||
                Runtime.behavior == WorkerState.Socialize) return;
            ActivityRules.ChangeNeeds(Runtime, 0f, relief ? .035f : -.07f, relief ? -.02f : .05f);
            Visuals?.ShowEmote(relief ? StatusEmote.Happy : StatusEmote.Sad, 2.4f);
            SetState(WorkerState.React, 1.3f);
        }

        public void ForceStateForCapture(StationKind kind, WorkerAgent partner = null)
        {
            if ((!AutomatedCaptureDirector.Requested && !AutomatedVideoDirector.Requested) || IsFired) return;
            switch (kind)
            {
                case StationKind.Coffee:
                    SetTargetState(WorkerState.SeekCoffee, office.Coffee.UsePoint.position, 20f);
                    break;
                case StationKind.Water:
                    activeActivity = PlacementActivity.GetWater;
                    activityEffectApplied = false;
                    SetTargetState(WorkerState.SeekWater, office.Water.UsePoint.position, 20f);
                    break;
                case StationKind.Break:
                    activeActivity = PlacementActivity.Rest;
                    activityEffectApplied = false;
                    transform.position = office.Break.UsePoint.position;
                    SetState(WorkerState.TakeBreak, ActivityRules.RestDuration);
                    break;
                default:
                    if (partner != null)
                    {
                        socialPartner = partner;
                        partner.AcceptSocial(this);
                        SetTargetState(WorkerState.SeekCoworker, partner.transform.position, 20f);
                    }
                    break;
            }
        }

        private static string StateReason(WorkerState state)
        {
            switch (state)
            {
                case WorkerState.Socialize: return "In conversation";
                case WorkerState.UseCoffeeMachine:
                case WorkerState.SeekCoffee: return "Getting coffee";
                case WorkerState.UseWaterCooler:
                case WorkerState.SeekWater: return "Getting water";
                case WorkerState.TakeBreak: return "Resting";
                case WorkerState.FiredReaction:
                case WorkerState.PackDesk:
                case WorkerState.CarryBox: return "Leaving the company";
                case WorkerState.WalkToPlacement: return "Following a placement command";
                case WorkerState.BuySnack: return "Using the vending machine";
                case WorkerState.Smoke: return "Smoking outside";
                case WorkerState.UseRestroom: return "Using the restroom";
                case WorkerState.WalkOutForAway: return "Walking through the exit";
                case WorkerState.Away: return "Away from the office";
                case WorkerState.ReturnFromAway: return "Returning to the office";
                case WorkerState.LookAtPhone: return "Distracted by phone";
                case WorkerState.Wander: return "Wandering briefly";
                case WorkerState.StandConfused: return "Unsure what to do";
                case WorkerState.Sleep: return "Fell asleep";
                case WorkerState.Unassigned: return "Waiting for a desk assignment";
                default: return "Away from desk";
            }
        }

        private string DestinationLabel()
        {
            if (IsPlayerCarried) return "Player is choosing";
            if (Runtime == null) return "None";
            if (Runtime.behavior == WorkerState.Wander) return "Around the office";
            if (Runtime.behavior == WorkerState.Unassigned) return Desk == null ? "Phone work" : "Current position";
            if (Runtime.behavior == WorkerState.SeekCoworker && socialPartner != null)
                return socialPartner.Definition.displayName;
            if (Runtime.behavior == WorkerState.SeekCoffee || Runtime.behavior == WorkerState.UseCoffeeMachine)
                return "Coffee machine";
            if (Runtime.behavior == WorkerState.WalkOutForAway || Runtime.behavior == WorkerState.Away)
                return "Outside office";
            if (Runtime.behavior == WorkerState.ReturnFromAway) return "Office entrance";
            if (commandedZone != null) return commandedZone.ActivityLabel;
            if (Runtime.behavior == WorkerState.Work || Runtime.behavior == WorkerState.ReturnToDesk ||
                Runtime.behavior == WorkerState.WalkToDesk) return Desk == null ? "Desk" : Desk.ZoneLabel;
            return "Current position";
        }

        private static string PrettyState(WorkerState state)
        {
            switch (state)
            {
                case WorkerState.LookAtPhone: return "Looking at phone";
                case WorkerState.StandConfused: return "Standing confused";
                case WorkerState.Sleep: return "Falling asleep";
                case WorkerState.UseWaterCooler: return "Getting water";
                case WorkerState.BuySnack: return "Buying a snack";
                case WorkerState.UseRestroom: return "Using the restroom";
            }
            string name = state.ToString();
            for (int i = 1; i < name.Length; i++)
                if (char.IsUpper(name[i])) { name = name.Insert(i, " "); i++; }
            return name;
        }

        private static string PrettyAwayReason(AwayReason reason)
        {
            switch (reason)
            {
                case AwayReason.LongBreak: return "Long break";
                case AwayReason.OffSiteTask: return "Off-site task";
                default: return reason.ToString();
            }
        }

        private void OnDestroy()
        {
            office?.ReleaseTransientPlacement(this);
            RestoreVendingMachine();
            if (smokeParticles != null) Destroy(smokeParticles.gameObject);
            if (cigaretteProp != null) Destroy(cigaretteProp);
        }
    }
}
