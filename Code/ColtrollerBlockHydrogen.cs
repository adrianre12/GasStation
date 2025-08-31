using Catopia.GasStation.Pump;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.Components;

namespace Catopia.GasStation
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_TextPanel), false, new[] { "GasStationControllerH2" })]
    public class ControllerBlockH2 : ControllerBlockBase
    {
        protected override void ControllerSetupClient()
        {
            NamePanel("NamePanelH2", true);
        }

        protected override void ControllerSetupServer()
        {
            holderName = "tanks";
            energyPump = new HydrogenPump(stationCubeGrid);
            screen0 = new ScreenEnergy((IMyTextSurfaceProvider)block, 0, "Gas", "KL", "tank", "tanks");
        }
    }
}
