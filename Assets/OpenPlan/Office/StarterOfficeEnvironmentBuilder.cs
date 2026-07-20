using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace OpenPlan
{
    /// <summary>Authored 14 m x 11 m Level One office and its visible locked neighboring unit.</summary>
    public sealed class StarterOfficeEnvironmentBuilder : IOfficeEnvironmentBuilder
    {
        private static readonly Color PartitionTint = new Color(.72f, .71f, .67f, 1f);
        private static readonly Color WallTint = new Color(.85f, .82f, .75f, 1f);
        private static readonly Color CharcoalTint = new Color(.20f, .21f, .22f, 1f);
        private static readonly Color NeighborTint = new Color(.19f, .23f, .26f, 1f);
        private static readonly Color CarpetTint = new Color(.325f, .38f, .43f, 1f);

        private readonly OfficeAssetCatalog catalog;
        private readonly Transform root;
        private readonly bool expanded;
        private GameObject connectingWall;
        private GameObject doorwayTrim;
        private OfficeObstacleVolume connectingObstacle;
        private Light neighborLight;
        private TextMeshPro neighborPurchaseLabel;
        private readonly List<Renderer> neighborRenderers = new List<Renderer>();

        public List<Workstation> Workstations { get; } = new List<Workstation>();
        public List<PlacementZone> PlacementZones { get; } = new List<PlacementZone>();
        public CoffeeStation Coffee { get; private set; }
        public WaterStation Water { get; private set; }
        public NeedStation Break { get; private set; }
        public NeedStation Elevator { get; private set; }
        public OfficeStageLayout Layout { get; private set; }

        public StarterOfficeEnvironmentBuilder(OfficeAssetCatalog assetCatalog, Transform parent, bool isExpanded)
        {
            catalog = assetCatalog;
            root = parent;
            expanded = isExpanded;
        }

        public void Build()
        {
            BuildLayoutContract();
            BuildFloorsAndWalls();
            BuildStarterDesks();
            BuildStarterAmenities();
            BuildLockedNeighbor();
            BuildBusinessDistrictScenery();
            BuildClutterAndDetails();
            BuildLighting();
            PlacementZones.AddRange(Workstations);
            BuildExpansionController();
        }

        private void BuildLayoutContract()
        {
            Layout = root.gameObject.AddComponent<OfficeStageLayout>();
            Layout.Configure(new Bounds(new Vector3(2f, 0f, 0f), new Vector3(25f, 4f, 15.5f)),
                expanded ? new Bounds(new Vector3(2f, 0f, 0f), new Vector3(26f, 1f, 10f)) :
                    new Bounds(new Vector3(-1f, 0f, 0f), new Vector3(16f, 1f, 9f)),
                expanded ? 13.2f : 11.8f,
                expanded ? new Bounds(new Vector3(3f, 0f, 0f), new Vector3(22f, 1f, 10.6f)) :
                    new Bounds(new Vector3(-1f, 0f, 0f), new Vector3(14f, 1f, 10.6f)),
                expanded ? (Bounds?)null :
                    new Bounds(new Vector3(10f, 0f, 0f), new Vector3(8f, 1f, 10.6f)));
            Layout.RegisterWalkableRegion(new Bounds(new Vector3(-10.05f, 0f, 3.35f),
                new Vector3(4.2f, 1f, 3.2f)));
            Layout.AddOverviewPoint("starter-southwest", new Vector3(-8f, 0f, -5.5f));
            Layout.AddOverviewPoint("starter-northeast", new Vector3(6f, 0f, 5.5f));
            Layout.AddOverviewPoint("neighbor-southeast", new Vector3(14f, 0f, -5.5f));
            Layout.AddOverviewPoint("neighbor-northeast", new Vector3(14f, 0f, 5.5f));
            Layout.AddOverviewPoint("main-entrance", new Vector3(-7.1f, 0f, 4.7f));
            Layout.AddOverviewPoint("smoking-area", new Vector3(-10.1f, 0f, 3.2f));

            AddPrimaryRoute("route.central", new Vector3(-.8f, 0f, 0f), new Vector3(12f, .12f, 1f));
            AddPrimaryRoute("route.entrance", new Vector3(-6.4f, 0f, 2.1f), new Vector3(1f, .12f, 3.2f));
        }

        private void BuildFloorsAndWalls()
        {
            GameObject starterFloor = catalog.Spawn("FloorSlab", root, new Vector3(-1f, 0f, 0f), Quaternion.identity, new Vector3(.467f, 1f, .50f));
            Tint(starterFloor, CarpetTint);
            GameObject neighborFloor = catalog.Spawn("FloorSlab", root, new Vector3(10f, -.02f, 0f), Quaternion.identity, new Vector3(.267f, 1f, .50f));
            AddNeighborRenderers(neighborFloor);
            Tint(neighborFloor, expanded ? new Color(.55f,.48f,.38f,1f) : new Color(.20f,.24f,.25f,1f));

            for (int i = 0; i < 4; i++)
                AddWall("wall.starter.north." + (i + 1), new Vector3(-6f + i * 4f, 0f, 5.5f), Quaternion.identity, Vector3.one);
            AddWall("wall.starter.west.south", new Vector3(-8f, 0f, -3.5f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            AddWall("wall.starter.west.middle", new Vector3(-8f, 0f, .5f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);

            AddWall("wall.shared.south", new Vector3(6f, 0f, -3.8f), Quaternion.Euler(0f, 90f, 0f), new Vector3(.85f,1f,1f));
            AddWall("wall.shared.north", new Vector3(6f, 0f, 3.8f), Quaternion.Euler(0f, 90f, 0f), new Vector3(.85f,1f,1f));
            connectingWall = catalog.Spawn("ConnectingWallTrim", root, new Vector3(6f, 0f, 0f),
                Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            connectingWall.name = "Removable Connecting Wall";
            connectingObstacle = AddObstacle(connectingWall, "wall.shared.removable",
                new Vector3(0f, .75f, 0f), new Vector3(3.6f, 1.5f, .28f));
            doorwayTrim = catalog.Spawn("ConnectingWallTrim", root, new Vector3(6f, 0f, 0f),
                Quaternion.Euler(0f, 90f, 0f), new Vector3(1f,1f,.22f));
            doorwayTrim.name = "Open Doorway Trim";
            doorwayTrim.SetActive(expanded);
            connectingWall.SetActive(!expanded);
            if (expanded)
            {
                connectingObstacle.VolumeCollider.enabled = false;
                Layout.UnregisterObstacle(connectingObstacle);
            }

            AddWall("wall.neighbor.north.1", new Vector3(8f, 0f, 5.5f), Quaternion.identity, Vector3.one);
            AddWall("wall.neighbor.north.2", new Vector3(12f, 0f, 5.5f), Quaternion.identity, Vector3.one);
            AddWall("wall.neighbor.east.south", new Vector3(14f, 0f, -3.5f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            AddWall("wall.neighbor.east.north", new Vector3(14f, 0f, 3.5f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
        }

        private void BuildStarterDesks()
        {
            BuildDesk("DamagedDesk", new Vector3(-5.0f,0f,-2.6f), Quaternion.Euler(0f,180f,0f),
                "starter.work.01", true, false, "OfficeChair", "CheapCRTMonitor", new Color(.52f,.32f,.25f));
            BuildDesk("Desk_A", new Vector3(-1.6f,0f,-2.6f), Quaternion.identity,
                "starter.work.02", true, false, "VisitorChair", "CheapCRTMonitor", new Color(.28f,.42f,.46f));
            BuildDesk("DamagedDesk", new Vector3(1.8f,0f,-2.6f), Quaternion.Euler(0f,180f,0f),
                "starter.work.03", true, false, "MeetingChair", "CheapCRTMonitor", new Color(.42f,.30f,.48f));

            Workstation future = BuildDesk("DamagedDesk", new Vector3(3.7f,0f,2.5f), Quaternion.identity,
                "starter.work.future", false, true, "VisitorChair", "CheapCRTMonitor", new Color(.32f,.34f,.35f));
            future.SetUnavailableReason("Reserved for a later milestone.");
            future.gameObject.AddComponent<FutureDeskLocation>().Configure("future.starter.desk", false, false);
            AddWorldLabel(future.transform, "Locked desk label", "DESK SLOT\nFUTURE MILESTONE", new Vector3(0f, 1.65f, 0f),
                new Color(.95f,.55f,.22f), .34f);
        }

        private void BuildStarterAmenities()
        {
            GameObject entryDoor = catalog.Spawn("Door", root, new Vector3(-7.1f, 0f, 5.35f), Quaternion.identity, Vector3.one);
            AddObstacle(entryDoor, "door.main", new Vector3(0f, 1.05f, 0f), new Vector3(1.15f, 2.2f, .22f));
            catalog.Spawn("ExitSign", root, new Vector3(-7.1f, 2.25f, 5.25f), Quaternion.identity, Vector3.one * .82f);

            GameObject exit = new GameObject("Main Entrance Placement");
            exit.transform.SetParent(root, false);
            exit.transform.position = new Vector3(-7.1f, 0f, 4.45f);
            Elevator = exit.AddComponent<NeedStation>();
            Elevator.Configure(StationKind.Elevator, Vector3.zero);
            AddPlacementZone(exit, PlacementActivity.LeaveOffice, Vector3.zero, "Leave Office",
                "starter.exit.main", true, new Vector2(1.1f, .85f), 1);

            GameObject breakArea = new GameObject("Cramped Break Nook");
            breakArea.transform.SetParent(root, false);
            breakArea.transform.position = new Vector3(-4.25f, 0f, 3.45f);
            Break = breakArea.AddComponent<NeedStation>();
            Break.Configure(StationKind.Break, Vector3.zero);
            AddPlacementZone(breakArea, PlacementActivity.Rest, Vector3.zero, "Rest",
                "starter.rest.break-nook", true, new Vector2(1.8f, 1.5f), 2);
            GameObject breakChair = catalog.Spawn("VisitorChair", root, new Vector3(-4.8f, 0f, 4.1f), Quaternion.Euler(0f,155f,0f), Vector3.one * .82f);
            Tint(breakChair, new Color(.46f,.28f,.22f));
            GameObject secondChair = catalog.Spawn("OfficeChair", root, new Vector3(-3.7f, 0f, 4.0f), Quaternion.Euler(0f,205f,0f), Vector3.one * .76f);
            Tint(secondChair, new Color(.24f,.38f,.40f));

            GameObject water = catalog.Spawn("WaterCooler", root, new Vector3(-1.8f, 0f, 4.45f), Quaternion.identity, Vector3.one * .84f);
            Water = water.AddComponent<WaterStation>();
            Water.Configure(StationKind.Water, new Vector3(0f, 0f, -1.0f));
            AddPlacementZone(water, PlacementActivity.GetWater, new Vector3(0f,0f,-1.0f), "Get Water",
                "starter.water.cooler", true, new Vector2(1.2f, 1f), 1);

            GameObject vending = catalog.Spawn("CheapVendingMachine", root, new Vector3(.25f, 0f, 4.5f), Quaternion.identity, Vector3.one * .88f);
            AddPlacementZone(vending, PlacementActivity.BuySnack, new Vector3(0f,0f,-1.05f), "Buy Snack",
                "starter.snack.vending", true, new Vector2(1.35f, 1f), 1);

            GameObject coffee = catalog.Spawn("CoffeeMachine", root, new Vector3(2.2f, 0f, 4.55f), Quaternion.identity, Vector3.one * .72f);
            Coffee = coffee.AddComponent<CoffeeStation>();
            Coffee.Configure(StationKind.Coffee, new Vector3(0f,0f,-.9f));
            AddPlacementZone(coffee, PlacementActivity.GetCoffee, new Vector3(0f,0f,-.9f), "Get Coffee",
                "starter.coffee.machine", true, new Vector2(1.2f, 1f), 1);

            GameObject restroom = new GameObject("Starter Restroom Entrance");
            restroom.transform.SetParent(root, false);
            restroom.transform.position = new Vector3(-7.25f, 0f, .10f);
            AddPlacementZone(restroom, PlacementActivity.UseRestroom, Vector3.zero, "Use Restroom",
                "starter.restroom.main", true, new Vector2(.65f, .60f), 1);
            GameObject restroomDoor = catalog.Spawn("Door", root, new Vector3(-7.86f, 0f, .10f),
                Quaternion.Euler(0f, 90f, 0f), new Vector3(.82f, .92f, .82f));
            Tint(restroomDoor, new Color(.77f, .76f, .70f));
            AddWorldLabel(restroom.transform, "Restroom label", "RESTROOM\nPLACE NEAR DOOR",
                new Vector3(-.55f, 1.75f, 0f), new Color(.48f, .91f, .92f), .30f);

            GameObject smoke = new GameObject("Exterior Smoking Area");
            smoke.transform.SetParent(root, false);
            smoke.transform.position = new Vector3(-10.05f, 0f, 3.15f);
            AddPlacementZone(smoke, PlacementActivity.Smoke, Vector3.zero, "Smoke",
                "starter.smoke.exterior", true, new Vector2(1.8f, 1.5f), 2);
            catalog.Spawn("Ashtray", root, smoke.transform.position + new Vector3(.75f,0f,.1f), Quaternion.identity, Vector3.one);
            catalog.Spawn("Cigarette", root, smoke.transform.position + new Vector3(.75f,.92f,.1f), Quaternion.Euler(0f,20f,90f), Vector3.one);
            catalog.Spawn("WaitingBench", root, smoke.transform.position + new Vector3(-.55f,0f,.55f), Quaternion.Euler(0f,135f,0f), Vector3.one * .68f);
            GameObject smokeFloor = catalog.Spawn("FloorSlab", root, smoke.transform.position + new Vector3(0f,-.03f,.2f),
                Quaternion.identity, new Vector3(.12f,1f,.16f));
            Tint(smokeFloor, new Color(.18f,.22f,.25f));
            GameObject smokeBack = catalog.Spawn("PartialWall", root, smoke.transform.position + new Vector3(0f,0f,1.45f),
                Quaternion.identity, new Vector3(.72f,.82f,1f));
            GameObject smokeSide = catalog.Spawn("PartialWall", root, smoke.transform.position + new Vector3(-1.45f,0f,.2f),
                Quaternion.Euler(0f,90f,0f), new Vector3(.62f,.82f,1f));
            Tint(smokeBack, new Color(.31f,.32f,.32f));
            Tint(smokeSide, new Color(.31f,.32f,.32f));
            AddObstacle(smokeBack, "wall.smoking.back", new Vector3(0f,.75f,0f), new Vector3(4f,1.55f,.24f));
            AddObstacle(smokeSide, "wall.smoking.side", new Vector3(0f,.75f,0f), new Vector3(4f,1.55f,.24f));
            AddWorldLabel(smoke.transform, "Smoking label", "SMOKING AREA", new Vector3(0f, 1.7f, .2f),
                new Color(.96f,.68f,.34f), .32f);
        }

        private void BuildLockedNeighbor()
        {
            GameObject sign = catalog.Spawn("NeighborSign", root, new Vector3(10f, .05f, 3.85f), Quaternion.identity, Vector3.one);
            AddNeighborRenderers(sign);
            Tint(sign, expanded ? new Color(.70f,.58f,.36f) : NeighborTint);
            neighborPurchaseLabel = AddWorldLabel(sign.transform, "Neighbor purchase sign",
                expanded ? "UNIT NEXT DOOR\nPURCHASED" : "UNIT NEXT DOOR\nLOCKED  -  $1,000",
                new Vector3(0f, 1.25f, -.18f), expanded ? new Color(.55f,1f,.68f) : new Color(1f,.72f,.30f), .64f);

            Vector3[] positions =
            {
                new Vector3(8.1f,0f,-2.5f), new Vector3(11.1f,0f,-2.5f), new Vector3(8.1f,0f,1.2f)
            };
            for (int i = 0; i < positions.Length; i++)
            {
                Workstation future = BuildDesk(i == 1 ? "Desk_A" : "DamagedDesk", positions[i],
                    i % 2 == 0 ? Quaternion.Euler(0f,180f,0f) : Quaternion.identity,
                    $"neighbor.work.{i + 1:00}", expanded, true, i == 1 ? "OfficeChair" : "VisitorChair",
                    "CheapCRTMonitor", NeighborTint);
                AddNeighborRenderers(future.gameObject);
                future.gameObject.AddComponent<FutureDeskLocation>().Configure($"future.neighbor.desk.{i + 1:00}", true, expanded);
                if (!expanded)
                {
                    future.SetUnavailableReason("Area locked.");
                    Tint(future.gameObject, NeighborTint);
                }
            }

            GameObject utility = new GameObject("Neighbor Utility Corner");
            utility.transform.SetParent(root, false);
            utility.transform.position = new Vector3(11.8f, 0f, 3.45f);
            AddPlacementZone(utility, PlacementActivity.Rest, Vector3.zero, "Rest",
                "neighbor.rest.utility", expanded, new Vector2(1.6f, 1.4f), 2);
            if (!expanded) utility.GetComponent<PlacementZone>().SetUnavailableReason("Area locked.");
            GameObject counter = catalog.Spawn("Counter", root, new Vector3(12.3f,0f,4.45f), Quaternion.identity, new Vector3(.62f,.85f,.72f));
            GameObject chair = catalog.Spawn("VisitorChair", root, new Vector3(11.5f,0f,3.9f), Quaternion.Euler(0f,160f,0f), Vector3.one * .70f);
            AddNeighborRenderers(counter);
            AddNeighborRenderers(chair);
            if (!expanded) { Tint(counter, NeighborTint); Tint(chair, NeighborTint); }
        }

        private void BuildBusinessDistrictScenery()
        {
            Transform scenery = new GameObject("Business District Scenery").transform;
            scenery.SetParent(root, false);

            GameObject leftFacade = catalog.Spawn("PartialWall", scenery, new Vector3(-12.3f,0f,5.7f), Quaternion.identity, new Vector3(1.7f,1.35f,1f));
            Tint(leftFacade, new Color(.32f,.24f,.26f));
            catalog.Spawn("WindowModule", scenery, new Vector3(-12.3f,.2f,5.45f), Quaternion.identity, new Vector3(1.45f,.75f,.8f));
            GameObject leftSign = catalog.Spawn("CompanySign", scenery, new Vector3(-12.3f,1.25f,5.25f), Quaternion.identity, new Vector3(.75f,.7f,.75f));
            Tint(leftSign, new Color(.38f,.24f,.22f));

            GameObject rightFacade = catalog.Spawn("PartialWall", scenery, new Vector3(17.2f,0f,5.7f), Quaternion.identity, new Vector3(1.55f,1.35f,1f));
            Tint(rightFacade, new Color(.22f,.29f,.31f));
            catalog.Spawn("WindowModule", scenery, new Vector3(17.2f,.2f,5.45f), Quaternion.identity, new Vector3(1.30f,.75f,.8f));
            GameObject rightSign = catalog.Spawn("NeighborSign", scenery, new Vector3(17.2f,.8f,5.25f), Quaternion.identity, new Vector3(.72f,.72f,.72f));
            Tint(rightSign, new Color(.25f,.33f,.35f));

            AddWorldLabel(leftSign.transform, "Scenery cafe label", "CAFE", new Vector3(0f,.8f,-.15f),
                new Color(.83f,.65f,.48f), .32f);
            AddWorldLabel(rightSign.transform, "Scenery office label", "OFFICE TO LET", new Vector3(0f,.8f,-.15f),
                new Color(.58f,.75f,.78f), .28f);
        }

        private void BuildClutterAndDetails()
        {
            catalog.Spawn("FilingCabinet", root, new Vector3(-7.1f,0f,-.8f), Quaternion.Euler(0f,90f,0f), Vector3.one * .80f);
            catalog.Spawn("CardboardBox", root, new Vector3(-6.9f,0f,-3.9f), Quaternion.Euler(0f,-9f,0f), Vector3.one);
            catalog.Spawn("CardboardBox", root, new Vector3(-6.25f,0f,-4.2f), Quaternion.Euler(0f,14f,0f), Vector3.one * .72f);
            catalog.Spawn("CardboardBox", root, new Vector3(4.9f,0f,4.25f), Quaternion.Euler(0f,-18f,0f), Vector3.one * .78f);
            catalog.Spawn("PaperStack", root, new Vector3(-5f,.79f,-2.55f), Quaternion.Euler(0f,14f,0f), Vector3.one * .65f);
            catalog.Spawn("PaperStack", root, new Vector3(1.8f,.79f,-2.55f), Quaternion.Euler(0f,-9f,0f), Vector3.one * .72f);
            catalog.Spawn("FileTray", root, new Vector3(-1.25f,.79f,-2.55f), Quaternion.Euler(0f,8f,0f), Vector3.one * .68f);
            catalog.Spawn("TrashBin", root, new Vector3(3.3f,0f,4.3f), Quaternion.identity, Vector3.one * .72f);
            catalog.Spawn("Clock", root, new Vector3(-4.0f,1.45f,5.28f), Quaternion.identity, Vector3.one * .72f);
            catalog.Spawn("NoticeBoard", root, new Vector3(3.0f,1.0f,5.30f), Quaternion.identity, new Vector3(.72f,.72f,.72f));
            catalog.Spawn("PottedPlant", root, new Vector3(-7.0f,0f,1.8f), Quaternion.identity, Vector3.one * .68f);

            Vector3[] dividerPositions =
            {
                new Vector3(-6.0f,0f,-1.7f), new Vector3(-3.2f,0f,-1.7f),
                new Vector3(-.6f,0f,-1.7f), new Vector3(2.8f,0f,-1.7f)
            };
            foreach (Vector3 position in dividerPositions)
            {
                GameObject divider = catalog.Spawn("CubicleDivider", root, position, Quaternion.identity, new Vector3(.72f,.82f,.82f));
                Tint(divider, PartitionTint);
            }
            catalog.Spawn("DeskLamp", root, new Vector3(-4.55f,.79f,-2.55f), Quaternion.Euler(0f,20f,0f), Vector3.one * .62f);
            catalog.Spawn("DeskLamp", root, new Vector3(2.2f,.79f,-2.55f), Quaternion.Euler(0f,-18f,0f), Vector3.one * .62f);
            catalog.Spawn("Mug", root, new Vector3(-1.05f,.81f,-2.48f), Quaternion.identity, Vector3.one * .58f);
            catalog.Spawn("Mouse", root, new Vector3(-1.75f,.81f,-2.78f), Quaternion.identity, Vector3.one * .62f);
            catalog.Spawn("Mouse", root, new Vector3(1.62f,.81f,-2.78f), Quaternion.identity, Vector3.one * .62f);
            catalog.Spawn("FileTray", root, new Vector3(-6.35f,.02f,1.1f), Quaternion.Euler(0f,90f,0f), Vector3.one * .75f);
            catalog.Spawn("RecyclingBin", root, new Vector3(-2.7f,0f,-3.5f), Quaternion.identity, Vector3.one * .72f);
        }

        private void BuildLighting()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.30f, .34f, .36f);
            RenderSettings.ambientEquatorColor = new Color(.17f, .19f, .20f);
            RenderSettings.ambientGroundColor = new Color(.035f, .045f, .055f);
            RenderSettings.ambientIntensity = .62f;
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(.035f,.026f,.022f);
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = .004f;

            GameObject keyObject = new GameObject("Cheap Warm Window Light");
            keyObject.transform.SetParent(root, false);
            keyObject.transform.rotation = Quaternion.Euler(52f,-38f,0f);
            Light key = keyObject.AddComponent<Light>();
            key.type = LightType.Directional;
            key.color = new Color(1f,.72f,.52f);
            key.intensity = expanded ? 1.48f : 1.24f;
            key.shadows = LightShadows.Soft;
            key.shadowStrength = .55f;

            AddPointLight("Break nook lamp", new Vector3(-4.2f,2.4f,3.5f), new Color(1f,.48f,.22f), 4.2f, 1.9f);
            AddPointLight("Starter utility lamp", new Vector3(.2f,2.3f,4.0f), new Color(.95f,.54f,.28f), 4.4f, 1.7f);
            Light waterLight = AddPointLight("Water cooler cyan practical", new Vector3(-1.8f,1.9f,4.1f),
                new Color(.44f,.84f,.91f), 3.4f, 2.25f);
            waterLight.shadows = LightShadows.Soft;
            AddPointLight("Restroom entrance practical", new Vector3(-7.1f,2.0f,.10f),
                new Color(.54f,.88f,.86f), 3.0f, 1.25f);
            Light smokeLight = AddPointLight("Smoking alcove practical", new Vector3(-10.1f,2.0f,3.8f),
                new Color(1f,.48f,.25f), 3.8f, 2.1f);
            smokeLight.shadows = LightShadows.Soft;
            AddPointLight("Smoking alcove office spill", new Vector3(-8.55f,1.8f,3.15f),
                new Color(.42f,.72f,.82f), 3.2f, .85f);
            neighborLight = AddPointLight("Neighbor dim practical", new Vector3(10.2f,2.4f,.2f), new Color(.24f,.42f,.46f), 5.2f, expanded ? 1.8f : .48f);
        }

        private Workstation BuildDesk(string deskAsset, Vector3 position, Quaternion facing, string stableIdentifier,
            bool zoneEnabled, bool expansion, string chairAsset, string monitorAsset, Color chairTint)
        {
            GameObject desk = catalog.Spawn(deskAsset, root, position, facing, Vector3.one);
            Tint(desk, stableIdentifier.Contains("neighbor") && !expanded ? NeighborTint : CharcoalTint);
            Workstation station = desk.AddComponent<Workstation>();
            int index = Workstations.Count;
            float noise = stableIdentifier.Contains("neighbor") ? .38f : index == 0 ? .22f : index == 1 ? .60f : .42f;
            float light = stableIdentifier.Contains("neighbor") ? .42f : index == 2 ? .78f : .60f;
            station.Configure(index, noise, light, Mathf.Lerp(.93f,1.05f,light),
                expansion ? "Future desk location" : "Modest starter desk", expansion, stableIdentifier, zoneEnabled);
            AddObstacle(desk, "furniture." + stableIdentifier, new Vector3(0f,.48f,0f),
                new Vector3(1.65f,1f,.82f));
            Workstations.Add(station);

            Vector3 forward = facing * Vector3.forward;
            GameObject chair = catalog.Spawn(chairAsset, root, position - forward * .88f, facing, Vector3.one * .84f);
            Tint(chair, chairTint);
            GameObject monitor = catalog.Spawn(monitorAsset, root, position + Vector3.up * .76f + forward * .12f, facing, Vector3.one * .78f);
            if (!zoneEnabled) Tint(monitor, NeighborTint);
            catalog.Spawn("Keyboard", root, position + Vector3.up * .79f - forward * .21f, facing, Vector3.one * .68f);
            return station;
        }

        private void AddPlacementZone(GameObject owner, PlacementActivity activity, Vector3 localPoint, string label,
            string stableIdentifier, bool zoneEnabled, Vector2 footprint, int capacity)
        {
            ActivityPlacementZone zone = owner.AddComponent<ActivityPlacementZone>();
            zone.Configure(activity, localPoint, label, stableIdentifier, zoneEnabled, footprint, capacity);
            PlacementZones.Add(zone);
        }

        private void AddWall(string identifier, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            GameObject wall = catalog.Spawn("PartialWall", root, position, rotation, scale);
            Tint(wall, WallTint);
            AddObstacle(wall, identifier, new Vector3(0f,.75f,0f), new Vector3(4f * scale.x,1.55f,.24f));
        }

        private OfficeObstacleVolume AddObstacle(GameObject owner, string identifier, Vector3 localCenter, Vector3 size)
        {
            OfficeObstacleVolume obstacle = owner.AddComponent<OfficeObstacleVolume>();
            obstacle.Configure(identifier, localCenter, size);
            Layout.RegisterObstacle(obstacle);
            return obstacle;
        }

        private void AddPrimaryRoute(string identifier, Vector3 position, Vector3 size)
        {
            GameObject routeObject = new GameObject(identifier);
            routeObject.transform.SetParent(root, false);
            routeObject.transform.position = position;
            PrimaryRouteVolume route = routeObject.AddComponent<PrimaryRouteVolume>();
            route.Configure(identifier, new Vector3(0f,.06f,0f), size);
            Layout.RegisterPrimaryRoute(route);
        }

        private Light AddPointLight(string name, Vector3 position, Color color, float range, float intensity)
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
            return light;
        }

        private static TextMeshPro AddWorldLabel(Transform parent, string name, string copy, Vector3 localPosition, Color color, float scale)
        {
            GameObject labelObject = new GameObject(name);
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = localPosition;
            labelObject.transform.localScale = Vector3.one * scale;
            TextMeshPro text = labelObject.AddComponent<TextMeshPro>();
            text.text = copy;
            text.fontSize = 3.4f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.enableWordWrapping = false;
            text.rectTransform.sizeDelta = new Vector2(7f, 2.5f);
            labelObject.AddComponent<WorldSpaceOfficeLabel>();
            return text;
        }

        private void AddNeighborRenderers(GameObject owner)
        {
            if (owner == null) return;
            foreach (Renderer renderer in owner.GetComponentsInChildren<Renderer>(true))
                if (renderer != null && !neighborRenderers.Contains(renderer)) neighborRenderers.Add(renderer);
        }

        private void BuildExpansionController()
        {
            OfficeExpansionController controller = root.gameObject.AddComponent<OfficeExpansionController>();
            controller.Configure(expanded, connectingWall, connectingObstacle, doorwayTrim, neighborLight,
                neighborPurchaseLabel, neighborRenderers.ToArray(), Workstations.ToArray(), PlacementZones.ToArray(),
                root.GetComponentsInChildren<FutureDeskLocation>(true));
        }

        private static void Tint(GameObject owner, Color color)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            foreach (Renderer renderer in owner.GetComponentsInChildren<Renderer>(true))
            {
                renderer.GetPropertyBlock(block);
                block.SetColor("_BaseColor", color);
                renderer.SetPropertyBlock(block);
            }
        }
    }

    public sealed class WorldSpaceOfficeLabel : MonoBehaviour
    {
        private void LateUpdate()
        {
            Camera camera = Camera.main;
            if (camera == null) return;
            Vector3 awayFromCamera = transform.position - camera.transform.position;
            if (awayFromCamera.sqrMagnitude > .01f)
                transform.rotation = Quaternion.LookRotation(awayFromCamera.normalized, Vector3.up);
        }
    }
}
