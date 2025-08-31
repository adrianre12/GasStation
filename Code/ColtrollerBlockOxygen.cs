using Catopia.GasStation.Pump;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.Components;

namespace Catopia.GasStation
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_TextPanel), false, new[] { "GasStationControllerO2" })]
    public class ControllerBlockO2 : ControllerBlockBase
    {
        protected override void ControllerSetupClient()
        {
            NamePanel("NamePanelO2", true);
        }

        protected override void ControllerSetupServer()
        {
            holderName = "tanks";
            energyPump = new OxygenPump(stationCubeGrid);
            screen0 = new ScreenEnergy((IMyTextSurfaceProvider)block, 0, "Gas", "KL", "tank", "tanks");
        }
    }
}
