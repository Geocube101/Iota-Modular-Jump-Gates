using Sandbox.ModAPI;
using System;
using System.IO;
using System.Linq;
using VRage.Utils;

namespace IOTA.ModularJumpGates.Util
{
    /// <summary>
    /// Class holding the console and file logger
    /// </summary>
    internal static class Logger
    {
        #region Private Static Variables
		/// <summary>
		/// The writer to write to the internal mod log file
		/// </summary>
        private static TextWriter ModLogWriter = null;
		#endregion

		#region Private Static Methods
		/// <summary>
		/// Writes information to the internal mod-specific log file
		/// </summary>
		/// <param name="category">The logging category</param>
		/// <param name="message">The message</param>
		private static void WriteInternal(string category, string message)
		{
            if (Logger.ModLogWriter == null) return;
			string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
			Logger.ModLogWriter.WriteLine($"[{timestamp}] -> {category}: {message}");
		}
		#endregion

		#region Public Static Methods
		/// <summary>
		/// Opens a new mod-specific log file
		/// </summary>
		public static void Init()
        {
			if (Logger.ModLogWriter != null) return;
            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH-mm-ss");
            string filename = $"IotaModularJumpGates ({timestamp}).log";

            try
            {
				TextWriter writer = MyAPIGateway.Utilities.WriteFileInGlobalStorage(filename);
				Logger.Log($"Mod-specifid log file created");
				Logger.ModLogWriter = writer;
				Logger.ModLogWriter.WriteLine($"-=-=-= [ Iota's Modular Jump Gates (LOG) ] =-=-=-\n");
                Logger.WriteInternal("MGMT", "Log Started");
			}
            catch (Exception e)
			{
				Logger.ModLogWriter = null;
				Logger.Critical($"Failed to open mod-specific log file\n  ...\n[ {e.GetType().Name} ]: {e.Message}\n{e.StackTrace}\n{e.InnerException}");
			}
        }

		/// <summary>
		/// Closes the mod-specific log file
		/// </summary>
        public static void Close()
        {
			if (Logger.ModLogWriter == null) return;
			Logger.WriteInternal("MGMT", "Log Closed");
            Logger.ModLogWriter.Flush();
            Logger.ModLogWriter.Close();
			Logger.ModLogWriter = null;
		}

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
            Logger.WriteInternal("DEBUG", message);
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
			Logger.WriteInternal("INFO", message);
		}

		/// <summary>
		/// Writes a message with the "Warn" prefix
		/// </summary>
		/// <param name="message">The message to write</param>
		public static void Warn(string message)
        {
            message = string.Join("\n  ...  ", message.Split('\n').Where((s) => s.Trim().Length > 0));
            MyLog.Default.WriteLineAndConsole($"[ {MyJumpGateModSession.MODID} ] [ WARN ]:  {message}");
			Logger.WriteInternal("WARN", message);
		}

		/// <summary>
		/// Writes a message with the "Error" prefix
		/// </summary>
		/// <param name="message">The message to write</param>
		public static void Error(string message)
        {
            message = string.Join("\n  ...  ", message.Split('\n').Where((s) => s.Trim().Length > 0));
            MyLog.Default.WriteLineAndConsole($"[ {MyJumpGateModSession.MODID} ] [ ERROR ]:  {message}");
			Logger.WriteInternal("ERROR", message);
		}

		/// <summary>
		/// Writes a message with the "Critical" prefix
		/// </summary>
		/// <param name="message">The message to write</param>
		public static void Critical(string message)
        {
            message = string.Join("\n  ...  ", message.Split('\n').Where((s) => s.Trim().Length > 0));
            MyLog.Default.WriteLineAndConsole($"[ {MyJumpGateModSession.MODID} ] [ CRITICAL ]:  {message}");
			Logger.WriteInternal("CRITICAL", message);
		}

        /// <summary>
        /// Flushes the buffer to file
        /// </summary>
        public static void Flush()
        {
            MyLog.Default.Flush();
            Logger.ModLogWriter?.Flush();
		}
		#endregion
	}
}
