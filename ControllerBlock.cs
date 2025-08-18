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
        private MyInventory inventory;
        private MyDefinitionId id = new MyDefinitionId(typeof(MyObjectBuilder_PhysicalObject), "SpaceCredit");
        private GasPump gasPump = new GasPump();
        private bool enableTransfer;
        private IMyShipConnector tradeConnector;
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
            inventory = block.GetInventory() as MyInventory;

            CheckSCVisability();

            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
        }
        public override void UpdateAfterSimulation100()
        {
            Log.Msg($"Tick {block.CubeGrid.DisplayName}");
            CheckSCVisability();

            if (tradeConnector == null)
            {
                FindTradeConnector();
                return;
            }

            WriteText("Connector Found");
        }

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();
            Log.Msg($"OnAddedToScene {block.CubeGrid.DisplayName}");
            block.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
            block.WriteText("Hello World1");

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
            var scAmount = (float)inventory.GetItemAmount(id);
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

        private void FindTradeConnector()
        {
            tradeConnector = null;

            foreach (var connector in stationCubeGrid.GetFatBlocks<IMyShipConnector>())
            {
                if (!connector.CustomName.Contains(gasPump.GasPumpIdentifier))
                    continue;
                var connectorBlock = connector.GameLogic.GetAs<ConnectorBlock>();
                if (connectorBlock == null)
                {
                    Log.Msg($"No Gamelogic found on {connector.CustomName}");
                    WriteText($"No Gamelogic found on {connector.CustomName}");
                    break;
                }

                if (connector.GetValue<bool>("Trading"))
                {
                    tradeConnector = connector;
                    connectorBlock.ControllerBlock = this;
                    return;
                }
                Log.Msg($"Connector {connector.CustomName} is not in Trade mode");
                WriteText($"Connector {connector.CustomName} is not in Trade mode");
                tradeConnector = null;
                connectorBlock.ControllerBlock = null;
            }

            return;
        }
    }
}