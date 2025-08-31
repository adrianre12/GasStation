using Catopia.GasStation.Energy;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace Catopia.GasStation.Pump
{
    internal class PowerPump : PumpBase
    {
        public PowerPump(IMyCubeGrid cubeGrid) : base(cubeGrid)
        {
        }

        protected override void CheckAndAddHolder(MyCubeBlock fatBlock, bool isTarget, bool isConnected, ref EnergyHolders energyHolders)
        {
            IMyBatteryBlock battery;
            if ((battery = fatBlock as IMyBatteryBlock) != null && battery.Enabled && battery.IsFunctional // isWorking fails on no charge
                && (isTarget || battery.ChargeMode != Sandbox.ModAPI.Ingame.ChargeMode.Recharge) // source not in recharge
                && (!isTarget || battery.ChargeMode != Sandbox.ModAPI.Ingame.ChargeMode.Discharge)) //target not in discharge
            {
                //Log.Msg($"Name={battery.DisplayNameText} {battery.CurrentStoredPower}");
                energyHolders.Add(new EnergyPower(battery));
            }
        }
    }
}
