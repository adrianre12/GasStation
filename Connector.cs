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
    internal class ConnectorBlock : MyGameLogicComponent
    {

        private IMyShipConnector block;
        private IMyCubeGrid cubeGrid;

        internal ControllerBlock ControllerBlock;

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
/*            if (!block.Enabled)
            {
                ControllerBlock.UpdateTradeConnector();
                return;
            }*/
        }

        private void Block_IsConnectedChanged(IMyShipConnector obj)
        {
            Log.Msg($"IsConenctedChanged IsConnected = {block.IsConnected}");
/*            if (!block.IsConnected) {
                targetH2Tanks.Clear();
                enableTransfer = false;
                return;
            }
            tradingMode = block.GetValue<bool>("Trading");
            if (!tradingMode)
                return;

            FindSourceTanks();
            FindTargetTanks();
            enableTransfer = true;*/
        }

        public override void UpdateAfterSimulation100()
        {
            //Log.Msg($"Tick {block.CubeGrid.DisplayName} block.IsWorking={block.IsWorking} tradingMode={tradingMode}");
/*            if (!block.IsConnected || !block.IsWorking || !tradingMode || !enableTransfer || targetH2Tanks.Count == 0)
                return;*/

 

        }



/*        public override void OnAddedToScene()
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

        }*/

 
    }
}