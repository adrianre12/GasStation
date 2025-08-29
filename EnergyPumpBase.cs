using Catopia.GasStation.Energy;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI;

namespace Catopia.GasStation.Pump
{
    internal abstract class EnergyPumpBase
    {
        internal EnergyHolders targetEnergy = new EnergyHolders();
        public EnergyHolders TargetEnergy { get { return targetEnergy; } }
        internal EnergyHolders sourceEnergy = new EnergyHolders();
        public EnergyHolders SourceEnergy { get { return sourceEnergy; } }

        internal int SorurceHoldersCount { get { return sourceEnergy.Count; } }
        internal int TargetHoldersCount { get { return targetEnergy.Count; } }

        private MyCubeGrid stationCubeGrid;

        public enum TransferResult
        {
            Continue,
            EmptySource,
            FullTarget,
            NotEnoughCash,
            Error
        }

        public EnergyPumpBase(IMyCubeGrid cubeGrid)
        {
            stationCubeGrid = cubeGrid as MyCubeGrid;
        }

        public void TargetHolderssReset()
        {
            targetEnergy.Clear();
        }

        public bool SourceHoldersMarkedForClose()
        {
            return sourceEnergy.HoldersMarkedForClose();
        }

        public bool TargetHolderssMarkedForClose()
        {
            return targetEnergy.HoldersMarkedForClose();
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
            long transferL = (long)Math.Round(Math.Min(transferRequestKL * 1000, sourceEnergy.TotalAvailable));
            if (transferL == 0)
                return TransferResult.EmptySource;
            //Log.Msg($"transferL={transferL} after source tank check");
            //check target tank space available
            transferL = (long)Math.Round(Math.Min(transferL, targetEnergy.TotalFree));
            //Log.Msg($"transferL={transferL} after target tank check");

            if (transferL == 0)
                return TransferResult.FullTarget;


            // add gas to target
            long amountFilled = targetEnergy.Fill(transferL);

            //remove gas from source
            double amountDrained = sourceEnergy.Drain(amountFilled);
            //Log.Msg($"transferL={transferL} amountFilled={amountFilled} amountDrained=={amountDrained}");

            transferedKL = (int)amountFilled / 1000;
            if (Math.Abs(amountDrained - amountFilled) > 100) //acount for rounding errors
            {
                Log.Msg($"Amount drained != filled [ {amountDrained} != {amountFilled} ]");
                return TransferResult.Error;
            }

            return TransferResult.Continue;
        }

        internal bool TryFindTargets(IMyShipConnector tradeConnector, out string shipName)
        {
            shipName = null; ;
            targetEnergy.Clear();
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
                    CheckAndAddHolder(fatBlock, true, ref targetEnergy);
                }

            }
            //Log.Msg($"targetTanks found = {targetH2Tanks.Count}");
            shipName = connectedGrid.DisplayName;
            return targetEnergy.Count > 0;
        }

        protected abstract void CheckAndAddHolder(MyCubeBlock fatBlock, bool isTarget, ref EnergyHolders energyHolders);

        internal bool TryFindSources(IMyShipConnector tradeConnector, string gasPumpIdentifier)
        {
            sourceEnergy.Clear();
            if (stationCubeGrid == null)
                return false;

            foreach (var fatBlock in stationCubeGrid.GetFatBlocks())
            {
                //Log.Msg($">>> fatblock displayNameText={fatBlock.DisplayNameText}");
                if (fatBlock.DisplayNameText.Contains(gasPumpIdentifier) && !tradeConnector.GetInventory().IsConnectedTo(fatBlock.GetInventory()))
                    continue;

                CheckAndAddHolder(fatBlock, false, ref sourceEnergy);
            }
            //Log.Msg($"sourceHolders found = {sourceEnergy.Count}");
            return sourceEnergy.Count > 0;
        }

    }
}
