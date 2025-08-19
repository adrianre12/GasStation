using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Text;
using VRage;
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
        private DockedState shipConnected = DockedState.Unknown;

        private enum DockedState
        {
            Unknown,
            UnDocked,
            Docked
        }

        private DockedState DockedStateFromBool(bool value)
        {
            if (value)
                return DockedState.Docked;
            return DockedState.UnDocked;
        }

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

            block.Font = "Debug";
            block.FontSize = 1f;

            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
            block.EnabledChanged += Block_EnabledChanged;
        }

        private void Block_EnabledChanged(IMyTerminalBlock obj)
        {
            Log.Msg($"EnabledChanged Enabled = {block.Enabled}");
                        if (!block.Enabled)
                        {
                            Reset();
                            return;
                        }
        }

        public override void UpdateAfterSimulation100()
        {

            Log.Msg($"Tick {block.CubeGrid.DisplayName} enabled={block.Enabled}");
            CheckSCVisability();
            if (!block.Enabled)
                return;

            if (tradeConnector == null || !tradeConnector.IsWorking )
            {
                Log.Msg(">>trade");
                UpdateTradeConnector();
                shipConnected = DockedState.Unknown; //force missmatch
                return;
            }  
            
            if (gasPump.SorurceTanksCount == 0)
            {
                Log.Msg(">>sourcetanks=0");

                if (!gasPump.TryFindSourceTanks(tradeConnector))
                {
                    WriteText($"No source tanks found with\nidentifier: {gasPump.GasPumpIdentifier}");
                }
                WriteText($"Source Tanks found: {gasPump.SorurceTanksCount}");
                return;
            }

            var dockedState = DockedStateFromBool(tradeConnector.IsConnected);
            if ( dockedState != shipConnected)
            {
                Log.Msg("isconnected changed");

                shipConnected = dockedState;
                switch (shipConnected)
                {
                    case DockedState.Docked:
                        {
                            if (gasPump.TargetTanksCount == 0)
                            {
                                if (!gasPump.TryFindTargetTanks(tradeConnector))
                                {
                                    WriteText($"No target tanks found on ship");
                                }
                                WriteText($"Target Tanks found: {gasPump.TargetTanksCount}");
                            }

                            break;
                        }
                    case DockedState.UnDocked:
                        {
                            Log.Msg(">>target tanks reset");
                            WriteText($"No Ship Docked");
                            gasPump.TargetTanksReset();
                            break;
                        }
                }
                return;
            }

            

            /*            var sb = new StringBuilder();
                        sb.Append("01234567890123456789012345678901234567890123456789012345678901234567890123456789\n");
                        for (int i = 1; i < 10; i++)
                        {
                            sb.Append($"{i.ToString()}\n");
                        }
                        WriteText(sb.ToString());*/
            //WriteText($"enableTransfer = {enableTransfer}");
        }

        private void Reset()
        {
            tradeConnector = null;
            gasPump = new GasPump(stationCubeGrid, cashInventory);
            enableTransfer = false;
            shipConnected = DockedState.Unknown;
            block.WriteText("Booting ....");
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
            try
            {
                MyEntitySubpart subpart;
                if (Entity.TryGetSubpart("SpaceCredit", out subpart)) // subpart does not exist when block is in build stage
                {
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