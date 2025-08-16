using VRage.Utils;

namespace Catopia.GasStation
{
    public static class Log
    {
        const string Prefix = "GasStation";

        public static bool DebugLog;
        public static void Msg(string msg)
        {
            MyLog.Default.WriteLine($"{Prefix}: {msg}");
        }

        public static void Debug(string msg)
        {
            if (DebugLog)
                Msg($"[DEBUG] {msg}");
        }
    }
}
