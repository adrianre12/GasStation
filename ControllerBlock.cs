using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
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
        private DockedState dockedState = DockedState.Unknown;
        private List<string> screenText = new List<string>();
        private int booting = 2;
        private StringBuilder screenStringBuilder = new StringBuilder();

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

            block.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
            block.WriteText(".");
            block.Font = "Debug";
            block.FontSize = 0.85f;

            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
            block.EnabledChanged += Block_EnabledChanged;

            block.CubeGrid.OnBlockRemoved += CubeGrid_OnBlockRemoved;
        }

        private void CubeGrid_OnBlockRemoved(IMySlimBlock obj)
        {
            //WriteText("Checking Removed Blocks");
            if (tradeConnector != null && tradeConnector.MarkedForClose)
            {
                Reset();
                WriteText("Trade Connector Removed");
                return;
            }
            if (gasPump != null && gasPump.SourceTanksMarkedForClose())
            {
                Reset();
                WriteText("Gas Tank Removed");
            }
        }

        private void Block_EnabledChanged(IMyTerminalBlock obj)
        {
            Log.Msg($"EnabledChanged Enabled = {block.Enabled}");
            if (!block.Enabled)
                Reset();
        }

        public override void UpdateAfterSimulation100()
        {
           // Log.Msg($"Tick {block.CubeGrid.DisplayName} enabled={block.Enabled}");
            CheckSCVisability();
            if (!block.Enabled)
                return;

            if(gasPump.TargetTanksMarkedForClose()) //cant detect grid change ove trade connector
            {
                Reset();
                WriteText("Ship Tank Removed");
                return;
            }

            if (booting > 0)
            {
                WriteText("Booting....");
                booting--;
                return;
            }

            if (tradeConnector == null || tradeConnector.MarkedForClose)
            {
                FindTradeConnector();
                dockedState = DockedState.Unknown; //force missmatch
                enableTransfer = false;
                return;
            } 
            
            if (!CheckTradeConnector())
            {
                dockedState = DockedState.Unknown;
                enableTransfer = false;
                return;
            }
            
            if (gasPump.SorurceTanksCount == 0)
            {
                if (!gasPump.TryFindSourceTanks(tradeConnector))
                {
                    WriteText($"No source tanks found with identifier: {gasPump.GasPumpIdentifier}");
                    enableTransfer = false;
                    return;
                }
                WriteText($"Source Tanks found: {gasPump.SorurceTanksCount}");
                return;
            }

            var newDockedState = DockedStateFromBool(tradeConnector.IsConnected);
            if ( newDockedState != dockedState)
            {
                dockedState = newDockedState;
                switch (dockedState)
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
                            //Log.Msg(">>target tanks reset");
                            WriteText($"No Ship Docked");
                            gasPump.TargetTanksReset();
                            enableTransfer = false;
                            break;
                        }
                }
                return;
            }

            

            //WriteText("01234567890123456789012345678901234567890123456789012345678901234567890123456789");
            //WriteText($"enableTransfer = {enableTransfer}");
        }


        private void Reset()
        {
            tradeConnector = null;
            gasPump = new GasPump(stationCubeGrid, cashInventory);
            enableTransfer = false;
            dockedState = DockedState.Unknown;
            booting = 2;
            screenText.Clear();
        }

        internal void WriteText(string text)
        {
            if (screenText.Count > 11)
                screenText.RemoveAt(0);
            screenText.Add($"{text}\n");
            screenStringBuilder.Clear();
            foreach( var line in screenText )
                screenStringBuilder.Append(line);
            block.WriteText(screenStringBuilder.ToString());
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

        private void FindTradeConnector()
        {
            tradeConnector = null;

            foreach (var connector in stationCubeGrid.GetFatBlocks<IMyShipConnector>())
            {
                if (!connector.CustomName.Contains(gasPump.GasPumpIdentifier))
                    continue;
                tradeConnector = connector;
                WriteText($"Connector: '{tradeConnector.CustomName}'");

                return;
            }

            WriteText($"No Gas Pump Connector found with\nidentifier: {gasPump.GasPumpIdentifier}");
            return;
        }

        private bool CheckTradeConnector() {
            if (!tradeConnector.IsWorking)
                {
                    WriteText($"Connector: '{tradeConnector.CustomName}' Not Enabled.");
                    return false;
                }

                if (!tradeConnector.GetValue<bool>("Trading"))
                {
                    WriteText($"Connector: '{tradeConnector.CustomName}' Not in Trade mode.");
                    return false;
                }

                //WriteText($"Connector: '{tradeConnector.CustomName}'");
            return true; ;

        }
    }
}