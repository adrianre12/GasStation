using Sandbox.ModAPI;
using System;
using System.Text;
using VRage.Game.ModAPI.Ingame.Utilities;
using static Catopia.GasStation.ControllerBlock;

namespace Catopia.GasStation
{
    internal class Config
    {
        private MyIni myIni = new MyIni();

        private const string KeyPricePerKL = "PricePerKL";
        public const int DefaultPricePerKL = 2507;
        public int PricePerKL = DefaultPricePerKL;

        private const string KeyGasPumpIdentifier = "Identifier";
        public const string DefaultGasPumpIdentifier = "[GS1]";
        public string GasPumpIdentifier = DefaultGasPumpIdentifier;

        private const string KeyCreditMethod = "CreditMethod";
        public const CreditMethodEnum DefaultCreditMethod = CreditMethodEnum.TradeConnector;
        public CreditMethodEnum CreditMethod = DefaultCreditMethod;


        internal void LoadConfigFromCD(IMyTerminalBlock block)
        {
            if (!ParseConfigFromCD(block))
            {
                Log.Msg("Error in CD, creating a new config.");
                GasPumpIdentifier = DefaultGasPumpIdentifier;
                PricePerKL = DefaultPricePerKL;
                CreditMethod = DefaultCreditMethod;

                SaveConfigToCD(block);
            }
        }

        internal void SaveConfigToCD(IMyTerminalBlock block)
        {
            Log.Msg("Saving config to CD.");

            myIni.Clear();
            var sb = new StringBuilder();
            sb.AppendLine("GasStation Settings");
            sb.AppendLine($"{KeyGasPumpIdentifier}: Used to identify connector and H2 tanks.");
            sb.AppendLine("   Each GasStation on this grid needs to be unique [GS1] [GS2] etc.");
            sb.AppendLine($"{KeyPricePerKL}: Default price based on Ice 50SC/Kg and H2 yield of 19.95L/Kg.");
            sb.AppendLine($"{KeyCreditMethod}: Where to put earned Space Credits.");
            sb.AppendLine($"   {CreditMethodEnum.TradeConnector.ToString()}: Put Space Credits in the trade connector inventory.");
            sb.AppendLine($"   {CreditMethodEnum.GridOwner.ToString()}: Put Space Credits in the grid owners account.");
            sb.AppendLine("");
            myIni.AddSection("Settings");
            myIni.SetSectionComment("Settings", sb.ToString());

            myIni.Set("Settings", KeyGasPumpIdentifier, GasPumpIdentifier);
            myIni.Set("Settings", KeyPricePerKL, PricePerKL);
            myIni.Set("Settings", KeyCreditMethod, CreditMethod.ToString());

            myIni.Invalidate();
            block.CustomData = myIni.ToString();
        }

        private bool ParseConfigFromCD(IMyTerminalBlock block)
        {
            //Log.Msg("ParseConfigFromCD");
            if (myIni.TryParse(block.CustomData))
            {
                if (!myIni.ContainsSection("Settings"))
                    return false;

                if (!myIni.Get("Settings", KeyGasPumpIdentifier).TryGetString(out GasPumpIdentifier))
                    return false;

                if (!myIni.Get("Settings", KeyPricePerKL).TryGetInt32(out PricePerKL))
                    return false;

                string tmp;
                if (!myIni.Get("Settings", KeyCreditMethod).TryGetString(out tmp))
                    return false;
                if (!Enum.TryParse<CreditMethodEnum>(tmp, out CreditMethod))
                    return false;

                return true;
            }
            Log.Msg("Error: Failed to load config");
            return false;
        }
    }
}
