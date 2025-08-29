using Catopia.GasStation.Energy;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;

namespace Catopia.GasStation.Pump
{
    internal class OxygenPump : EnergyPumpBase
    {
        internal static MyDefinitionId O2DefId = MyDefinitionId.Parse("MyObjectBuilder_GasProperties/Oxygen");

        public OxygenPump(IMyCubeGrid cubeGrid) : base(cubeGrid)
        {
        }

        protected override void CheckAndAddHolder(MyCubeBlock fatBlock, bool isTarget, ref EnergyHolders energyHolders)
        {
            IMyGasTank gasTank;
            if ((gasTank = fatBlock as IMyGasTank) != null && gasTank.IsWorking && (isTarget || !gasTank.Stockpile))
            {
                var sb = gasTank.SlimBlock.BlockDefinition as MyGasTankDefinition;
                Log.Msg($"Name={gasTank.DisplayNameText} {sb.StoredGasId.ToString()}");
                if (sb.StoredGasId == O2DefId)
                    energyHolders.Add(new EnergyGas(gasTank));
            }
        }
    }
}
