using System.Collections;
using TMPro;
using UnityEngine;

namespace OpenPlan
{
    /// <summary>Opens the neighboring unit in the live starter-office world without a scene reload.</summary>
    public sealed class OfficeExpansionController : MonoBehaviour
    {
        public bool IsExpanded { get; private set; }
        public bool IsAnimating { get; private set; }
        public bool ConnectingWallOpen => connectingWall == null || !connectingWall.activeSelf;
        public bool DoorwayTrimVisible => doorwayTrim != null && doorwayTrim.activeSelf;
        public bool NavigationEnabled { get; private set; }
        public int PurchaseCount { get; private set; }

        private OfficeDirector office;
        private GameObject connectingWall;
        private OfficeObstacleVolume connectingObstacle;
        private GameObject doorwayTrim;
        private Light neighborLight;
        private TextMeshPro purchaseLabel;
        private Renderer[] neighborRenderers;
        private Workstation[] workstations;
        private PlacementZone[] placementZones;
        private FutureDeskLocation[] futureLocations;

        public void Configure(bool expanded, GameObject wall, OfficeObstacleVolume obstacle, GameObject trim,
            Light light, TextMeshPro label, Renderer[] renderers, Workstation[] desks, PlacementZone[] zones,
            FutureDeskLocation[] futures)
        {
            IsExpanded = expanded;
            connectingWall = wall;
            connectingObstacle = obstacle;
            doorwayTrim = trim;
            neighborLight = light;
            purchaseLabel = label;
            neighborRenderers = renderers ?? System.Array.Empty<Renderer>();
            workstations = desks ?? System.Array.Empty<Workstation>();
            placementZones = zones ?? System.Array.Empty<PlacementZone>();
            futureLocations = futures ?? System.Array.Empty<FutureDeskLocation>();
            NavigationEnabled = expanded;
        }

        public void Initialize(OfficeDirector director)
        {
            office = director;
            if (IsExpanded) ApplyExpandedState(false);
        }

        public bool TryPurchase(out string reason)
        {
            reason = null;
            if (office == null || office.Stage == OfficeStage.EstablishedOffice)
            {
                reason = "Expansion is only available from the Starter Office.";
                return false;
            }
            if (IsExpanded || IsAnimating)
            {
                reason = "The neighboring unit is already open.";
                return false;
            }
            if (!ExpansionRules.CanPurchase(office.Cash.CurrentCash, false))
            {
                reason = $"Need ${ExpansionRules.PurchasePrice:N0} cash to purchase the neighboring unit.";
                return false;
            }
            if (!office.Cash.TrySpend(ExpansionRules.PurchasePrice))
            {
                reason = "The purchase could not be completed.";
                return false;
            }

            PurchaseCount++;
            IsAnimating = true;
            office.SetInputLocked(true);
            office.CarryController?.CancelCarry(true);
            office.Audio?.PlayExpansionPurchase();
            StartCoroutine(ExpansionSequence());
            return true;
        }

        private IEnumerator ExpansionSequence()
        {
            float previousSpeed = SimulationSpeedController.Instance == null ? 1f :
                Mathf.Max(1f, SimulationSpeedController.Instance.Speed);
            SimulationSpeedController.Instance?.SetSpeed(0f);

            float startingLight = neighborLight == null ? 0f : neighborLight.intensity;
            Vector3 startingScale = connectingWall == null ? Vector3.one : connectingWall.transform.localScale;
            const float duration = 1.05f;
            float elapsed = 0f;
            bool openingCuePlayed = false;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                if (neighborLight != null) neighborLight.intensity = Mathf.Lerp(startingLight, 2.25f, t);
                if (connectingWall != null)
                    connectingWall.transform.localScale = Vector3.Scale(startingScale, new Vector3(1f, 1f - t, 1f));
                if (!openingCuePlayed && t >= .42f)
                {
                    openingCuePlayed = true;
                    if (doorwayTrim != null) doorwayTrim.SetActive(true);
                    office.Audio?.PlayWallOpen();
                }
                yield return null;
            }

            ApplyExpandedState(true);
            yield return new WaitForSecondsRealtime(.18f);
            office.SetInputLocked(false);
            SimulationSpeedController.Instance?.SetSpeed(previousSpeed);
            IsAnimating = false;
            office.ShowNotice("FIRST EXPANSION COMPLETE");
        }

        private void ApplyExpandedState(bool animateCamera)
        {
            IsExpanded = true;
            NavigationEnabled = true;
            if (neighborLight != null) neighborLight.intensity = 1.8f;
            if (connectingWall != null) connectingWall.SetActive(false);
            if (connectingObstacle != null)
            {
                if (connectingObstacle.VolumeCollider != null) connectingObstacle.VolumeCollider.enabled = false;
                office?.Layout?.UnregisterObstacle(connectingObstacle);
            }
            if (doorwayTrim != null) doorwayTrim.SetActive(true);

            foreach (Workstation desk in workstations)
            {
                if (desk == null || !desk.StableIdentifier.StartsWith("neighbor.work.")) continue;
                desk.SetZoneEnabled(true);
                desk.SetUnavailableReason(null);
            }
            foreach (PlacementZone zone in placementZones)
            {
                if (zone == null || !zone.StableIdentifier.StartsWith("neighbor.")) continue;
                zone.SetZoneEnabled(true);
                zone.SetUnavailableReason(null);
            }
            foreach (FutureDeskLocation location in futureLocations)
                if (location != null && location.IsNeighboringUnit) location.SetAvailable(true);

            foreach (Renderer renderer in neighborRenderers)
            {
                if (renderer == null) continue;
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetColor("_BaseColor", new Color(.62f, .53f, .41f, 1f));
                renderer.SetPropertyBlock(block);
            }
            if (purchaseLabel != null)
            {
                purchaseLabel.text = "UNIT NEXT DOOR\nPURCHASED";
                purchaseLabel.color = new Color(.55f, 1f, .68f);
            }

            if (office != null && office.Layout != null)
                office.Layout.Configure(new Bounds(new Vector3(2f, 0f, 0f), new Vector3(25f, 4f, 15.5f)),
                    new Bounds(new Vector3(2f, 0f, 0f), new Vector3(26f, 1f, 10f)), 13.2f,
                    new Bounds(new Vector3(3f, 0f, 0f), new Vector3(22f, 1f, 10.6f)));
            office?.MarkExpansionComplete(animateCamera);
            office?.InvalidateNavigation();
            Camera.main?.GetComponent<OfficeCameraRig>()?.ApplyLayoutChange(animateCamera);
        }
    }
}
