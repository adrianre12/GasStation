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
            holderName = "batteries";
            energyPump = new PowerPump(stationCubeGrid);
            screen0 = new ScreenEnergy((IMyTextSurfaceProvider)block, 0, "Power", "KWh", "battery", "batteries", "Note: Power transfered is before charging losses");
        }
    }
}
