using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.ObjectBuilders.Private;
using VRageMath;

namespace Catopia.GasStation
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_TextPanel), false, new[] { "GasStationController" })]
    public class ControllerBlock : MyGameLogicComponent
    {
        private const string BUTTON_EMISSIVE_NAME = "Emissive1";
        private const int DEFAULT_BOOT_STEPS = 2;

        private IMyTextPanel block;
        private GasPump gasPump;
        private bool enableTransfer;
        private bool enableTransferButton;
        private IMyShipConnector tradeConnector;
        private MyInventory tradeConnectorInventory;
        private MyInventory cashSourceInventory;
        private IMyCubeGrid stationCubeGrid;
        private DockedStateEnum dockedState = DockedStateEnum.Unknown;
        private string dockedShipName;
        private int bootSteps = DEFAULT_BOOT_STEPS;

        private List<string> screenText = new List<string>();
        private StringBuilder screenSB = new StringBuilder();

        private Color BLACK = new Color(0, 0, 0);
        private Color RED = new Color(255, 0, 0);
        private Color GREY = new Color(128, 128, 128);
        private Color MUSTARD = new Color(255, 255, 0);
        private Color GREEN = new Color(10, 255, 0);
        private Color CYAN = new Color(0, 255, 255);

        private MyDefinitionId SCDefId = new MyDefinitionId(typeof(MyObjectBuilder_PhysicalObject), "SpaceCredit");
        private MyObjectBuilder_PhysicalObject SCobjectBuilder;

        internal Config Settings = new Config();
        private string prevCDRef;

        private enum DockedStateEnum
        {
            Unknown,
            UnDocked,
            Docked
        }

        public enum CreditMethodEnum
        {
            TradeConnector,
            OwnerAccount
        }

        private DockedStateEnum DockedStateFromBool(bool value)
        {
            if (value)
                return DockedStateEnum.Docked;
            return DockedStateEnum.UnDocked;
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            block = Entity as IMyTextPanel;

            if (block.Storage == null)
                block.Storage = new MyModStorageComponent();

            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            Log.DebugLog = true;
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            if (block.CubeGrid?.Physics == null)
                return;

            stationCubeGrid = block.CubeGrid;
            cashSourceInventory = block.GetInventory() as MyInventory;
            gasPump = new GasPump(stationCubeGrid);


            CheckSCVisability();

            block.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
            block.WriteText(".");
            block.Font = "Debug";
            block.FontSize = 0.85f;

            SetEmissives(RED);

            Settings.LoadConfigFromCD(block);

            SCobjectBuilder = (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(SCDefId);


            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
            block.EnabledChanged += Block_EnabledChanged;
            block.IsWorkingChanged += Block_IsWorkingChanged;
            block.CubeGrid.OnBlockRemoved += CubeGrid_OnBlockRemoved;
        }

        private void SetEmissives(Color colour)
        {
            block.SetEmissiveParts(BUTTON_EMISSIVE_NAME, colour, 1f);
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

        private void Block_IsWorkingChanged(IMyCubeBlock obj)
        {
            //Log.Msg($"IsWorkingChanged IsWorking = {block.IsWorking}");
            if (!block.IsWorking)
                Reset();
        }
        private void Block_EnabledChanged(IMyTerminalBlock obj)
        {
            //Log.Msg($"EnabledChanged Enabled = {block.Enabled}");
            if (!block.Enabled)
                Reset();
        }

        public override void UpdateAfterSimulation100()
        {
            enableTransferButton = false;

            //Log.Msg($"Tick {block.CubeGrid.DisplayName} IsWorking={block.IsWorking}");
            CheckSCVisability();
            if (!block.IsWorking)
                return;

            if (!object.ReferenceEquals(prevCDRef, block.CustomData))
            {
                prevCDRef = block.CustomData;
                Settings.LoadConfigFromCD(block);
                Reset();
                WriteText("Loading Config");
                return;
            }

                if (gasPump.TargetTanksMarkedForClose()) //cant detect grid change ove trade connector
            {
                Reset();
                WriteText("Ship Tank Removed");
                return;
            }

            if (bootSteps > 0)
            {
                WriteText("Booting....");
                bootSteps--;
                return;
            }

            if (tradeConnector == null || tradeConnector.MarkedForClose)
            {
                tradeConnectorInventory = null;
                tradeConnector = FindTradeConnector();
                if (tradeConnector != null)
                    tradeConnectorInventory = (MyInventory)tradeConnector.GetInventory();
                dockedState = DockedStateEnum.Unknown; //force missmatch
                enableTransfer = false;
                return;
            }

            if (!CheckTradeConnector())
            {
                dockedState = DockedStateEnum.Unknown;
                enableTransfer = false;
                return;
            }

            if (gasPump.SorurceTanksCount == 0)
            {
                if (!gasPump.TryFindSourceTanks(tradeConnector, Settings.GasPumpIdentifier))
                {
                    WriteText($"No source tanks found with identifier: {Settings.GasPumpIdentifier}");
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
                    case DockedStateEnum.Docked:
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
                    case DockedStateEnum.UnDocked:
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

            var cashSC = (int)cashSourceInventory.GetItemAmount(SCDefId);
            var freeSpaceKL = (int)gasPump.TargetH2Tanks.TotalFree / 1000;
            var maxFillKL = (int)Math.Min(freeSpaceKL, cashSC / Settings.PricePerKL);
            switch (dockedState)
            {
                case DockedStateEnum.Docked:
                    {
                        ScreenDocked(cashSC, freeSpaceKL, maxFillKL);
                        enableTransferButton = maxFillKL > 0;
                        break;
                    }
                case DockedStateEnum.UnDocked:
                    {
                        ScreenUndocked(cashSC);
                        break;
                    }
            }

            if (!enableTransferButton)
                enableTransfer = false;

            if (enableTransfer)
            {
                int transferedKL;
                switch (gasPump.BatchTransfer(maxFillKL, out transferedKL))
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

                if (!TryTransferCash(transferedKL))
                {
                    Log.Msg("Cash transfer failed");
                    enableTransfer = false;
                }
            }

            UpdateEmissives();
            // think abut sleep mode.
        }

        private bool TryTransferCash(int transferedKL)
        {
            if (transferedKL == 0)
                return true;

            int amount = transferedKL * Settings.PricePerKL;

            if (cashSourceInventory.ItemCount == 0)
                return false;
            if (cashSourceInventory.RemoveItemsOfType(amount, SCDefId).ToIntSafe() != amount)
            {
                Log.Msg("Failed to transfer full cash amount");
            }

            switch (Settings.CreditMethod)
            {
                default:
                    {
                        Log.Msg("CreditMethod Not implemented");
                        enableTransfer = false;
                        return false;
                    }
                case CreditMethodEnum.TradeConnector:
                    {
                        if (!TryCreditTradeConnector(amount))
                        {
                            enableTransfer = false;
                            Log.Msg("TryCreditTradeConnector failed");
                            return false;
                        }
                        break;
                    }
                case CreditMethodEnum.OwnerAccount:
                    {
                        if (!TryCreditOwner(amount))
                        {
                            enableTransfer = false;
                            Log.Msg("TryCreditOwner failed");
                            return false;
                        }
                        break;
                    }
            }

            return true;
        }

        private bool TryCreditTradeConnector(int amount)
        {
            if (!tradeConnectorInventory.AddItems(amount, SCobjectBuilder))
            {
                Log.Msg("SC cant be added to tradeConnector");
                return false;
            }
            return true;
        }

        private bool TryCreditOwner(int amount)
        {

            return false;
        }

        private void Reset()
        {
            tradeConnector = null;
            tradeConnectorInventory = null;
            gasPump = new GasPump(stationCubeGrid);
            enableTransfer = false;
            enableTransferButton = false;
            dockedState = DockedStateEnum.Unknown;
            bootSteps = DEFAULT_BOOT_STEPS;
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
            screenSB.Append($"Price SC/KL: SC {Settings.PricePerKL} \n");
            screenSB.Append($"SC Inserted: SC {cashSC}\n");
            screenSB.Append($"\n");
            screenSB.Append($"Ship: '{dockedShipName}'\n");
            screenSB.Append($"Free Space: {freeSpaceKL}KL in {gasPump.TargetTanksCount} tanks\n");
            screenSB.Append($"Max Price: SC {freeSpaceKL * Settings.PricePerKL}\n");
            screenSB.Append($"Max Fill: {maxFillKL}KL\n");
            screenSB.Append($"Total Price: SC {(int)maxFillKL * Settings.PricePerKL}\n");
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
            screenSB.Append($"Price SC/KL: SC {Settings.PricePerKL} \n");
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
            var scAmount = (float)cashSourceInventory.GetItemAmount(SCDefId);
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

        private IMyShipConnector FindTradeConnector()
        {
            foreach (var connector in stationCubeGrid.GetFatBlocks<IMyShipConnector>())
            {
                if (!connector.CustomName.Contains(Settings.GasPumpIdentifier))
                    continue;
                WriteText($"Connector: '{connector.CustomName}'");

                return connector;
            }

            WriteText($"No Gas Pump Connector found with\nidentifier: {Settings.GasPumpIdentifier}");
            return null;
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

            return true; ;
        }
    }
}