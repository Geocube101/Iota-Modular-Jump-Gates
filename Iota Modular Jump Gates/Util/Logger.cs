using System.Linq;
using VRage.Utils;

namespace IOTA.ModularJumpGates.Util
{
    /// <summary>
    /// Class holding the console and file logger
    /// </summary>
    internal static class Logger
    {
		#region Public Static Methods
        /// <summary>
        /// Writes a message with "Debug" prefix
        /// </summary>
        /// <param name="message">The message to write</param>
        /// <param name="verbosity">The message verbosity</param>
		public static void Debug(string message, byte verbosity = 0)
        {
            if (!MyJumpGateModSession.DebugMode && (MyJumpGateModSession.Configuration != null && verbosity > MyJumpGateModSession.Configuration.GeneralConfiguration.DebugLogVerbosity)) return;
            message = string.Join("\n  ...  ", message.Split('\n').Where((s) => s.Trim().Length > 0));
            message = $"[ {MyJumpGateModSession.MODID} ] [ DEBUG -> {verbosity} ]: {message}";
			MyLog.Default.WriteLine(message);
            if (verbosity < 3) MyLog.Default.WriteLineToConsole(message);
		}

        /// <summary>
        /// Writes a message with the "Log" prefix
        /// </summary>
        /// <param name="message">The message to write</param>
        public static void Log(string message)
        {
            message = string.Join("\n  ...  ", message.Split('\n').Where((s) => s.Trim().Length > 0));
            MyLog.Default.WriteLineAndConsole($"[ {MyJumpGateModSession.MODID} ] [ INFO ]: {message}");
        }

		/// <summary>
		/// Writes a message with the "Warn" prefix
		/// </summary>
		/// <param name="message">The message to write</param>
		public static void Warn(string message)
        {
            message = string.Join("\n  ...  ", message.Split('\n').Where((s) => s.Trim().Length > 0));
            MyLog.Default.WriteLineAndConsole($"[ {MyJumpGateModSession.MODID} ] [ WARN ]:  {message}");
        }

		/// <summary>
		/// Writes a message with the "Error" prefix
		/// </summary>
		/// <param name="message">The message to write</param>
		public static void Error(string message)
        {
            message = string.Join("\n  ...  ", message.Split('\n').Where((s) => s.Trim().Length > 0));
            MyLog.Default.WriteLineAndConsole($"[ {MyJumpGateModSession.MODID} ] [ ERROR ]:  {message}");
        }

		/// <summary>
		/// Writes a message with the "Critical" prefix
		/// </summary>
		/// <param name="message">The message to write</param>
		public static void Critical(string message)
        {
            message = string.Join("\n  ...  ", message.Split('\n').Where((s) => s.Trim().Length > 0));
            MyLog.Default.WriteLineAndConsole($"[ {MyJumpGateModSession.MODID} ] [ CRITICAL ]:  {message}");
        }

        /// <summary>
        /// Flushes the buffer to file
        /// </summary>
        public static void Flush()
        {
            MyLog.Default.Flush();
        }
		#endregion
	}
}
