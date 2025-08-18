using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using System;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Catopia.GasStation
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_TextPanel), false, new[] { "GasStationController" })]
    public class ControllerBlock : MyGameLogicComponent
    {
        private IMyTextPanel block;
        internal MyInventory Inventory;
        private MyDefinitionId id = new MyDefinitionId(typeof(MyObjectBuilder_PhysicalObject), "SpaceCredit");
        private GasPump gasPump;
        private bool enableTransfer;
        private IMyShipConnector tradeConnector;
        private MyInventory cashInventory;
        private IMyCubeGrid stationCubeGrid;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            block = Entity as IMyTextPanel;

            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            if (block.CubeGrid?.Physics == null)
                return;

            stationCubeGrid = block.CubeGrid;
            cashInventory = block.GetInventory() as MyInventory;

            gasPump = new GasPump(stationCubeGrid, cashInventory);

            CheckSCVisability();

            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public override void UpdateAfterSimulation100()
        {
            Log.Msg($"Tick {block.CubeGrid.DisplayName}");
            CheckSCVisability();

            if (tradeConnector == null )
            {
                UpdateTradeConnector();
                return;
            }  
            
            if (gasPump.SorurceTanksCount == 0)
            {
                if (!gasPump.TryFindSourceTanks(tradeConnector))
                {
                    WriteText($"No source tanks found with\nidentifier: {gasPump.GasPumpIdentifier}");
                }
                WriteText($"Source Tanks found: {gasPump.SorurceTanksCount}");
                return;
            }

            if(tradeConnector.IsConnected && gasPump.TargetTanksCount == 0)
            {
                if (!gasPump.TryFindTargetTanks(tradeConnector)){
                    WriteText($"No target tanks found on ship");
                }
                WriteText($"Target Tanks found: {gasPump.TargetTanksCount}");

                return;
            }



            //WriteText($"enableTransfer = {enableTransfer}");
        }

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();
            Log.Msg($"OnAddedToScene {block.CubeGrid.DisplayName}");
            block.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
            block.WriteText("Booting ....");

        }

        public void WriteText(string text)
        {
            block.WriteText(text);
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

        private void CheckSCVisability()
        {
            var scAmount = (float)cashInventory.GetItemAmount(id);
            Log.Msg($"{scAmount}");
            try
            {
                MyEntitySubpart subpart;
                if (Entity.TryGetSubpart("SpaceCredit", out subpart)) // subpart does not exist when block is in build stage
                {
                    Log.Msg($"{subpart.Name}");
                    subpart.Render.Visible = scAmount > 0.001;
                }
            }
            catch (Exception e)
            {
                Log.Msg(e.ToString());
            }
        }

        internal void ToggleTransfer()
        {
            enableTransfer = !enableTransfer;
        }

        internal void UpdateTradeConnector()
        {
            tradeConnector = null;

            foreach (var connector in stationCubeGrid.GetFatBlocks<IMyShipConnector>())
            {
                if (!connector.CustomName.Contains(gasPump.GasPumpIdentifier))
                    continue;
                var connectorBlock = connector.GameLogic.GetAs<ConnectorBlock>();
                if (connectorBlock == null)
                {
                    WriteText($"Connector: '{connector.CustomName}'\nGamelogic not found.");
                    break;
                }

                if (!connector.Enabled)
                {
                    WriteText($"Connector: '{connector.CustomName}'\nNot Enabled.");
                    tradeConnector = null;
                    connectorBlock.ControllerBlock = null;
                    return;
                }

                if (connector.GetValue<bool>("Trading"))
                {
                    tradeConnector = connector;
                    connectorBlock.ControllerBlock = this;
                    WriteText($"Connector: '{connector.CustomName}'\nFound.");
                    return;
                }
                WriteText($"Connector: '{connector.CustomName}'\nNot in Trade mode.");
                tradeConnector = null;
                connectorBlock.ControllerBlock = null;
                return;
            }

            WriteText($"No Gas Pump Connector found with\nidentifier: {gasPump.GasPumpIdentifier}");
            return;
        }
    }
}