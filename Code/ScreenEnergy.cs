using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace Catopia.GasStation
{
    internal class ScreenEnergy : ScreenBase
    {
        private List<string> screenText = new List<string>();

        private readonly Color GreenCRT = new Color(51, 255, 0);
        private string energyName;
        private string unitName;
        private string holderPlural;
        private string holderSingular;
        private string noteLine;

        internal ScreenEnergy(IMyTextSurfaceProvider surfaceProvider, int index, string energyName, string unitName, string holderSingular, string holderPlural, string noteLine = "")
        {
            base.Init(surfaceProvider, index);
            DefaultRotationOrScale = 0.85f;
            BackgroundColor = Color.MidnightBlue;
            this.energyName = energyName;
            this.unitName = unitName;
            this.holderPlural = holderPlural;
            this.holderSingular = holderSingular;
            this.noteLine = noteLine;
        }

        internal void ScreenDocked(int cashSC, int freeSpaceK, int maxFillK, ControllerBlockBase controller)
        {
            var frame = GetFrame();
            var position = new Vector2(5, 0);
            var positionX150 = new Vector2(170, 0);
            //Func<int, Vector2> ph = (x) => { return new Vector2(position.X + x, position.Y); };
            //for (int x = 0; x < viewport.Width; x += 50)
            //    frame.Add(NewTextSprite("_", ph(x)));

            frame.Add(NewTextSprite("Station Name:", position));
            frame.Add(NewTextSprite($"'{controller.block.CubeGrid.DisplayName}'", position + positionX150, Color.Cyan));
            position.Y += LineSpaceing;

            frame.Add(NewTextSprite($"{energyName} Available:", position));
            var availableKL = (int)Math.Round(controller.energyPump.SourceEnergy.TotalAvailable / 100) / 10; //fudge the rounding for display
            frame.Add(NewTextSprite($"{availableKL}{unitName}", position + positionX150, availableKL > freeSpaceK ? Color.Green : Color.Red));
            position.Y += LineSpaceing;

            frame.Add(NewTextSprite($"Price SC/{unitName}:", position));
            frame.Add(NewTextSprite($"SC {controller.Settings.PricePerK}", position + positionX150));
            position.Y += 2 * LineSpaceing;

            frame.Add(NewTextSprite("Ship Name:", position));
            frame.Add(NewTextSprite($"'{controller.dockedShipName}'", position + positionX150, Color.Cyan));
            position.Y += LineSpaceing;

            frame.Add(NewTextSprite("Free Space:", position));
            var holderStr = controller.energyPump.TargetHoldersCount == 1 ? holderSingular : holderPlural;
            frame.Add(NewTextSprite($"{freeSpaceK}{unitName} in {controller.energyPump.TargetHoldersCount} {holderStr}", position + positionX150));
            position.Y += LineSpaceing;

            frame.Add(NewTextSprite("Max Price:", position));
            var maxPrice = freeSpaceK * controller.Settings.PricePerK;
            frame.Add(NewTextSprite($"SC {maxPrice}", position + positionX150));
            position.Y += LineSpaceing;

            frame.Add(NewTextSprite("SC Inserted:", position));
            frame.Add(NewTextSprite($"SC {cashSC}", position + positionX150, maxPrice > cashSC ? Color.Red : Color.Green));
            position.Y += LineSpaceing;

            frame.Add(NewTextSprite("Transfer:", position));
            frame.Add(NewTextSprite($"{maxFillK}{unitName}", position + positionX150, Color.Yellow));
            position.Y += LineSpaceing;

            frame.Add(NewTextSprite("Total Price:", position));
            frame.Add(NewTextSprite($"SC {maxFillK * controller.Settings.PricePerK}", position + positionX150, Color.Yellow));
            position.Y += LineSpaceing;

            frame.Add(NewTextSprite(noteLine, position, 0.5f));
            position.Y += LineSpaceing;

            if (maxFillK > 0)
            {
                if (controller.enableTransfer.Value)
                    frame.Add(NewTextSprite($"Press button to Stop", position + new Vector2(25, 0), Color.Red));
                else
                    frame.Add(NewTextSprite($"Press button to Start", position + new Vector2(25, 0), Color.Green));

            }

            frame.Add(NewTextSprite("Insert Space Credits", position + new Vector2(280, 0), Color.Yellow));


            frame.Dispose();
        }


        internal void ScreenUndocked(int cashSC, ControllerBlockBase controller)
        {
            var frame = GetFrame();
            var position = new Vector2(5, 0);
            var positionX150 = new Vector2(170, 0);
            //Func<int, Vector2> ph = (x) => { return new Vector2(position.X + x, position.Y); };
            //for (int x = 0; x < viewport.Width; x += 50)
            //    frame.Add(NewTextSprite("_", ph(x)));

            frame.Add(NewTextSprite("Station Name:", position));
            frame.Add(NewTextSprite($"'{controller.block.CubeGrid.DisplayName}'", position + positionX150, Color.Cyan));
            position.Y += LineSpaceing;

            frame.Add(NewTextSprite($"{unitName} Available:", position));
            var availableKL = (int)controller.energyPump.SourceEnergy.TotalAvailable / 1000;
            frame.Add(NewTextSprite($"{availableKL}{unitName}", position + positionX150, availableKL > 0 ? Color.Green : Color.Red));
            position.Y += LineSpaceing;

            frame.Add(NewTextSprite($"Price SC/{unitName}:", position));
            frame.Add(NewTextSprite($"SC {controller.Settings.PricePerK}", position + positionX150));
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
