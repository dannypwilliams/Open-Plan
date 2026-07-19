using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace OpenPlan
{
    /// <summary>Minimal Level One environment used until the authored starter-office pass lands.</summary>
    public sealed class StarterOfficeEnvironmentBuilder : IOfficeEnvironmentBuilder
    {
        private readonly OfficeAssetCatalog catalog;
        private readonly Transform root;
        private readonly bool expanded;

        public List<Workstation> Workstations { get; } = new List<Workstation>();
        public List<PlacementZone> PlacementZones { get; } = new List<PlacementZone>();
        public CoffeeStation Coffee { get; private set; }
        public WaterStation Water { get; private set; }
        public NeedStation Break { get; private set; }
        public NeedStation Elevator { get; private set; }

        public StarterOfficeEnvironmentBuilder(OfficeAssetCatalog assetCatalog, Transform parent, bool isExpanded)
        {
            catalog = assetCatalog;
            root = parent;
            expanded = isExpanded;
        }

        public void Build()
        {
            BuildFloorAndShell();
            BuildDesks();
            BuildAmenities();
            BuildClutter();
            BuildLighting();
            PlacementZones.AddRange(Workstations);
        }

        private void BuildFloorAndShell()
        {
            catalog.Spawn("FloorSlab", root, new Vector3(-2.2f, 0f, 0f), Quaternion.identity, new Vector3(.38f, 1f, .56f));
            if (expanded)
                catalog.Spawn("FloorSlab", root, new Vector3(6.5f, 0f, 0f), Quaternion.identity, new Vector3(.26f, 1f, .56f));

            for (int i = 0; i < 3; i++)
                catalog.Spawn("PartialWall", root, new Vector3(-5.0f + i * 4.1f, 0f, 5.25f), Quaternion.identity, new Vector3(.72f, 1f, 1f));
            catalog.Spawn("PartialWall", root, new Vector3(-7.7f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), new Vector3(.92f, 1f, 1f));
            catalog.Spawn("PartialWall", root, new Vector3(2.65f, 0f, -2.6f), Quaternion.Euler(0f, 90f, 0f), new Vector3(.48f, 1f, 1f));

            if (expanded)
            {
                catalog.Spawn("PartialWall", root, new Vector3(7.2f, 0f, 5.25f), Quaternion.identity, new Vector3(1.12f, 1f, 1f));
                catalog.Spawn("PartialWall", root, new Vector3(10.4f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), new Vector3(.92f, 1f, 1f));
            }
            else
            {
                catalog.Spawn("Door", root, new Vector3(2.65f, 0f, 2.65f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
                catalog.Spawn("CardboardBox", root, new Vector3(1.7f, 0f, 3.4f), Quaternion.Euler(0f, 12f, 0f), Vector3.one);
            }
        }

        private void BuildDesks()
        {
            Vector3[] positions = expanded
                ? new[]
                {
                    new Vector3(-4.8f,0f,-1.7f), new Vector3(-1.2f,0f,-1.7f), new Vector3(-4.8f,0f,1.4f),
                    new Vector3(4.2f,0f,-1.7f), new Vector3(7.5f,0f,-1.7f), new Vector3(4.2f,0f,1.4f)
                }
                : new[] { new Vector3(-4.8f,0f,-1.7f), new Vector3(-1.2f,0f,-1.7f), new Vector3(-4.8f,0f,1.4f) };

            for (int i = 0; i < positions.Length; i++)
            {
                Quaternion facing = i % 2 == 0 ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
                GameObject desk = catalog.Spawn(i % 2 == 0 ? "Desk_A" : "Desk_B", root, positions[i], facing, new Vector3(.88f, .92f, .88f));
                Workstation station = desk.AddComponent<Workstation>();
                float noise = i == 0 ? .24f : i == 1 ? .62f : .42f;
                float light = i == 2 ? .82f : .64f;
                station.Configure(i, noise, light, Mathf.Lerp(.94f, 1.05f, light),
                    i >= 3 ? "Neighboring unit desk" : "Cramped starter desk", i >= 3);
                Workstations.Add(station);
                Vector3 forward = facing * Vector3.forward;
                catalog.Spawn("OfficeChair", root, positions[i] - forward * .86f, facing, Vector3.one * .82f);
                catalog.Spawn("Monitor", root, positions[i] + Vector3.up * .75f + forward * .12f, facing, Vector3.one * .72f);
                catalog.Spawn("Keyboard", root, positions[i] + Vector3.up * .78f - forward * .20f, facing, Vector3.one * .72f);
            }
        }

        private void BuildAmenities()
        {
            GameObject entry = new GameObject("Starter Office Exit");
            entry.transform.SetParent(root, false);
            entry.transform.position = new Vector3(-6.6f, 0f, 4.1f);
            Elevator = entry.AddComponent<NeedStation>();
            Elevator.Configure(StationKind.Elevator, Vector3.zero);
            AddPlacementZone(entry, PlacementActivity.LeaveOffice, Vector3.zero);
            catalog.Spawn("Door", root, entry.transform.position, Quaternion.identity, Vector3.one);
            catalog.Spawn("ExitSign", root, entry.transform.position + new Vector3(0f, 2.25f, .18f), Quaternion.identity, Vector3.one * .82f);

            GameObject water = catalog.Spawn("WaterCooler", root, new Vector3(.9f, 0f, 3.9f), Quaternion.Euler(0f, 180f, 0f), Vector3.one * .86f);
            Water = water.AddComponent<WaterStation>();
            Water.Configure(StationKind.Water, new Vector3(0f, 0f, -1f));
            AddPlacementZone(water, PlacementActivity.GetWater, new Vector3(0f, 0f, -1f));

            GameObject coffee = catalog.Spawn("CoffeeMachine", root, new Vector3(.7f, 0f, -4.0f), Quaternion.Euler(0f, 180f, 0f), Vector3.one * .82f);
            Coffee = coffee.AddComponent<CoffeeStation>();
            Coffee.Configure(StationKind.Coffee, new Vector3(0f, 0f, -1f));

            GameObject vending = catalog.Spawn("VendingMachine", root, new Vector3(-1.2f, 0f, 4.0f), Quaternion.Euler(0f, 180f, 0f), Vector3.one * .82f);
            AddPlacementZone(vending, PlacementActivity.BuySnack, new Vector3(0f, 0f, -1f));

            GameObject breakArea = new GameObject("Cramped Break Area");
            breakArea.transform.SetParent(root, false);
            breakArea.transform.position = new Vector3(-3.2f, 0f, 3.75f);
            Break = breakArea.AddComponent<NeedStation>();
            Break.Configure(StationKind.Break, Vector3.zero);
            AddPlacementZone(breakArea, PlacementActivity.Rest, Vector3.zero);
            catalog.Spawn("WaitingBench", root, breakArea.transform.position + new Vector3(0f, 0f, .3f), Quaternion.identity, new Vector3(.72f,.9f,.72f));

            GameObject smoking = new GameObject("Starter Smoking Area");
            smoking.transform.SetParent(root, false);
            smoking.transform.position = new Vector3(-6.1f, 0f, -4.5f);
            AddPlacementZone(smoking, PlacementActivity.Smoke, Vector3.zero);
            catalog.Spawn("TrashBin", root, smoking.transform.position + new Vector3(.8f, 0f, .1f), Quaternion.identity, Vector3.one * .8f);
        }

        private void BuildClutter()
        {
            catalog.Spawn("FilingCabinet", root, new Vector3(-6.8f, 0f, -.2f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * .84f);
            catalog.Spawn("CardboardBox", root, new Vector3(-6.6f, 0f, -3.1f), Quaternion.Euler(0f, -9f, 0f), Vector3.one * .92f);
            catalog.Spawn("PaperStack", root, new Vector3(-1.2f, .78f, -1.7f), Quaternion.Euler(0f, 14f, 0f), Vector3.one * .65f);
            catalog.Spawn("PottedPlant", root, new Vector3(1.6f, 0f, -3.6f), Quaternion.identity, Vector3.one * .72f);
        }

        private void BuildLighting()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.19f, .25f, .25f);
            RenderSettings.ambientEquatorColor = new Color(.11f, .13f, .13f);
            RenderSettings.ambientGroundColor = new Color(.035f, .029f, .025f);
            RenderSettings.ambientIntensity = .58f;
            RenderSettings.fog = false;

            GameObject lightObject = new GameObject("Starter Office Window Light");
            lightObject.transform.SetParent(root, false);
            lightObject.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, .76f, .52f);
            light.intensity = 1.5f;
            light.shadows = LightShadows.Soft;
        }

        private void AddPlacementZone(GameObject owner, PlacementActivity activity, Vector3 localPoint)
        {
            ActivityPlacementZone zone = owner.AddComponent<ActivityPlacementZone>();
            zone.Configure(activity, localPoint);
            PlacementZones.Add(zone);
        }
    }
}
