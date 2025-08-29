using Catopia.GasStation.Pump;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.Components;

namespace Catopia.GasStation
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_TextPanel), false, new[] { "GasStationController" })]
    public class ControllerBlock : ControllerBlockBase
    {
        protected override void ControllerSetup()
        {
            energyPump = new HydrogenPump(stationCubeGrid);
            screen0 = new ScreenGas();
            screen0.Init((IMyTextSurfaceProvider)block, 0);
        }
    }
}
