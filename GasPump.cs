using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.ModAPI;

namespace Catopia.GasStation
{
    internal class GasPump
    {
        private const int KLPerTick = 80;

        private GasTanks targetH2Tanks = new GasTanks();
        private GasTanks sourceH2Tanks = new GasTanks();
        private int pricePerKL = 1;
        internal string GasPumpIdentifier = "[GS1]";

        private static MyDefinitionId SCDefId = MyDefinitionId.Parse("MyObjectBuilder_PhysicalObject/SpaceCredit");
        private static MyDefinitionId H2DefId = MyDefinitionId.Parse("MyObjectBuilder_GasProperties/Hydrogen");

        internal int SorurceTanksCount { get { return sourceH2Tanks.Count;  } }
        internal int TargetTanksCount { get { return targetH2Tanks.Count; } }

        private MyInventory cashInventory;
        private IMyCubeGrid stationCubeGrid;

        public enum TransferResult
        {
            Continue,
            EmptySource,
            FullTarget,
            NotEnoughCash,
            Error
        }

        public GasPump( IMyCubeGrid cubeGrid, MyInventory inventory)
        {
            cashInventory = inventory;
            stationCubeGrid = cubeGrid;
        }

        public void TargetTanksReset()
        {
            targetH2Tanks.Clear();
        }

        public bool SourceTanksMarkedForClose()
        {
            return sourceH2Tanks.TanksMarkedForClose();
        }

        public bool TargetTanksMarkedForClose()
        {
            return  targetH2Tanks.TanksMarkedForClose();
        }

        public TransferResult BatchTransfer()
        {
            int cashSC;
            if ((cashSC = FindCashAmount()) == 0)
            {
                Log.Msg("No money, disable transfer");
                //enableTransfer = false;
                return TransferResult.NotEnoughCash;
            }

            Log.Msg($"Found SC={cashSC}");

            TransferResult transferResult = StartGasTransfer(cashSC);
            switch (transferResult)
            {
                case TransferResult.Continue:
                    {
                        break;
                    }
                case TransferResult.FullTarget:
                    {
                        //enableTransfer = false;
                        Log.Debug("Target Full");
                        break;
                    }
                case TransferResult.EmptySource:
                    {
                        //enableTransfer = false;
                        Log.Debug("Source Empty");
                        break;
                    }
                case TransferResult.NotEnoughCash:
                    {
                        //enableTransfer = false;
                        Log.Msg("Not Enough Cash");
                        break;
                    }
                case TransferResult.Error:
                    {
                        //enableTransfer = false;
                        Log.Msg("Error transfer stopped");
                        break;
                    }
            }
            return transferResult;
        }

        private TransferResult StartGasTransfer(int cashSC)
        {
            int canAffordKL = cashSC / pricePerKL;

            if (canAffordKL == 0)
                return TransferResult.NotEnoughCash;

            int transferRequestKL = Math.Min(canAffordKL, KLPerTick);
            Log.Msg($"Cash={cashSC} canAffordKL{canAffordKL} transferRequestKL={transferRequestKL}");

            Log.Msg("transfer gas");

            int transferedKL = 0;
            TransferResult transferResult = TransferGas(transferRequestKL, out transferedKL);
            Log.Msg($"TransferGas transferResult={transferResult.ToString()}");

            int removeCash = transferedKL * pricePerKL;

            if (!TryRemoveCash(removeCash))
            {
                Log.Msg($"Failed to remove SC = {removeCash}");
                return TransferResult.Error;
            }

            return transferResult;
        }

        private TransferResult TransferGas(int transferRequestKL, out int transferedKL)
        {
            double transferL = transferRequestKL * 1000;
            transferedKL = 0;

            //check source gas available 
            transferL = Math.Min(transferL, sourceH2Tanks.TotalAvailable);
            if (transferL == 0)
                return TransferResult.EmptySource;

            //check target tank space available
            transferL = Math.Min(transferL, targetH2Tanks.TotalFree);

            if (transferL == 0)
                return TransferResult.FullTarget;

            // add gas to target
            double amountFilled = targetH2Tanks.Fill(transferL);

            //remove gas from source
            double amountDrained = sourceH2Tanks.Drain(amountFilled);
            if (amountDrained != amountFilled)
            {
                Log.Msg($"amountDrained != amountFilled {amountDrained} != {amountFilled}");
            }

            Log.Debug($"Transfered {amountFilled}");

            transferedKL = (int)amountFilled / 1000;
            return TransferResult.Continue;
        }

        private bool TryRemoveCash(int amount)
        {
            if (amount == 0)
                return true;

            if (cashInventory.ItemCount == 0)
                return false;
            return cashInventory.RemoveItemsOfType(amount, SCDefId).ToIntSafe() == amount;
        }

        private int FindCashAmount()
        {
            if (cashInventory.ItemCount == 0)
                return 0;
            return cashInventory.GetItemAmount(SCDefId).ToIntSafe();
        }

        internal bool TryFindTargetTanks(IMyShipConnector tradeConnector, out string shipName)
        {
            shipName = null; ;
            targetH2Tanks.Clear();
            if (tradeConnector == null || !tradeConnector.IsConnected)
                return false;

            IMyShipConnector targetConector = tradeConnector.OtherConnector;
            MyCubeGrid connectedGrid = (MyCubeGrid)(targetConector?.CubeGrid);
            if (!tradeConnector.IsConnected || connectedGrid == null)
            {
                Log.Msg("Trade connector is not connected.");
                return false;
            }

            List<MyCubeGrid> groupNodes = connectedGrid.GetConnectedGrids(GridLinkTypeEnum.Mechanical);
            if (groupNodes == null || groupNodes.Count == 0)
            {
                Log.Msg("groupNodes is null or empty");
                return false;
            }

            foreach (MyCubeGrid myCubeGrid in groupNodes)
            {
                foreach (var fatBlock in myCubeGrid.GetFatBlocks())
                {
                    if (!targetConector.GetInventory().IsConnectedTo(fatBlock.GetInventory()))
                        continue;
                    IMyGasTank gasTank;
                    if ((gasTank = fatBlock as IMyGasTank) != null && gasTank.IsWorking)
                    {
                        var sb = gasTank.SlimBlock.BlockDefinition as MyGasTankDefinition;
                        if (sb.StoredGasId == H2DefId)
                            targetH2Tanks.Add(gasTank);
                    }

                }

            }
            //Log.Msg($"targetTanks found = {targetH2Tanks.Count}");
            shipName = connectedGrid.DisplayName;
            return targetH2Tanks.Count > 0;
        }

        internal bool TryFindSourceTanks(IMyShipConnector tradeConnector)
        {
            sourceH2Tanks.Clear();
            if (stationCubeGrid == null)
                return false;

            foreach (var gasTank in stationCubeGrid.GetFatBlocks<Sandbox.ModAPI.IMyGasTank>())
            {
                if (!tradeConnector.GetInventory().IsConnectedTo(gasTank.GetInventory()))
                    continue;
                Log.Msg($">>> grid displayNameText={gasTank.DisplayNameText} customName={gasTank.CustomName}");
                if (gasTank.CustomName.Contains(GasPumpIdentifier) && gasTank.IsWorking && !gasTank.Stockpile)
                {
                    var sb = gasTank.SlimBlock.BlockDefinition as MyGasTankDefinition;
                    if (sb.StoredGasId == H2DefId)
                        sourceH2Tanks.Add(gasTank);
                }
            }
            Log.Msg($"sourceTanks found = {sourceH2Tanks.Count}");
            return sourceH2Tanks.Count > 0;
        }

    }
}
