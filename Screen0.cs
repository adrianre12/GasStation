using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace Catopia.GasStation
{
    internal class Screen0 : ScreenBase
    {
        private List<string> screenText = new List<string>();
        private bool showCursor;

        internal readonly Color GreenCRT = new Color(51, 255, 0);

        public override void Init(IMyTextSurfaceProvider surfaceProvider, int index)
        {
            base.Init(surfaceProvider, index);
            DefaultRotationOrScale = 0.85f;
            BackgroundColor = Color.MidnightBlue;
        }

        internal void ScreenDocked(int cashSC, int freeSpaceKL, int maxFillKL, ControllerBlock controller)
        {
            var frame = GetFrame();
            var position = new Vector2(5, 0);
            var positionX150 = new Vector2(150, 0);
            //Func<int, Vector2> ph = (x) => { return new Vector2(position.X + x, position.Y); };
            //for (int x = 0; x < viewport.Width; x += 50)
            //    frame.Add(NewTextSprite("_", ph(x)));

            frame.Add(NewTextSprite("Station Name:", position));
            frame.Add(NewTextSprite($"'{controller.block.CubeGrid.DisplayName}'", position + positionX150, Color.Cyan));
            position.Y += LineSpaceing;

            frame.Add(NewTextSprite("H2 Available:", position));
            var availableKL = (int)controller.gasPump.SourceH2Tanks.TotalAvailable / 1000;
            frame.Add(NewTextSprite($"{availableKL}KL", position + positionX150, availableKL > freeSpaceKL ? Color.Green : Color.Red));
            position.Y += LineSpaceing;

            frame.Add(NewTextSprite("Price SC/KL:", position));
            frame.Add(NewTextSprite($"SC {controller.Settings.PricePerKL}", position + positionX150));
            position.Y += 2 * LineSpaceing;

            frame.Add(NewTextSprite("Ship Name:", position));
            frame.Add(NewTextSprite($"'{controller.dockedShipName}'", position + positionX150, Color.Cyan));
            position.Y += LineSpaceing;

            frame.Add(NewTextSprite("Free Space:", position));
            var tankStr = controller.gasPump.TargetTanksCount > 0 ? "tanks" : "tank";
            frame.Add(NewTextSprite($"{freeSpaceKL}KL in {controller.gasPump.TargetTanksCount} {tankStr}", position + positionX150));
            position.Y += LineSpaceing;

            frame.Add(NewTextSprite("Max Price:", position));
            var maxPrice = freeSpaceKL * controller.Settings.PricePerKL;
            frame.Add(NewTextSprite($"SC {maxPrice}", position + positionX150));
            position.Y += LineSpaceing;

            frame.Add(NewTextSprite("SC Inserted:", position));
            frame.Add(NewTextSprite($"SC {cashSC}", position + positionX150, maxPrice > cashSC ? Color.Red : Color.Green));
            position.Y += LineSpaceing;

            frame.Add(NewTextSprite("Max Fill:", position));
            frame.Add(NewTextSprite($"{maxFillKL}KL", position + positionX150, Color.Yellow));
            position.Y += LineSpaceing;

            frame.Add(NewTextSprite("Total Price:", position));
            frame.Add(NewTextSprite($"SC {maxFillKL * controller.Settings.PricePerKL}", position + positionX150, Color.Yellow));
            position.Y += 2 * LineSpaceing;

            /*            Func<int, Vector2> ph = (x) => { return new Vector2(position.X + x, position.Y); };
                        for (int x = 0; x < viewport.Width; x += 50)
                            frame.Add(NewTextSprite("_", ph(x)));*/

            if (maxFillKL > 0)
            {
                if (controller.enableTransfer.Value)
                    frame.Add(NewTextSprite($"Press button to Stop", position + new Vector2(25, 0), Color.Red));
                else
                    frame.Add(NewTextSprite($"Press button to Start", position + new Vector2(25, 0), Color.Green));

            }

            frame.Add(NewTextSprite("Insert Space Credits", position + new Vector2(280, 0), Color.Yellow));


            frame.Dispose();
        }


        internal void ScreenUndocked(int cashSC, ControllerBlock controller)
        {
            var frame = GetFrame();
            var position = new Vector2(5, 0);
            var positionX150 = new Vector2(150, 0);
            //Func<int, Vector2> ph = (x) => { return new Vector2(position.X + x, position.Y); };
            //for (int x = 0; x < viewport.Width; x += 50)
            //    frame.Add(NewTextSprite("_", ph(x)));

            frame.Add(NewTextSprite("Station Name:", position));
            frame.Add(NewTextSprite($"'{controller.block.CubeGrid.DisplayName}'", position + positionX150, Color.Cyan));
            position.Y += LineSpaceing;

            frame.Add(NewTextSprite("H2 Available:", position));
            var availableKL = (int)controller.gasPump.SourceH2Tanks.TotalAvailable / 1000;
            frame.Add(NewTextSprite($"{availableKL}KL", position + positionX150, availableKL > 0 ? Color.Green : Color.Red));
            position.Y += LineSpaceing;

            frame.Add(NewTextSprite("Price SC/KL:", position));
            frame.Add(NewTextSprite($"SC {controller.Settings.PricePerKL}", position + positionX150));
            position.Y += LineSpaceing;

            frame.Add(NewTextSprite("SC Inserted:", position));
            frame.Add(NewTextSprite($"SC {cashSC}", position + positionX150, cashSC > 0 ? Color.Green : Color.Red));
            position.Y += 3 * LineSpaceing;


            frame.Add(NewTextSprite("No Ship Docked", position + new Vector2(viewport.Width / 2, 0), Color.Cyan, 1.5f, TextAlignment.CENTER, DefaultFontId));
            position.Y += 5 * LineSpaceing;


            frame.Add(NewTextSprite("Insert Space Credits", position + new Vector2(280, 0), Color.Yellow));


            frame.Dispose();
        }

        internal void ScreenText(string text)
        {
            AddText(text);
            ScreenText();
        }

        internal void AddText(string text)
        {
            if (screenText.Count > 11)
                screenText.RemoveAt(0);
            screenText.Add(text);
            showCursor = true;
        }

        internal void ClearText()
        {
            screenText.Clear();
        }

        internal void ScreenText()
        {
            var frame = GetFrame(Color.Black);
            var position = new Vector2(5, 0);
            //var positionX150 = new Vector2(150, 0);
            //Func<int, Vector2> ph = (x) => { return new Vector2(position.X + x, position.Y); };
            //for (int x = 0; x < viewport.Width; x += 50)
            //    frame.Add(NewTextSprite("_", ph(x)));

            foreach (var line in screenText)
            {
                frame.Add(NewTextSprite(line, position, GreenCRT));
                position.Y += LineSpaceing;
            }

            if (showCursor)
            {
                frame.Add(NewTextSprite("|", position, GreenCRT));
            }
            showCursor = !showCursor;

            frame.Dispose();
        }

        internal void ScreenSleep()
        {
            var frame = GetFrame(Color.Black);
            var position = new Vector2(viewport.Width / 2, viewport.Height / 2 - 45);

            frame.Add(NewTextSprite("Cash Is King", position, Color.Cyan, 1.5f, TextAlignment.CENTER, DefaultFontId));

            position.Y += 2 * LineSpaceing;
            frame.Add(NewTextSprite("Press Button", position, Color.Cyan, 1.5f, TextAlignment.CENTER, DefaultFontId));

            frame.Dispose();
        }
    }
}
