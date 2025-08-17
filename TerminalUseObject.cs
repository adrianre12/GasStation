using Sandbox.ModAPI;
using VRage.Game.Entity.UseObject;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;

namespace Catopia.GasStation
{
     [MyUseObject("GasStationToggleFill")]
    public class TerminalUseObject : MyUseObjectBase
    {
        private IMyTextPanel block;
        private Terminal terminal;

        public override UseActionEnum SupportedActions => UseActionEnum.Manipulate
                                                        | UseActionEnum.Close
                                                        | UseActionEnum.BuildPlanner
                                                        | UseActionEnum.OpenInventory
                                                        | UseActionEnum.OpenTerminal
                                                        | UseActionEnum.PickUp
                                                        | UseActionEnum.UseFinished; // gets called when releasing manipulate

        // What action gets sent to Use() when interacted with PrimaryAttack or Use binds.
        public override UseActionEnum PrimaryAction => UseActionEnum.Manipulate;

        // What action gets sent to Use() when interacted with SecondaryAttack or Inventory/Terminal binds.
        public override UseActionEnum SecondaryAction => UseActionEnum.OpenTerminal;

        public TerminalUseObject(IMyEntity owner, string dummyName, IMyModelDummy dummyData, uint shapeKey) : base(owner, dummyData)
        {
            block = owner as IMyTextPanel;
            terminal = block.GameLogic.GetAs<Terminal>();
        }

        public override MyActionDescription GetActionInfo(UseActionEnum actionEnum)
        {
            switch (actionEnum)
            {
                default:
                    return default(MyActionDescription);

                case UseActionEnum.Manipulate:
                    return new MyActionDescription()
                    {
                        Text = MyStringId.GetOrCompute("Toggle Gas transfer On/Off"),
                        IsTextControlHint = true,
                    };
            }
        }

        public override void Use(UseActionEnum actionEnum, IMyEntity user)
        {
            switch(actionEnum)
            {
                case UseActionEnum.Manipulate:
                    {
                        if (terminal != null)
                        {
                            terminal.WriteText("Oh you pressed it!");
                        }

                        break;
                    }
            }
        }

    }
}