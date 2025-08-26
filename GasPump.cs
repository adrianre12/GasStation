using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.ModAPI;

namespace Catopia.GasStation
{
    internal class GasPump
    {
        private GasTanks targetH2Tanks = new GasTanks();
        internal GasTanks TargetH2Tanks { get { return targetH2Tanks; } }
        private GasTanks sourceH2Tanks = new GasTanks();
        internal GasTanks SourceH2Tanks { get { return sourceH2Tanks; } }

        private static MyDefinitionId H2DefId = MyDefinitionId.Parse("MyObjectBuilder_GasProperties/Hydrogen");

        internal int SorurceTanksCount { get { return sourceH2Tanks.Count; } }
        internal int TargetTanksCount { get { return targetH2Tanks.Count; } }

        private IMyCubeGrid stationCubeGrid;

        public enum TransferResult
        {
            Continue,
            EmptySource,
            FullTarget,
            NotEnoughCash,
            Error
        }

        public GasPump(IMyCubeGrid cubeGrid)
        {
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
            return targetH2Tanks.TanksMarkedForClose();
        }

        public TransferResult BatchTransfer(int transferRequestKL, out int transferedKL)
        {
            transferedKL = 0;
            TransferResult transferResult = TransferGas(transferRequestKL, out transferedKL);
            //Log.Msg($"TransferGas transferRequestKL={transferRequestKL} transferResult ={transferResult.ToString()} transferedKL={transferedKL}");

            switch (transferResult)
            {
                /*                case TransferResult.Continue:
                                    {
                                        break;
                                    }
                                case TransferResult.FullTarget:
                                    {
                                        //Log.Debug("Target Full");
                                        break;
                                    }
                                case TransferResult.EmptySource:
                                    {
                                        //Log.Debug("Source Empty");
                                        break;
                                    }
                                case TransferResult.NotEnoughCash:
                                    {
                                        //.Msg("Not Enough Cash");
                                        break;
                                    }*/
                case TransferResult.Error:
                    {
                        Log.Msg("Error transfer stopped");
                        break;
                    }
                default:
                    break;
            }
            return transferResult;
        }

        private TransferResult TransferGas(long transferRequestKL, out int transferedKL)
        {
            transferedKL = 0;
            //Log.Msg($"transferRequestKL={transferRequestKL}");
            //check source gas available 
            long transferL = (long)Math.Round(Math.Min(transferRequestKL * 1000, sourceH2Tanks.TotalAvailable));
            if (transferL == 0)
                return TransferResult.EmptySource;
            //Log.Msg($"transferL={transferL} after source tank check");
            //check target tank space available
            transferL = (long)Math.Round(Math.Min(transferL, targetH2Tanks.TotalFree));
            //Log.Msg($"transferL={transferL} after target tank check");

            if (transferL == 0)
                return TransferResult.FullTarget;


            // add gas to target
            long amountFilled = targetH2Tanks.Fill(transferL);

            //remove gas from source
            double amountDrained = sourceH2Tanks.Drain(amountFilled);
            //Log.Msg($"transferL={transferL} amountFilled={amountFilled} amountDrained=={amountDrained}");

            transferedKL = (int)amountFilled / 1000;
            if (Math.Abs(amountDrained - amountFilled) > 100) //acount for rounding errors
            {
                Log.Msg($"Amount drained != filled [ {amountDrained} != {amountFilled} ]");
                return TransferResult.Error;
            }

            return TransferResult.Continue;
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
                //Log.Msg("Trade connector is not connected.");
                return false;
            }

            List<MyCubeGrid> groupNodes = connectedGrid.GetConnectedGrids(GridLinkTypeEnum.Mechanical);
            if (groupNodes == null || groupNodes.Count == 0)
            {
                //Log.Msg("groupNodes is null or empty");
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

        internal bool TryFindSourceTanks(IMyShipConnector tradeConnector, string gasPumpIdentifier)
        {
            sourceH2Tanks.Clear();
            if (stationCubeGrid == null)
                return false;

            foreach (var gasTank in stationCubeGrid.GetFatBlocks<Sandbox.ModAPI.IMyGasTank>())
            {
                if (!tradeConnector.GetInventory().IsConnectedTo(gasTank.GetInventory()))
                    continue;
                //Log.Msg($">>> grid displayNameText={gasTank.DisplayNameText} customName={gasTank.CustomName}");
                if (gasTank.CustomName.Contains(gasPumpIdentifier) && gasTank.IsWorking && !gasTank.Stockpile)
                {
                    var sb = gasTank.SlimBlock.BlockDefinition as MyGasTankDefinition;
                    if (sb.StoredGasId == H2DefId)
                        sourceH2Tanks.Add(gasTank);
                }
            }
            //Log.Msg($"sourceTanks found = {sourceH2Tanks.Count}");
            return sourceH2Tanks.Count > 0;
        }

    }
}
