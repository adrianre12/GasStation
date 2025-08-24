using Sandbox.ModAPI;
using VRageMath;

namespace Catopia.GasStation
{
    internal class Screen0 : ScreenBase
    {
        public override void Init(IMyTextSurfaceProvider surfaceProvider, int index)
        {
            base.Init(surfaceProvider, index);
            DefaultRotationOrScale = 0.85f;
        }

        public void ScreenDocked(int cashSC, int freeSpaceKL, int maxFillKL, ControllerBlock controller)
        {
            var frame = GetFrame();
            var position = Vector2.Zero;

            frame.Add(NewTextSprite($"Station: '{controller.block.CubeGrid.DisplayName}'", position));
            position.Y += LineSpaceing;
            frame.Add(NewTextSprite($"H2 Available: {(int)controller.gasPump.SourceH2Tanks.TotalAvailable / 1000}KL", position));
            position.Y += LineSpaceing;
            frame.Add(NewTextSprite($"Price SC/KL: SC {controller.Settings.PricePerKL}", position));
            position.Y += LineSpaceing;
            frame.Add(NewTextSprite($"SC Inserted: SC {cashSC}", position));
            position.Y += 2 * LineSpaceing;
            frame.Add(NewTextSprite($"Ship: '{controller.dockedShipName}'", position));
            position.Y += LineSpaceing;
            frame.Add(NewTextSprite($"Free Space: {freeSpaceKL}KL in {controller.gasPump.TargetTanksCount} tanks", position));
            position.Y += LineSpaceing;
            frame.Add(NewTextSprite($"Max Price: SC {freeSpaceKL * controller.Settings.PricePerKL}", position));
            position.Y += LineSpaceing;
            frame.Add(NewTextSprite($"Max Fill: {maxFillKL}KL", position));
            position.Y += LineSpaceing;
            frame.Add(NewTextSprite($"Total Price: SC {(int)maxFillKL * controller.Settings.PricePerKL}", position));
            position.Y += 2 * LineSpaceing;

            if (cashSC == 0)
            {
                frame.Add(NewTextSprite("Insert Space Credits", position));
            }
            else
            {
                string tmp = controller.enableTransfer.Value ? "Stop" : "Start";
                frame.Add(NewTextSprite($"Press button to {tmp}", position));
            }

            frame.Dispose();
        }
    }
}
