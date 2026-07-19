using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OpenPlan
{
    /// <summary>The preserved released large office, used by the Established Office stage.</summary>
    public sealed class OfficeEnvironmentBuilder : IOfficeEnvironmentBuilder
    {
        private readonly OfficeAssetCatalog catalog;
        private readonly Transform root;
        public List<Workstation> Workstations { get; } = new List<Workstation>();
        public List<PlacementZone> PlacementZones { get; } = new List<PlacementZone>();
        public CoffeeStation Coffee { get; private set; }
        public WaterStation Water { get; private set; }
        public NeedStation Break { get; private set; }
        public NeedStation Elevator { get; private set; }

        public OfficeEnvironmentBuilder(OfficeAssetCatalog assetCatalog, Transform parent)
        {
            catalog = assetCatalog;
            root = parent;
        }

        public void Build()
        {
            catalog.Spawn("FloorSlab", root, Vector3.zero, Quaternion.identity, Vector3.one);
            BuildShell();
            BuildReception();
            BuildDesks();
            BuildAmenities();
            BuildMeetingAndManager();
            BuildUtilityAndClutter();
            BuildLighting();
            PlacementZones.AddRange(Workstations);
        }

        private void BuildShell()
        {
            for (int i = -4; i <= 4; i += 2)
                catalog.Spawn("WindowModule", root, new Vector3(i * 3.1f, 0f, 10.55f), Quaternion.identity, Vector3.one);
            for (int i = -2; i <= 2; i++)
                catalog.Spawn("PartialWall", root, new Vector3(-14.65f, 0f, i * 4.25f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            catalog.Spawn("PartialWall", root, new Vector3(13.9f, 0f, 8.9f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            foreach (Vector3 position in new[] { new Vector3(-14f,0f,-10f), new Vector3(14f,0f,-10f), new Vector3(-14f,0f,10f), new Vector3(14f,0f,10f) })
                catalog.Spawn("StructuralColumn", root, position, Quaternion.identity, Vector3.one);
        }

        private void BuildReception()
        {
            catalog.Spawn("Elevator", root, new Vector3(-10.8f, 0f, 8.7f), Quaternion.Euler(0f, 180f, 0f), Vector3.one);
            GameObject elevatorPoint = new GameObject("ElevatorStation");
            elevatorPoint.transform.SetParent(root, false);
            elevatorPoint.transform.position = new Vector3(-10.8f, 0f, 7.25f);
            Elevator = elevatorPoint.AddComponent<NeedStation>();
            Elevator.Configure(StationKind.Elevator, Vector3.zero);
            AddPlacementZone(elevatorPoint, PlacementActivity.LeaveOffice, Vector3.zero);
            catalog.Spawn("ReceptionDesk", root, new Vector3(-7.3f, 0f, 6.9f), Quaternion.Euler(0f, 16f, 0f), Vector3.one);
            catalog.Spawn("CompanySign", root, new Vector3(-7.3f, 1.25f, 9.95f), Quaternion.identity, Vector3.one);
            catalog.Spawn("WaitingBench", root, new Vector3(-11.5f, 0f, 4.8f), Quaternion.Euler(0f, -90f, 0f), Vector3.one);
            catalog.Spawn("TallPlant", root, new Vector3(-13f, 0f, 7.3f), Quaternion.identity, Vector3.one);
        }

        private void BuildDesks()
        {
            Vector3[] positions =
            {
                new Vector3(-7f,0f,-5.8f), new Vector3(-3f,0f,-5.8f), new Vector3(1f,0f,-5.8f), new Vector3(5f,0f,-5.8f),
                new Vector3(-7f,0f,-1.8f), new Vector3(-3f,0f,-1.8f), new Vector3(1f,0f,-1.8f), new Vector3(5f,0f,-1.8f),
                new Vector3(-7f,0f,2.2f), new Vector3(-3f,0f,2.2f), new Vector3(1f,0f,2.2f), new Vector3(5f,0f,2.2f)
            };
            float[] noise = { .14f, .34f, .54f, .72f, .20f, .46f, .78f, .66f, .28f, .42f, .62f, .37f };
            float[] light = { .86f, .74f, .68f, .57f, .92f, .78f, .65f, .60f, .88f, .75f, .62f, .72f };
            string[] zones = { "Quiet corner bonus", "Calm workstation", "Central lane", "Coffee traffic", "Window light bonus", "Balanced desk", "Social cluster", "Water-cooler chatter", "Expansion quiet desk", "Expansion desk", "Expansion social desk", "Expansion desk" };
            for (int i = 0; i < positions.Length; i++)
            {
                Quaternion facing = i % 2 == 0 ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
                GameObject desk = catalog.Spawn(i % 4 == 3 ? "Desk_B" : "Desk_A", root, positions[i], facing, Vector3.one);
                Workstation station = desk.AddComponent<Workstation>();
                float modifier = Mathf.Lerp(0.90f, 1.10f, light[i]) * Mathf.Lerp(1.06f, 0.94f, noise[i]);
                station.Configure(i, noise[i], light[i], modifier, zones[i], i >= 8);
                Workstations.Add(station);
                Vector3 forward = facing * Vector3.forward;
                catalog.Spawn("OfficeChair", root, positions[i] - forward * 0.92f, facing, Vector3.one * 0.92f);
                catalog.Spawn("Monitor", root, positions[i] + Vector3.up * 0.78f + forward * 0.14f, facing, Vector3.one * 0.82f);
                catalog.Spawn("Keyboard", root, positions[i] + Vector3.up * 0.82f - forward * 0.22f, facing, Vector3.one * 0.82f);
                if (i % 3 == 0) catalog.Spawn("Mug", root, positions[i] + Vector3.up * 0.82f + facing * new Vector3(.55f,0f,-.15f), facing, Vector3.one * .8f);
                if (i % 4 == 1) catalog.Spawn("DeskPlant", root, positions[i] + Vector3.up * .82f + facing * new Vector3(-.55f,0f,.05f), facing, Vector3.one * .72f);
                if (i < 8 && i % 2 == 0)
                    catalog.Spawn("CubicleDivider", root, positions[i] + new Vector3(0f, 0f, 1.02f), Quaternion.identity, Vector3.one);
            }
        }

        private void BuildAmenities()
        {
            GameObject waterObject = catalog.Spawn("WaterCooler", root, new Vector3(10.6f, 0f, -0.5f), Quaternion.Euler(0f, -90f, 0f), Vector3.one);
            Water = waterObject.AddComponent<WaterStation>();
            Water.Configure(StationKind.Water, new Vector3(-1.05f, 0f, 0f));
            AddPlacementZone(waterObject, PlacementActivity.GetWater, new Vector3(-1.05f, 0f, 0f));
            catalog.Spawn("NoticeBoard", root, new Vector3(13.75f, 1.1f, 0f), Quaternion.Euler(0f, -90f, 0f), Vector3.one);

            GameObject coffeeObject = catalog.Spawn("CoffeeMachine", root, new Vector3(10.8f, 0f, -6.9f), Quaternion.Euler(0f, -90f, 0f), Vector3.one);
            Coffee = coffeeObject.AddComponent<CoffeeStation>();
            Coffee.Configure(StationKind.Coffee, new Vector3(-1.05f, 0f, 0f));
            GameObject vending = catalog.Spawn("VendingMachine", root, new Vector3(10.8f, 0f, -8.5f), Quaternion.Euler(0f, -90f, 0f), Vector3.one);
            AddPlacementZone(vending, PlacementActivity.BuySnack, new Vector3(-1.05f, 0f, 0f));
            catalog.Spawn("Counter", root, new Vector3(8.6f, 0f, -9.0f), Quaternion.identity, Vector3.one);
            catalog.Spawn("TrashBin", root, new Vector3(12.7f, 0f, -6.7f), Quaternion.identity, Vector3.one);
            catalog.Spawn("ConferenceTable", root, new Vector3(9.0f, 0f, -5.2f), Quaternion.identity, new Vector3(.58f,.9f,.58f));
            GameObject breakObject = new GameObject("BreakStation");
            breakObject.transform.SetParent(root, false);
            breakObject.transform.position = new Vector3(8.8f, 0f, -5.3f);
            Break = breakObject.AddComponent<NeedStation>();
            Break.Configure(StationKind.Break, Vector3.zero);
            AddPlacementZone(breakObject, PlacementActivity.Rest, Vector3.zero);

            GameObject smoking = new GameObject("Established Smoking Area");
            smoking.transform.SetParent(root, false);
            smoking.transform.position = new Vector3(12.2f, 0f, 9.0f);
            AddPlacementZone(smoking, PlacementActivity.Smoke, Vector3.zero);
        }

        private void BuildMeetingAndManager()
        {
            catalog.Spawn("GlassWall", root, new Vector3(8.0f, 0f, 4.5f), Quaternion.identity, new Vector3(1.7f,1f,1f));
            catalog.Spawn("GlassWall", root, new Vector3(5.8f, 0f, 7.4f), Quaternion.Euler(0f, 90f, 0f), new Vector3(1.45f,1f,1f));
            catalog.Spawn("ConferenceTable", root, new Vector3(9.3f, 0f, 7.0f), Quaternion.identity, Vector3.one);
            for (int i = 0; i < 4; i++)
            {
                float x = 8.1f + i * .8f;
                catalog.Spawn("MeetingChair", root, new Vector3(x, 0f, 5.9f), Quaternion.identity, Vector3.one * .82f);
                catalog.Spawn("MeetingChair", root, new Vector3(x, 0f, 8.1f), Quaternion.Euler(0f,180f,0f), Vector3.one * .82f);
            }
            catalog.Spawn("Whiteboard", root, new Vector3(12.9f, 1.1f, 8.7f), Quaternion.Euler(0f,-90f,0f), Vector3.one);
            catalog.Spawn("DisplayScreen", root, new Vector3(9.3f, 1.25f, 9.85f), Quaternion.identity, Vector3.one);

            catalog.Spawn("Desk_B", root, new Vector3(-2.0f, 0f, 7.1f), Quaternion.Euler(0f,180f,0f), new Vector3(1.22f,1f,1.1f));
            catalog.Spawn("Bookshelf", root, new Vector3(-4.9f, 0f, 9.7f), Quaternion.identity, Vector3.one);
            catalog.Spawn("TallPlant", root, new Vector3(.4f, 0f, 8.8f), Quaternion.identity, Vector3.one);
            catalog.Spawn("DisplayScreen", root, new Vector3(-2.0f, 1.15f, 9.8f), Quaternion.identity, new Vector3(.85f,.8f,.85f));
        }

        private void BuildUtilityAndClutter()
        {
            catalog.Spawn("Copier", root, new Vector3(-11.8f, 0f, -5.7f), Quaternion.Euler(0f,90f,0f), Vector3.one);
            catalog.Spawn("Printer", root, new Vector3(-11.6f, 0f, -3.8f), Quaternion.Euler(0f,90f,0f), Vector3.one);
            catalog.Spawn("FilingCabinet", root, new Vector3(-13.0f, 0f, -1.8f), Quaternion.Euler(0f,90f,0f), Vector3.one);
            catalog.Spawn("RecyclingBin", root, new Vector3(-10.8f, 0f, -5.5f), Quaternion.identity, Vector3.one);
            catalog.Spawn("Clock", root, new Vector3(-13.8f, 1.5f, 1.8f), Quaternion.Euler(0f,90f,0f), Vector3.one);
            catalog.Spawn("ExitSign", root, new Vector3(-10.8f, 2.35f, 9.55f), Quaternion.identity, Vector3.one);
            foreach (Vector3 p in new[] { new Vector3(-12.5f,0f,-8.3f), new Vector3(12.5f,0f,3f), new Vector3(3.5f,0f,8.9f), new Vector3(7f,0f,-9f) })
                catalog.Spawn("PottedPlant", root, p, Quaternion.identity, Vector3.one);
            for (int i = 0; i < 9; i++)
                catalog.Spawn(i % 2 == 0 ? "PaperStack" : "FileTray", root,
                    new Vector3(-11.8f + (i % 3) * .5f, .72f + (i % 2) * .05f, -2.8f + (i / 3) * .48f),
                    Quaternion.Euler(0f, i * 11f, 0f), Vector3.one * .75f);
        }

        private void BuildLighting()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.20f, .34f, .38f);
            RenderSettings.ambientEquatorColor = new Color(.12f, .18f, .19f);
            RenderSettings.ambientGroundColor = new Color(.035f, .025f, .022f);
            RenderSettings.ambientIntensity = .62f;
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(.035f, .021f, .018f);
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = .006f;

            GameObject sun = new GameObject("Warm Window Key");
            sun.transform.SetParent(root, false);
            sun.transform.rotation = Quaternion.Euler(48f, -38f, 0f);
            Light key = sun.AddComponent<Light>();
            key.type = LightType.Directional;
            key.color = new Color(1f, .64f, .34f);
            key.intensity = 2.1f;
            key.shadows = LightShadows.Soft;
            key.shadowStrength = .72f;

            CreatePoint("Water cyan practical", new Vector3(10f, 2.6f, -0.5f), new Color(.18f,.74f,.86f), 5.5f, 3.0f);
            CreatePoint("Coffee amber practical", new Vector3(10f, 2.4f, -6.8f), new Color(1f,.38f,.12f), 5.2f, 3.2f);
            CreatePoint("Meeting cool practical", new Vector3(9f, 2.7f, 7f), new Color(.22f,.52f,.62f), 6.5f, 2.2f);
        }

        private void CreatePoint(string name, Vector3 position, Color color, float range, float intensity)
        {
            GameObject lightObject = new GameObject(name);
            lightObject.transform.SetParent(root, false);
            lightObject.transform.position = position;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
        }

        private void AddPlacementZone(GameObject owner, PlacementActivity activity, Vector3 localPoint)
        {
            ActivityPlacementZone zone = owner.AddComponent<ActivityPlacementZone>();
            zone.Configure(activity, localPoint);
            PlacementZones.Add(zone);
        }
    }
}
