using Catopia.GasStation.Energy;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI;

namespace Catopia.GasStation.Pump
{
    internal abstract class EnergyPumpBase //: IEnergyPumpBase
    {
        internal EnergyHolders sourceEnergy = new EnergyHolders();
        public EnergyHolders SourceEnergy { get { return sourceEnergy; } }
        internal EnergyHolders targetEnergy = new EnergyHolders();
        public EnergyHolders TargetEnergy { get { return targetEnergy; } }
        public int SorurceHoldersCount { get { return sourceEnergy.Count; } }
        public int TargetHoldersCount { get { return targetEnergy.Count; } }

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

        public void Clear()
        {
            sourceEnergy.Clear();
            targetEnergy.Clear();
        }

        public void TargetHoldersReset()
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

        public TransferResult BatchTransfer(int transferRequestK, out int transferedK)
        {
            transferedK = 0;
            TransferResult transferResult = TransferEnergy(transferRequestK, out transferedK);
            //Log.Msg($"TransferGas transferRequestK={transferRequestK} transferResult ={transferResult.ToString()} transferedKL={transferedKL}");

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

        private TransferResult TransferEnergy(long transferRequestK, out int transferedK)
        {
            transferedK = 0;
            //Log.Msg($"transferRequestK={transferRequestK}");
            //check source available 
            long transferL = (long)Math.Round(Math.Min(transferRequestK * 1000, sourceEnergy.TotalAvailable));
            if (transferL == 0)
                return TransferResult.EmptySource;

            //check target tank space available
            transferL = (long)Math.Round(Math.Min(transferL, targetEnergy.TotalFree));

            if (transferL == 0)
                return TransferResult.FullTarget;

            // add to target
            long amountFilled = targetEnergy.Fill(transferL);

            //remove from source
            double amountDrained = sourceEnergy.Drain(amountFilled);
            //Log.Msg($"transferL={transferL} amountFilled={amountFilled} amountDrained=={amountDrained}");

            transferedK = (int)amountFilled / 1000;
            if (Math.Abs(amountDrained - amountFilled) > 100) //acount for rounding errors
            {
                Log.Msg($"Amount drained != filled [ {amountDrained} != {amountFilled} ]");
                return TransferResult.Error;
            }

            return TransferResult.Continue;
        }

        public bool TryFindTargets(IMyShipConnector tradeConnector, out string shipName)
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
                    CheckAndAddHolder(fatBlock, true, targetConector.GetInventory().IsConnectedTo(fatBlock.GetInventory()), ref targetEnergy);
                }

            }
            //Log.Msg($"targetTanks found = {targetH2Tanks.Count}");
            shipName = connectedGrid.DisplayName;
            return targetEnergy.Count > 0;
        }

        protected abstract void CheckAndAddHolder(MyCubeBlock fatBlock, bool isTarget, bool isConnected, ref EnergyHolders energyHolders);

        public bool TryFindSources(IMyShipConnector tradeConnector, string gasPumpIdentifier)
        {
            sourceEnergy.Clear();
            if (stationCubeGrid == null)
                return false;

            foreach (var fatBlock in stationCubeGrid.GetFatBlocks())
            {
                //Log.Msg($">>> fatblock displayNameText={fatBlock.DisplayNameText}");
                if (!fatBlock.DisplayNameText.Contains(gasPumpIdentifier))
                    continue;

                CheckAndAddHolder(fatBlock, false, tradeConnector.GetInventory().IsConnectedTo(fatBlock.GetInventory()), ref sourceEnergy);
            }
            //Log.Msg($"sourceHolders found = {sourceEnergy.Count}");
            return sourceEnergy.Count > 0;
        }

    }
}
