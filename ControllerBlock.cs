using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Network;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Sync;
using VRageMath;


namespace Catopia.GasStation
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_TextPanel), false, new[] { "GasStationController" })]
    public class ControllerBlock : MyGameLogicComponent
    {
        private const string BUTTON_EMISSIVE_NAME = "Emissive1";
        private const int DEFAULT_BOOT_STEPS = 2;
        private const int DEFAULT_SLEEP_COUNT = 17; //187;

        internal IMyTextPanel block;
        internal GasPump gasPump;
        internal MySync<bool, SyncDirection.BothWays> enableTransfer;
        internal MySync<bool, SyncDirection.FromServer> enableTransferButton;
        internal MySync<bool, SyncDirection.BothWays> sleepWake;
        internal bool sleepMode;
        private int sleepCounter;
        private IMyShipConnector tradeConnector;
        private MyInventory tradeConnectorInventory;
        private MyInventory cashSourceInventory;
        private IMyCubeGrid stationCubeGrid;
        private DockedStateEnum dockedState = DockedStateEnum.Unknown;
        internal string dockedShipName;
        private int bootSteps = DEFAULT_BOOT_STEPS;

        // private List<string> screenText = new List<string>();

        private Color BLACK = new Color(0, 0, 0);
        private Color RED = new Color(255, 0, 0);
        //private Color GREY = new Color(128, 128, 128);
        private Color MUSTARD = new Color(255, 255, 0);
        private Color GREEN = new Color(10, 255, 0);
        //private Color CYAN = new Color(0, 255, 255);

        private MyDefinitionId SCDefId = new MyDefinitionId(typeof(MyObjectBuilder_PhysicalObject), "SpaceCredit");
        private MyObjectBuilder_PhysicalObject SCobjectBuilder;

        internal Config Settings = new Config();
        private string prevCDRef;
        private Screen0 screen0;

        private enum DockedStateEnum
        {
            Unknown,
            UnDocked,
            Docked
        }

        public enum CreditMethodEnum
        {
            TradeConnector,
            GridOwner
        }

        private DockedStateEnum DockedStateFromBool(bool value)
        {
            if (value)
                return DockedStateEnum.Docked;
            return DockedStateEnum.UnDocked;
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            block = Entity as IMyTextPanel;
            Log.DebugLog = true;

            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            if (block.CubeGrid?.Physics == null)
                return;

            cashSourceInventory = block.GetInventory() as MyInventory;
            if (!MyAPIGateway.Utilities.IsDedicated) //client only
            {
                SetEmissives(RED);
                cashSourceInventory.ContentsChanged += CashSourceInventory_ContentsChanged;
                CashSourceInventory_ContentsChanged(cashSourceInventory);
                enableTransfer.ValueChanged += EnableTransfer_ValueChanged;
                enableTransferButton.ValueChanged += EnableTransferButton_ValueChanged;
            }

            /*            block.IsWorkingChanged += Block_IsWorkingChanged;
                        block.EnabledChanged += Block_EnabledChanged;*/

            if (!MyAPIGateway.Session.IsServer)
                return;

            stationCubeGrid = block.CubeGrid;
            gasPump = new GasPump(stationCubeGrid);

            block.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
            block.WriteText(".");
            block.Font = "Debug";
            block.FontSize = 0.85f;

            Settings.LoadConfigFromCD(block);

            SCobjectBuilder = (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(SCDefId);

            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
            block.CubeGrid.OnBlockRemoved += CubeGrid_OnBlockRemoved;
            block.CubeGrid.OnBlockAdded += CubeGrid_OnBlockAdded;
            sleepWake.ValueChanged += SleepWake_ValueChanged;
            screen0 = new Screen0();
            screen0.Init((IMyTextSurfaceProvider)block, 0);
        }

        private void SleepWake_ValueChanged(MySync<bool, SyncDirection.BothWays> obj)
        {
            Log.Msg($"Wake {sleepWake.Value}, mode = {sleepMode}");
            //sleepWake.Value = false;
            sleepCounter = 0;
            if (!sleepMode)
                return;
            Reset();
            screen0.ScreenText();
        }

        public override void UpdateAfterSimulation100()
        {

            enableTransferButton.Value = false;

            //Log.Msg($"Tick {block.CubeGrid.DisplayName} IsWorking={block.IsWorking}");
            if (!block.Enabled || !block.IsFunctional || !block.IsWorking)
            {
                Reset();
                return;
            }
            Log.Msg($"SleepMode={sleepMode} Counter={sleepCounter}");

            if (sleepMode)
            {
                Log.Msg($"counter={sleepCounter}");
                if (--sleepCounter > 0)
                    return;
                sleepCounter = 5;
                screen0.ScreenSleep();
                return;
            }
            if (++sleepCounter > DEFAULT_SLEEP_COUNT)
            {
                sleepCounter = 0;
                sleepMode = true;
            }


            if (!ReferenceEquals(prevCDRef, block.CustomData)) // detect CD changed.
            {
                prevCDRef = block.CustomData;
                Settings.LoadConfigFromCD(block);
                Reset();
                screen0.ScreenText("Loading Config");
                return;
            }

            if (gasPump.TargetTanksMarkedForClose()) //cant detect grid change ove trade connector
            {
                Reset();
                screen0.ScreenText("Ship Tank Removed");
                return;
            }

            if (bootSteps > 0)
            {
                screen0.ScreenText("Booting....");
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
                enableTransfer.Value = false;
                return;
            }

            if (!CheckTradeConnector())
            {
                dockedState = DockedStateEnum.Unknown;
                enableTransfer.Value = false;
                return;
            }

            if (gasPump.SorurceTanksCount == 0)
            {
                if (!gasPump.TryFindSourceTanks(tradeConnector, Settings.GasPumpIdentifier))
                {
                    screen0.ScreenText($"No source tanks found with identifier: {Settings.GasPumpIdentifier}");
                    enableTransfer.Value = false;
                    return;
                }
                screen0.ScreenText($"Source Tanks found: {gasPump.SorurceTanksCount}");
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
                                    screen0.AddText($"No target tanks found on ship");
                                }
                                screen0.AddText($"Docked Ship: '{dockedShipName}'");
                                screen0.ScreenText($"Ship Tanks found: {gasPump.TargetTanksCount}");
                            }

                            break;
                        }
                    case DockedStateEnum.UnDocked:
                        {
                            //Log.Msg(">>target tanks reset");
                            dockedShipName = null;
                            screen0.ScreenText($"No Ship Docked");
                            gasPump.TargetTanksReset();
                            enableTransfer.Value = false;
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
                        screen0.ScreenDocked(cashSC, freeSpaceKL, maxFillKL, this);
                        enableTransferButton.Value = maxFillKL > 0;
                        break;
                    }
                case DockedStateEnum.UnDocked:
                    {
                        screen0.ScreenUndocked(cashSC, this);
                        break;
                    }
            }

            if (!enableTransferButton)
                enableTransfer.Value = false;

            if (enableTransfer)
            {
                sleepMode = false;
                int transferedKL;
                switch (gasPump.BatchTransfer(maxFillKL, out transferedKL))
                {
                    case GasPump.TransferResult.Continue:
                        {
                            break;
                        }
                    default:
                        {
                            enableTransfer.Value = false;
                            break;
                        }
                }

                if (!TryTransferCash(transferedKL))
                {
                    Log.Msg("Cash transfer failed");
                    enableTransfer.Value = false;
                }
            }

            // think abut sleep mode.
        }

        public override void Close()
        {
            enableTransfer.ValueChanged -= EnableTransfer_ValueChanged;
            enableTransferButton.ValueChanged -= EnableTransferButton_ValueChanged;
            //block.EnabledChanged -= Block_EnabledChanged;
            //block.IsWorkingChanged -= Block_IsWorkingChanged;
            block.CubeGrid.OnBlockRemoved -= CubeGrid_OnBlockRemoved;
            block.CubeGrid.OnBlockAdded -= CubeGrid_OnBlockAdded;
            base.Close();
        }


        private void CubeGrid_OnBlockAdded(IMySlimBlock obj)
        {
            Log.Msg("BLOCK ADDED");
            ;
        }

        private void CubeGrid_OnBlockRemoved(IMySlimBlock obj)
        {
            Log.Msg("BLOCK REMOVED");

            if (tradeConnector != null && tradeConnector.MarkedForClose)
            {
                Reset();
                screen0.ScreenText("Trade Connector Removed");
                return;
            }
            if (gasPump != null && gasPump.SourceTanksMarkedForClose())
            {
                Reset();
                screen0.ScreenText("Gas Tank Removed");
            }
        }

        /*        private void Block_EnabledChanged(IMyTerminalBlock obj)
                {
                    Log.Msg($"Enable Enabled={block.Enabled} IsWorking={block.IsWorking}");

                    if (!MyAPIGateway.Utilities.IsDedicated) //client only
                    {
                        UpdateEmissives();
                        return;
                    }
                    if (!block.Enabled)
                        Reset();
                }

                private void Block_IsWorkingChanged(IMyCubeBlock obj)
                {
                    Log.Msg($"IsWorking Enabled={block.Enabled} IsWorking={block.IsWorking}");

                    if (!MyAPIGateway.Utilities.IsDedicated) //client only
                    {
                        UpdateEmissives();
                        return;
                    }
                    if (!block.IsWorking)
                        Reset();
                }*/

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
                        enableTransfer.Value = false;
                        return false;
                    }
                case CreditMethodEnum.TradeConnector:
                    {
                        if (!TryCreditTradeConnector(amount))
                        {
                            enableTransfer.Value = false;
                            Log.Msg("TryCreditTradeConnector failed");
                            return false;
                        }
                        break;
                    }
                case CreditMethodEnum.GridOwner:
                    {
                        if (!TryCreditGridOwner(amount))
                        {
                            enableTransfer.Value = false;
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

        private bool TryCreditGridOwner(int amount)
        {
            var gridOwner = stationCubeGrid.BigOwners[0];
            List<IMyPlayer> players = new List<IMyPlayer>();
            MyAPIGateway.Multiplayer.Players.GetPlayers(players, (p) => { return p.IdentityId == gridOwner; });

            Log.Msg($"players.Count = {players?.Count}");

            if (players == null || players.Count == 0)
                return false;
            players[0].RequestChangeBalance(amount);
            return true;
        }

        private void Reset()
        {
            tradeConnector = null;
            tradeConnectorInventory = null;
            gasPump = new GasPump(stationCubeGrid);
            enableTransfer.Value = false;
            enableTransferButton.Value = false;
            dockedState = DockedStateEnum.Unknown;
            bootSteps = DEFAULT_BOOT_STEPS;
            sleepMode = false;
            screen0.ClearText();
        }

        private IMyShipConnector FindTradeConnector()
        {
            foreach (var connector in stationCubeGrid.GetFatBlocks<IMyShipConnector>())
            {
                if (!connector.CustomName.Contains(Settings.GasPumpIdentifier))
                    continue;
                screen0.ScreenText($"Connector: '{connector.CustomName}'");

                return connector;
            }

            screen0.ScreenText($"No Connector found with: {Settings.GasPumpIdentifier}");
            return null;
        }

        private bool CheckTradeConnector()
        {
            if (!tradeConnector.IsWorking)
            {
                screen0.ScreenText($"Connector: '{tradeConnector.CustomName}' Not Enabled.");
                return false;
            }

            if (!tradeConnector.GetValue<bool>("Trading"))
            {
                screen0.ScreenText($"Connector: '{tradeConnector.CustomName}' Not in Trade mode.");
                return false;
            }

            return true; ;
        }

        // On client

        private void CashSourceInventory_ContentsChanged(MyInventoryBase cashInventory)
        {
            var scAmount = (float)cashInventory.GetItemAmount(SCDefId);
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
            sleepWake.Value = !sleepWake.Value;
            enableTransfer.Value = enableTransferButton.Value && !enableTransfer.Value;
        }

        private void EnableTransferButton_ValueChanged(MySync<bool, SyncDirection.FromServer> obj)
        {
            UpdateEmissives();
        }

        private void EnableTransfer_ValueChanged(MySync<bool, SyncDirection.BothWays> obj)
        {
            UpdateEmissives();
        }

        internal void UpdateEmissives()
        {
            if (!block.IsWorking)
            {
                SetEmissives(BLACK);
                return;
            }
            if (!enableTransferButton)
            {
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

        private void SetEmissives(Color colour)
        {
            block.SetEmissiveParts(BUTTON_EMISSIVE_NAME, colour, 1f);
        }

    }
}