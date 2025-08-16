using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;


namespace Catopia.GasStation
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_ShipConnector), false, new[] { "GasPump" })]
    internal class GasPumpBlock : MyGameLogicComponent
    {
        private const int KLPerTick = 80;

        private IMyShipConnector block;
        private IMyCubeGrid cubeGrid;
        private GasTanks targetH2Tanks = new GasTanks();
        private GasTanks sourceH2Tanks = new GasTanks();
        private bool tradingMode;
        private int pricePerKL = 1;
        private string gasPumpIdentifier = "GasPump1";

        private static MyDefinitionId SCDefId = MyDefinitionId.Parse("MyObjectBuilder_PhysicalObject/SpaceCredit");
        private static MyDefinitionId H2DefId = MyDefinitionId.Parse("MyObjectBuilder_GasProperties/Hydrogen");
        private bool enableTransfer;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            block = Entity as IMyShipConnector;

            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
            cubeGrid = block.CubeGrid;
            block.IsConnectedChanged += Block_IsConnectedChanged;
            block.EnabledChanged += Block_EnabledChanged;
        }

        private void Block_EnabledChanged(IMyTerminalBlock obj)
        {
            Log.Msg($"EnabledChanged Enabled = {block.Enabled}");
            if (!block.Enabled)
            {
                targetH2Tanks.Clear();
                return;
            }

            FindSourceTanks();
        }

        private void Block_IsConnectedChanged(IMyShipConnector obj)
        {
            Log.Msg($"IsConenctedChanged IsConnected = {block.IsConnected}");
            if (!block.IsConnected) {
                targetH2Tanks.Clear();
                enableTransfer = false;
                return;
            }
            tradingMode = block.GetValue<bool>("Trading");
            if (!tradingMode)
                return;

            FindSourceTanks();
            FindTargetTanks();
            enableTransfer = true;
        }

        public override void UpdateAfterSimulation100()
        {
            //Log.Msg($"Tick {block.CubeGrid.DisplayName} block.IsWorking={block.IsWorking} tradingMode={tradingMode}");
            if (!block.IsConnected || !block.IsWorking || !tradingMode || !enableTransfer || targetH2Tanks.Count == 0)
                return;

            int cashSC;
            if ((cashSC = FindCashAmount()) == 0)
            {
                Log.Msg("No money, disable transfer");
                enableTransfer = false;
                return;
            }

            Log.Msg($"Found SC={cashSC}");



            switch (StartGasTransfer(cashSC))
            {
                case TransferResult.Continue:
                    {
                        break;
                    }
                case TransferResult.FullTarget:
                    {
                        enableTransfer = false;
                        Log.Debug("Target Full");
                        break;
                    }
                case TransferResult.EmptySource:
                    {
                        enableTransfer= false;
                        Log.Debug("Source Empty");
                        break;
                    }
                case TransferResult.NotEnoughCash:
                    {
                        enableTransfer = false;
                        Log.Msg("Not Enough Cash");
                        break;
                    }
                case TransferResult.Error:
                    {
                        enableTransfer = false;
                        Log.Msg("Error transfer stopped");
                        break;
                    }
            }

        }

        enum TransferResult { 
            Continue,
            EmptySource,
            FullTarget,
            NotEnoughCash,
            Error
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
                return TransferResult.FullTarget ;
            
            // add gas to target
            double amountFilled = targetH2Tanks.Fill(transferL);

            //remove gas from source
            double amountDrained = sourceH2Tanks.Drain(amountFilled);
            if (amountDrained != amountFilled)
            {
                Log.Msg($"amountDrained != amountFilled {amountDrained} != {amountFilled}");
            }
            
            Log.Debug($"Transfered {amountFilled}");

            transferedKL = (int)amountFilled/1000;
            return TransferResult.Continue;
        }

        private bool TryRemoveCash(int amount)
        {
            if (amount == 0)
                return true;

            MyInventory otherInventory = (MyInventory)block.OtherConnector.GetInventory();
            if (otherInventory == null || otherInventory.ItemCount == 0)
                return false;
            return otherInventory.RemoveItemsOfType(amount, SCDefId).ToIntSafe() == amount;
        }

        private int FindCashAmount()
        {
            IMyInventory otherInventory = block.OtherConnector.GetInventory();
            if (otherInventory == null || otherInventory.ItemCount == 0)
                return 0;
            return otherInventory.GetItemAmount(SCDefId).ToIntSafe();
        }

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();
            Log.Msg($"OnAddedToScene {block.CubeGrid.DisplayName}");
        }

        public override void OnRemovedFromScene()
        {
            Log.Msg($"OnRemovedFromScene {block.CubeGrid.DisplayName}");

        }

        public override void MarkForClose()
        {
            base.MarkForClose();
            Log.Msg($"MarkForClose {block.CubeGrid.DisplayName}");

        }

        private void FindTargetTanks()
        {
            targetH2Tanks.Clear();
            IMyShipConnector targetConector = block.OtherConnector;
            MyCubeGrid connectedGrid = (MyCubeGrid)(targetConector?.CubeGrid);
            if (!block.IsConnected || connectedGrid == null)
            {
                Log.Msg("Trade connector is not connected.");
                return;
            }

            List<MyCubeGrid> groupNodes = connectedGrid.GetConnectedGrids(GridLinkTypeEnum.Mechanical);
            if (groupNodes == null || groupNodes.Count == 0)
            {
                Log.Msg("groupNodes is null or empty");
                return;
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
            Log.Msg($"targetTanks found = {targetH2Tanks.Count}");
        }

        private void FindSourceTanks()
        {
            sourceH2Tanks.Clear();

            foreach (var gasTank in block.CubeGrid.GetFatBlocks<Sandbox.ModAPI.IMyGasTank>())
            {
                if (!block.GetInventory().IsConnectedTo(gasTank.GetInventory()))
                    continue;
                Log.Msg($">>> grid displayNameText={gasTank.DisplayNameText} customName={gasTank.CustomName}");
                if (gasTank.CustomName.Contains(gasPumpIdentifier) && gasTank.IsWorking && !gasTank.Stockpile)
                {
                    var sb = gasTank.SlimBlock.BlockDefinition as MyGasTankDefinition;
                    if (sb.StoredGasId == H2DefId)
                        sourceH2Tanks.Add(gasTank);
                }
            }
            Log.Msg($"sourceTanks found = {sourceH2Tanks.Count}");
        }
    }
}