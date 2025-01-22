using Verse;

namespace CaravanMyWay
{
    public static class ModLogger
    {
        private const string PREFIX = "[CaravanMyWay] ";
        private static bool debugMode = false;

        public static bool IsDebugEnabled => debugMode;

        public static void Debug(string message)
        {
            if (debugMode)
                Log.Message(PREFIX + message);
        }

        public static void Message(string message)
        {
            Log.Message(PREFIX + message);
        }

        public static void Warning(string message)
        {
            Log.Warning(PREFIX + message);
        }

        public static void Error(string message)
        {
            Log.Error(PREFIX + message);
        }

        public static void ToggleDebug(bool enable)
        {
            debugMode = enable;
        }
    }
}