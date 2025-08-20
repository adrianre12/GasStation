using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

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
        private bool enableTransferButton;
        private IMyShipConnector tradeConnector;
        private MyInventory cashInventory;
        private IMyCubeGrid stationCubeGrid;
        private DockedState dockedState = DockedState.Unknown;
        private string dockedShipName;
        private List<string> screenText = new List<string>();
        private int booting = 2;
        private StringBuilder screenSB = new StringBuilder();

        private const string EMISSIVE_MATERIAL_NAME = "Emissive1";
        private Color BLACK = new Color(0, 0, 0);
        private Color RED = new Color(255, 0, 0);
        private Color GREY = new Color(128, 128, 128);
        private Color MUSTARD = new Color(255, 255, 0);
        private Color GREEN = new Color(10, 255, 0);
        private Color CYAN = new Color(0, 255, 255);

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

            SetEmissives(RED);

            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
            block.EnabledChanged += Block_EnabledChanged;

            block.CubeGrid.OnBlockRemoved += CubeGrid_OnBlockRemoved;
        }

        private void SetEmissives(Color colour)
        {
            block.SetEmissiveParts(EMISSIVE_MATERIAL_NAME, colour, 1f);
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
            enableTransferButton = false;

            // Log.Msg($"Tick {block.CubeGrid.DisplayName} enabled={block.Enabled}");
            CheckSCVisability();
            if (!block.Enabled)
                return;

            if (gasPump.TargetTanksMarkedForClose()) //cant detect grid change ove trade connector
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
            if (newDockedState != dockedState)
            {
                dockedState = newDockedState;
                switch (dockedState)
                {
                    case DockedState.Docked:
                        {

                            if (gasPump.TargetTanksCount == 0)
                            {
                                if (!gasPump.TryFindTargetTanks(tradeConnector, out dockedShipName))
                                {
                                    WriteText($"No target tanks found on ship");
                                }
                                WriteText($"Docked Ship: '{dockedShipName}'");
                                WriteText($"Ship Tanks found: {gasPump.TargetTanksCount}");
                            }

                            break;
                        }
                    case DockedState.UnDocked:
                        {
                            //Log.Msg(">>target tanks reset");
                            dockedShipName = null;
                            WriteText($"No Ship Docked");
                            gasPump.TargetTanksReset();
                            enableTransfer = false;
                            break;
                        }
                }
                return;
            }

            var cashSC = (int)cashInventory.GetItemAmount(id);
            var freeSpaceKL = (int)gasPump.TargetH2Tanks.TotalFree / 1000;
            var maxFillKL = (int)Math.Min(freeSpaceKL, cashSC / gasPump.PricePerKL);
            switch (dockedState)
            {
                case DockedState.Docked:
                    {
                        ScreenDocked(cashSC, freeSpaceKL, maxFillKL);
                        enableTransferButton = maxFillKL > 0;
                        break;
                    }
                case DockedState.UnDocked:
                    {
                        ScreenUndocked(cashSC);
                        break;
                    }
            }

            if (!enableTransferButton)
                enableTransfer = false;

            if (enableTransfer)
            {
                switch (gasPump.BatchTransfer(cashSC))
                {
                    case GasPump.TransferResult.Continue:
                        {
                            break;
                        }
                    default:
                        {
                            enableTransfer = false;
                            break;
                        }
                }
            }

            UpdateEmissives();
            // think abut sleep mode.
        }


        private void Reset()
        {
            tradeConnector = null;
            gasPump = new GasPump(stationCubeGrid, cashInventory);
            enableTransfer = false;
            enableTransferButton = false;
            dockedState = DockedState.Unknown;
            booting = 2;
            screenText.Clear();
        }

        internal void WriteText(string text)
        {
            if (screenText.Count > 11)
                screenText.RemoveAt(0);
            screenText.Add($"{text}\n");
            screenSB.Clear();
            foreach (var line in screenText)
                screenSB.Append(line);
            block.WriteText(screenSB.ToString());
        }

        internal void ScreenDocked(int cashSC, int freeSpaceKL, int maxFillKL)
        {
            screenSB.Clear();
            //               "12345678901234567890123456789012345678901"
            screenSB.Append($"Station: '{block.CubeGrid.DisplayName}'\n");
            screenSB.Append($"H2 Available: {(int)gasPump.SourceH2Tanks.TotalAvailable / 1000}KL\n");
            screenSB.Append($"Price SC/KL: SC {gasPump.PricePerKL} \n");
            screenSB.Append($"SC Inserted: SC {cashSC}\n");
            screenSB.Append($"\n");
            screenSB.Append($"Ship: '{dockedShipName}'\n");
            screenSB.Append($"Free Space: {freeSpaceKL}KL in {gasPump.TargetTanksCount} tanks\n");
            screenSB.Append($"Max Price: SC {freeSpaceKL * gasPump.PricePerKL}\n");
            screenSB.Append($"Max Fill: {maxFillKL}KL\n");
            screenSB.Append($"Total Price: SC {(int)maxFillKL * gasPump.PricePerKL}\n");
            screenSB.Append($"\n");
            if (cashSC == 0)
            {
                screenSB.Append($"Insert Space Credits");
            }
            else
            {
                string tmp = enableTransfer ? "Stop" : "Start";
                screenSB.Append($"Press button to {tmp}");
            }
            block.WriteText(screenSB.ToString());
        }
        internal void ScreenUndocked(int cashSC)
        {
            screenSB.Clear();
            //               "12345678901234567890123456789012345678901"
            screenSB.Append($"Station: '{block.CubeGrid.DisplayName}'\n");
            screenSB.Append($"H2 Available: {(int)gasPump.SourceH2Tanks.TotalAvailable / 1000}KL\n");
            screenSB.Append($"Price SC/KL: SC {gasPump.PricePerKL} \n");
            screenSB.Append($"SC Inserted: SC {cashSC}\n");
            screenSB.Append($"\n");
            screenSB.Append($"Dock ship and insert SC to continue .\n");
            screenSB.Append($"\n");
            screenSB.Append($"\n");
            screenSB.Append($"\n");
            screenSB.Append($"\n");
            screenSB.Append($"\n");
            screenSB.Append($"\n");
            //screenSB.Append($"12345678901234567890123456789012345678901");

            block.WriteText(screenSB.ToString());
        }

        /*        internal void ScreenX()
                {
                    screenSB.Clear();
                    //               "12345678901234567890123456789012345678901"
                    screenSB.Append($"1\n");
                    screenSB.Append($"2\n");
                    screenSB.Append($"3\n");
                    screenSB.Append($"4\n");
                    screenSB.Append($"5\n");
                    screenSB.Append($"6\n");
                    screenSB.Append($"7\n");
                    screenSB.Append($"8\n");
                    screenSB.Append($"9\n");
                    screenSB.Append($"10\n");
                    screenSB.Append($"11\n");
                    screenSB.Append($"12345678901234567890123456789012345678901");

                    block.WriteText(screenSB.ToString());
                }*/

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
            enableTransfer = enableTransferButton && !enableTransfer;
            UpdateEmissives();
        }

        internal void UpdateEmissives()
        {
            if (!enableTransferButton)
            {
                enableTransfer = false;
                SetEmissives(RED);
                return;
            }

            if (enableTransfer)
            {
                SetEmissives(GREEN);
                return;
            }

            SetEmissives(MUSTARD);
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

        private bool CheckTradeConnector()
        {
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