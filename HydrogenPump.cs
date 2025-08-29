using Catopia.GasStation.Energy;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;

namespace Catopia.GasStation.Pump
{
    internal class HydrogenPump : EnergyPumpBase
    {
        internal static MyDefinitionId H2DefId = MyDefinitionId.Parse("MyObjectBuilder_GasProperties/Hydrogen");

        public HydrogenPump(IMyCubeGrid cubeGrid) : base(cubeGrid)
        {
        }

        protected override void CheckAndAddHolder(MyCubeBlock fatBlock, bool isTarget, ref EnergyHolders energyHolders)
        {
            IMyGasTank gasTank;
            if ((gasTank = fatBlock as IMyGasTank) != null && gasTank.IsWorking && (isTarget || !gasTank.Stockpile))
            {
                Log.Msg("Naughty");
                var sb = gasTank.SlimBlock.BlockDefinition as MyGasTankDefinition;
                if (sb.StoredGasId == H2DefId)
                    energyHolders.Add(new EnergyGas(gasTank));
            }
        }
    }
}
