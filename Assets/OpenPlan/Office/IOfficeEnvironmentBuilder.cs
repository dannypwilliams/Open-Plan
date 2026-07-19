using System.Collections.Generic;

namespace OpenPlan
{
    public interface IOfficeEnvironmentBuilder
    {
        List<Workstation> Workstations { get; }
        List<PlacementZone> PlacementZones { get; }
        CoffeeStation Coffee { get; }
        WaterStation Water { get; }
        NeedStation Break { get; }
        NeedStation Elevator { get; }
        void Build();
    }
}
