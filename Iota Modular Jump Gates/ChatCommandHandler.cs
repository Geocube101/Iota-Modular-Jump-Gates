using IOTA.ModularJumpGates.ChatCommands;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;

namespace IOTA.ModularJumpGates.Commands
{
	internal static class MyChatCommandHandler
	{
		private static Dictionary<string, MyChatCommand> Commands = new Dictionary<string, MyChatCommand>() {
			["reload"] = new MyReloadChatCommand(),
			["debug"] = new MyDebugChatCommand(),
		};

		public static bool Initialized { get; private set; } = false;

		private static void OnChatCommand(ulong sender, string message, ref bool broadcast)
		{
			ulong thisid = MyAPIGateway.Multiplayer.MyId;
			if (!(message = message.Trim().ToLowerInvariant()).StartsWith("/imjg") || sender != thisid) return;
			broadcast = false;
			char lc = '\x00';
			List<string> arguments = new List<string>();
			string part = "";
			bool split = true;

			foreach (char c in message)
			{
				if (Char.IsWhiteSpace(c) && split)
				{
					arguments.Add(part);
					part = "";
				}
				else if (c == '"' && split) split = false;
				else if (c == '"' && lc != '\\') split = true;
				else part += c;
				lc = c;
			}

			arguments.Add(part);
			arguments.RemoveAll((p) => p.Length == 0);

			if (arguments.Count == 1)
			{
				StringBuilder sb = new StringBuilder();
				sb.Append($" >>\n-=[ {MyTexts.GetString("ChatCommandHandler_CommandListing")} ]=-\n");
				foreach (KeyValuePair<string, MyChatCommand> pair in MyChatCommandHandler.Commands) sb.Append($" - [{((pair.Value.RequiresAdmin) ? "*" : "")}{pair.Value.CommandName}] - {pair.Value.CommandDescription}\n");
				MyAPIGateway.Utilities.ShowMessage(MyJumpGateModSession.DISPLAYNAME, sb.ToString());
				return;
			}

			string command_name = arguments[1];
			MyChatCommand command = MyChatCommandHandler.Commands.GetValueOrDefault(command_name, null);
			MyChatCommand.MyCommandResult result;

			if (command == null) result = MyChatCommand.MyCommandResult.CommandNotFound(command_name);
			else if (command.RequiresAdmin && !MyAPIGateway.Session.IsUserAdmin(MyAPIGateway.Multiplayer.MyId)) result = MyChatCommand.MyCommandResult.InsufficientPermissions(command_name);
			else
			{
				try { result = command.Execute(arguments.Skip(2).ToList()); }
				catch (Exception err) { result = MyChatCommand.MyCommandResult.Error(command_name, err); }
			}

			string outcome = (result.Successfull) ? MyTexts.GetString("ChatCommandHandler_Success") : MyTexts.GetString("ChatCommandHandler_Fail");
			if (result.ResultMessage == null) MyAPIGateway.Utilities.ShowMessage(MyJumpGateModSession.DISPLAYNAME, $"\"{result.CommandName}\" ({outcome})");
			else MyAPIGateway.Utilities.ShowMessage(MyJumpGateModSession.DISPLAYNAME, $"\"{result.CommandName}\" ({outcome}) > {result.ResultMessage}");
		}

		public static void Init()
		{
			if (MyChatCommandHandler.Initialized) return;
			MyChatCommandHandler.Initialized = true;
			MyAPIGateway.Utilities.MessageEnteredSender += MyChatCommandHandler.OnChatCommand;
		}

		public static void Close()
		{
			if (!MyChatCommandHandler.Initialized) return;
			MyChatCommandHandler.Initialized = false;
			MyAPIGateway.Utilities.MessageEnteredSender -= MyChatCommandHandler.OnChatCommand;
		}
	}

	internal abstract class MyChatCommand
	{
		public struct MyCommandResult
		{
			public readonly bool Successfull;
			public readonly string CommandName;
			public readonly string ResultMessage;

			/// <summary>
			/// Creates a "Success" command result
			/// </summary>
			/// <param name="name">The command name</param>
			/// <param name="message">The optional message</param>
			/// <returns></returns>
			public static MyCommandResult Success(string name, string message = null)
			{
				return new MyCommandResult(true, name, message);
			}

			/// <summary>
			/// Creates a "Failure" command result
			/// </summary>
			/// <param name="name">The command name</param>
			/// <param name="message">The optional message</param>
			/// <returns></returns>
			public static MyCommandResult Failure(string name, string message = null)
			{
				return new MyCommandResult(false, name, message);
			}

			/// <summary>
			/// Creates an "Error" command result
			/// </summary>
			/// <param name="name">The command name</param>
			/// <param name="message">The optional message</param>
			/// <returns></returns>
			public static MyCommandResult Error(string name, Exception error)
			{
				return new MyCommandResult(false, name, MyTexts.GetString("ChatCommandHandler_ErrorUnknownException").Replace("{%0}", error.GetType().Name).Replace("{%1}", error.Message));
			}

			/// <summary>
			/// Creates a "CommandNotFound" command result
			/// </summary>
			/// <param name="name">The command name</param>
			/// <returns></returns>
			public static MyCommandResult CommandNotFound(string name)
			{
				return new MyCommandResult(false, name, MyTexts.GetString("ChatCommandHandler_ErrorCommandNotFound"));
			}

			/// <summary>
			/// Creates an "InvalidNumberArguments" command result
			/// </summary>
			/// <param name="name">The command name</param>
			/// <param name="min_arguments">The minimum number of accepted arguments</param>
			/// <param name="max_arguments">The maximum number of accepted arguments</param>
			/// <param name="count">The number of supplied arguments</param>
			/// <returns></returns>
			public static MyCommandResult InvalidNumberArguments(string name, int min_arguments, int max_arguments, int count)
			{
				string message;
				if (min_arguments == max_arguments) message = MyTexts.GetString("ChatCommandHandler_ErrorInvalidNumberArguments1").Replace("{%0}", min_arguments.ToString()).Replace("{%1}", count.ToString());
				else message = MyTexts.GetString("ChatCommandHandler_ErrorInvalidNumberArguments2").Replace("{%0}", min_arguments.ToString()).Replace("{%1}", max_arguments.ToString()).Replace("{%2}", count.ToString());
				return new MyCommandResult(false, name, message);
			}

			/// <summary>
			/// Creates an "InsufficientPermissions" command result
			/// </summary>
			/// <param name="name">The command name</param>
			/// <returns></returns>
			public static MyCommandResult InsufficientPermissions(string name)
			{
				return new MyCommandResult(false, name, MyTexts.GetString("ChatCommandHandler_ErrorInsufficientPermissions"));
			}

			private MyCommandResult(bool success, string name, string message)
			{
				this.Successfull = success;
				this.CommandName = name;
				this.ResultMessage = message;
			}
		}

		/// <summary>
		/// Whether this command requires admin priviledges
		/// </summary>
		public abstract bool RequiresAdmin { get; }

		/// <summary>
		/// The name of this command
		/// </summary>
		public abstract string CommandName { get; }

		/// <summary>
		/// This command's description
		/// </summary>
		public abstract string CommandDescription { get; }

		/// <summary>
		/// Called when executing this command
		/// </summary>
		/// <param name="arguments">The arguments supplied</param>
		/// <returns></returns>
		public abstract MyCommandResult Execute(List<string> arguments);
	}
}
