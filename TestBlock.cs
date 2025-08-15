using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace SEtest
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_VirtualMass), false, new[] { "TestBlockLarge", "TestBlockSmall" })]
    internal class TestBlock : MyGameLogicComponent
    {
        private IMyFunctionalBlock block;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            block = Entity as IMyFunctionalBlock;

            NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;

        }

        public override void UpdateAfterSimulation100()
        {
            Log.Msg($"Tick {block.CubeGrid.DisplayName}");
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
    }
}