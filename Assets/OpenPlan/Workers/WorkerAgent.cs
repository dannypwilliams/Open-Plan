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
        public float Productivity => Runtime.effectiveProductivity;

        private OfficeDirector office;
        private TaskQueue tasks;
        private SeededRandomService random;
        private Vector3 target;
        private WorkerAgent socialPartner;
        private float stateTime;
        private float stateLimit;
        private float decisionTime;
        private float coffeeCooldown;
        private float waterCooldown;
        private float socialCooldown;
        private float stuckTime;
        private Vector3 previousPosition;
        private GameObject carriedBox;

        public void Initialize(OfficeDirector director, WorkerDefinition definition, Workstation desk, Vector3 spawn)
        {
            office = director;
            tasks = director.Tasks;
            random = director.Random;
            Definition = definition.Clone();
            Runtime = new WorkerRuntimeState
            {
                energy = random.Range(0.72f, 0.96f),
                focus = random.Range(0.68f, 0.94f),
                morale = random.Range(0.68f, 0.92f),
                socialNeed = random.Range(0.05f, 0.32f)
            };
            transform.position = spawn;
            previousPosition = spawn;
            Visuals = gameObject.AddComponent<WorkerVisuals>();
            Visuals.Initialize(Definition.clothing, director.Catalog.GetMaterial("cyan"));
            CapsuleCollider capsule = GetComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, 0.85f, 0f);
            capsule.height = 1.8f;
            capsule.radius = 0.42f;
            SetDesk(desk);
            SetState(WorkerState.EnterOffice, 0.75f);
        }

        public void SetDesk(Workstation desk)
        {
            Desk = desk;
        }

        private void Update()
        {
            if (Runtime == null || office == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) { Visuals?.Tick(Runtime.behavior, IsMoving, Productivity); return; }
            stateTime += dt;
            decisionTime -= dt;
            coffeeCooldown -= dt;
            waterCooldown -= dt;
            socialCooldown -= dt;

            Runtime.socialNeed = Mathf.Clamp01(Runtime.socialNeed + dt * (Definition.trait == WorkerTrait.Social ? 0.010f : 0.006f));
            if (Runtime.energy < 0.34f) Runtime.lowEnergySeconds += dt;
            TickState(dt);
            UpdateProductivity();
            Visuals?.Tick(Runtime.behavior, IsMoving, Productivity);
        }

        private void TickState(float dt)
        {
            switch (Runtime.behavior)
            {
                case WorkerState.EnterOffice:
                    if (stateTime >= stateLimit) GoToDesk();
                    break;
                case WorkerState.WalkToDesk:
                case WorkerState.ReturnToDesk:
                    MoveTowards(Desk != null ? Desk.WorkPoint.position : transform.position, dt, WorkerState.Work);
                    break;
                case WorkerState.Work:
                    TickWork(dt);
                    break;
                case WorkerState.IdleAtDesk:
                    Runtime.focus = Mathf.Clamp01(Runtime.focus + dt * 0.012f);
                    if (stateTime >= stateLimit) Decide();
                    break;
                case WorkerState.SeekCoffee:
                    MoveTowards(office.Coffee.UsePoint.position, dt, WorkerState.UseCoffeeMachine);
                    break;
                case WorkerState.UseCoffeeMachine:
                    if (stateTime >= stateLimit)
                    {
                        float boost = Definition.trait == WorkerTrait.Caffeinated ? 0.62f : 0.42f;
                        Runtime.energy = SimulationRules.RestoreCoffee(Runtime.energy, Definition.trait == WorkerTrait.Caffeinated);
                        Runtime.focus = Mathf.Clamp01(Runtime.focus + 0.18f);
                        coffeeCooldown = Definition.trait == WorkerTrait.Caffeinated ? 34f : 52f;
                        ReturnToDesk();
                    }
                    break;
                case WorkerState.SeekWater:
                    MoveTowards(office.Water.UsePoint.position, dt, WorkerState.UseWaterCooler);
                    break;
                case WorkerState.UseWaterCooler:
                    if (stateTime >= stateLimit)
                    {
                        Runtime.morale = Mathf.Clamp01(Runtime.morale + 0.16f);
                        Runtime.focus = Mathf.Clamp01(Runtime.focus + 0.07f);
                        waterCooldown = 48f;
                        ReturnToDesk();
                    }
                    break;
                case WorkerState.SeekCoworker:
                    if (socialPartner == null || socialPartner.IsFired) ReturnToDesk();
                    else MoveTowards(socialPartner.transform.position, dt, WorkerState.Socialize);
                    break;
                case WorkerState.Socialize:
                    Runtime.socialSeconds += dt;
                    Runtime.morale = Mathf.Clamp01(Runtime.morale + dt * 0.018f);
                    Runtime.focus = Mathf.Clamp01(Runtime.focus - dt * 0.008f);
                    if (stateTime >= stateLimit)
                    {
                        Runtime.socialNeed = 0.06f;
                        socialCooldown = 36f;
                        socialPartner = null;
                        ReturnToDesk();
                    }
                    break;
                case WorkerState.TakeBreak:
                    Runtime.energy = Mathf.Clamp01(Runtime.energy + dt * 0.035f);
                    Runtime.morale = Mathf.Clamp01(Runtime.morale + dt * 0.018f);
                    if (stateTime >= stateLimit) ReturnToDesk();
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
                    if (stateTime >= 0.5f) ReturnToDesk();
                    break;
                default:
                    if (stateTime >= stateLimit) ReturnToDesk();
                    break;
            }
        }

        private void TickWork(float dt)
        {
            IsMoving = false;
            float persistence = Definition.trait == WorkerTrait.Focused ? 0.72f : Definition.trait == WorkerTrait.Lazy ? 1.35f : 1f;
            Runtime.energy = SimulationRules.DecayEnergy(Runtime.energy, dt, 0.0018f * persistence);
            Runtime.focus = Mathf.Clamp01(Runtime.focus - dt * 0.00135f * (Desk != null ? Mathf.Lerp(0.75f, 1.45f, Desk.Noise) : 1f));
            Runtime.morale = Mathf.Clamp01(Runtime.morale - dt * (Definition.trait == WorkerTrait.Anxious && Desk != null && Desk.Noise > 0.6f ? 0.0011f : 0.00018f));
            Runtime.workSeconds += dt * Runtime.effectiveProductivity;
            tasks.Contribute(Runtime.effectiveProductivity * dt * 0.38f);
            if (decisionTime <= 0f || stateTime >= stateLimit) Decide();
        }

        private void Decide()
        {
            if (IsFired) return;
            decisionTime = random.Range(5.5f, 8.5f);
            if (Runtime.energy < 0.39f && coffeeCooldown <= 0f)
            {
                SetTargetState(WorkerState.SeekCoffee, office.Coffee.UsePoint.position, 12f);
                return;
            }
            if ((Runtime.energy < 0.28f || Runtime.morale < 0.40f) && random.Chance(0.78f))
            {
                target = office.Break.UsePoint.position;
                SetState(WorkerState.TakeBreak, random.Range(5f, 8f));
                transform.position = Vector3.MoveTowards(transform.position, target, 0.5f);
                return;
            }
            float socialThreshold = Definition.trait == WorkerTrait.Social ? 0.52f : 0.78f;
            if (Runtime.socialNeed > socialThreshold && socialCooldown <= 0f)
            {
                WorkerAgent partner = office.FindSocialPartner(this);
                if (partner != null)
                {
                    socialPartner = partner;
                    partner.AcceptSocial(this);
                    SetTargetState(WorkerState.SeekCoworker, partner.transform.position, 10f);
                    return;
                }
            }
            if (waterCooldown <= 0f && random.Chance(0.10f + (1f - Runtime.morale) * 0.12f))
            {
                SetTargetState(WorkerState.SeekWater, office.Water.UsePoint.position, 12f);
                return;
            }
            if (Runtime.focus < 0.35f && random.Chance(0.40f))
            {
                SetState(WorkerState.IdleAtDesk, random.Range(2.5f, 4.5f));
                return;
            }
            SetState(WorkerState.Work, random.Range(7f, Definition.trait == WorkerTrait.Focused ? 15f : 11f));
        }

        private void AcceptSocial(WorkerAgent initiator)
        {
            if (IsFired || Runtime.behavior == WorkerState.Socialize) return;
            socialPartner = initiator;
            SetState(WorkerState.Socialize, random.Range(4.5f, 7.5f));
        }

        private void UpdateProductivity()
        {
            if (IsFired || Runtime.behavior != WorkerState.Work)
            {
                Runtime.effectiveProductivity = 0f;
                Runtime.negativeInfluence = StateReason(Runtime.behavior);
                return;
            }
            float nearby = office.ComputeNearbyModifier(this, out string positive, out string negative);
            float noise = Desk != null ? Desk.Noise : 0.5f;
            float workstation = Desk != null ? Desk.Modifier : 1f;
            float trait = ProductivityModel.TraitModifier(Definition.trait, noise, office.Workday.Progress01, Runtime.energy);
            Runtime.effectiveProductivity = ProductivityModel.Evaluate(Definition.skill, Runtime.focus, Runtime.energy,
                Runtime.morale, workstation, nearby, trait);
            Runtime.positiveInfluence = positive ?? (Runtime.focus > 0.72f ? "Strong focus" : Desk != null ? Desk.ZoneLabel : "Steady pace");
            Runtime.negativeInfluence = negative ?? (Runtime.energy < 0.40f ? "Low energy" : noise > 0.62f ? "Noisy workstation" : "No major blocker");
        }

        private void GoToDesk()
        {
            if (Desk == null) return;
            SetTargetState(WorkerState.WalkToDesk, Desk.WorkPoint.position, 16f);
        }

        private void ReturnToDesk()
        {
            if (IsFired) return;
            if (Desk == null) { SetState(WorkerState.IdleAtDesk, 2f); return; }
            SetTargetState(WorkerState.ReturnToDesk, Desk.WorkPoint.position, 16f);
        }

        private void MoveTowards(Vector3 destination, float dt, WorkerState arrival)
        {
            IsMoving = true;
            target = destination;
            Vector3 flat = destination - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.08f)
            {
                IsMoving = false;
                transform.position = new Vector3(destination.x, transform.position.y, destination.z);
                if (arrival == WorkerState.Work) SetState(WorkerState.Work, random.Range(7f, 12f));
                else if (arrival == WorkerState.UseCoffeeMachine) SetState(arrival, 2.8f);
                else if (arrival == WorkerState.UseWaterCooler) SetState(arrival, 2.4f);
                else if (arrival == WorkerState.Socialize) SetState(arrival, random.Range(4.5f, 7.5f));
                else if (arrival == WorkerState.ExitOffice) SetState(arrival, 1.3f);
                return;
            }
            Vector3 direction = flat.normalized;
            transform.position += direction * (2.25f * dt);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), dt * 9f);
            if ((transform.position - previousPosition).sqrMagnitude < 0.00005f) stuckTime += dt;
            else { stuckTime = 0f; previousPosition = transform.position; }
            if (stuckTime > 3f)
            {
                transform.position += new Vector3(0.35f, 0f, 0.35f);
                SetState(WorkerState.RecoverFromStuck, 0.5f);
                stuckTime = 0f;
            }
        }

        private void SetTargetState(WorkerState state, Vector3 destination, float limit)
        {
            target = destination;
            SetState(state, limit);
        }

        private void SetState(WorkerState state, float limit)
        {
            Runtime.behavior = state;
            stateTime = 0f;
            stateLimit = Mathf.Max(0.25f, limit);
            IsMoving = state == WorkerState.WalkToDesk || state == WorkerState.ReturnToDesk ||
                       state == WorkerState.SeekCoffee || state == WorkerState.SeekWater ||
                       state == WorkerState.SeekCoworker || state == WorkerState.CarryBox;
        }

        public void Fire()
        {
            if (IsFired) return;
            IsFired = true;
            socialPartner = null;
            Desk?.Release(this);
            Runtime.morale = Mathf.Clamp01(Runtime.morale - 0.18f);
            SetState(WorkerState.FiredReaction, 1.35f);
        }

        private void BeginCarryBox()
        {
            carriedBox = office.Catalog.Spawn("CardboardBox", transform, Vector3.zero, Quaternion.identity, Vector3.one * 0.82f);
            carriedBox.transform.localPosition = new Vector3(0f, 0.72f, -0.48f);
            SetTargetState(WorkerState.CarryBox, office.Elevator.UsePoint.position, 18f);
        }

        public void ReactToFiring(bool relief)
        {
            if (IsFired || Runtime.behavior == WorkerState.Socialize) return;
            Runtime.morale = Mathf.Clamp01(Runtime.morale + (relief ? 0.035f : -0.07f));
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
                    SetTargetState(WorkerState.SeekWater, office.Water.UsePoint.position, 20f);
                    break;
                case StationKind.Break:
                    target = office.Break.UsePoint.position;
                    SetState(WorkerState.TakeBreak, 5f);
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
                case WorkerState.SeekCoffee: return "Needs coffee";
                case WorkerState.TakeBreak: return "Taking a short break";
                case WorkerState.FiredReaction:
                case WorkerState.PackDesk:
                case WorkerState.CarryBox: return "Leaving the company";
                default: return "Away from desk";
            }
        }
    }
}
