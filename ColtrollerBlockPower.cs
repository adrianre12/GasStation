using Catopia.GasStation.Pump;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.Components;

namespace Catopia.GasStation
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_TextPanel), false, new[] { "GasStationControllerPower" })]
    public class ControllerBlockPower : ControllerBlockBase
    {
        protected override void ControllerSetupClient()
        {
            NamePanel("NamePanelPower", true);
        }

        protected override void ControllerSetupServer()
        {
            energyPump = new HydrogenPump(stationCubeGrid);
            screen0 = new ScreenGas();
            screen0.Init((IMyTextSurfaceProvider)block, 0);
        }
    }
}
