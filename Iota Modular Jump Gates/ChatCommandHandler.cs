using IOTA.ModularJumpGates.ChatCommands;
using IOTA.ModularJumpGates.Util;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game.ModAPI;

namespace IOTA.ModularJumpGates.Commands
{
	internal static class MyChatCommandHandler
	{
		private static Dictionary<string, MyChatCommand> Commands = new Dictionary<string, MyChatCommand>() {};

		private static List<MyChatCommand> PreLoadCommands = new List<MyChatCommand>() {
			new MyReloadChatCommand(),
			new MyDebugChatCommand(),
			new MyHelpChatCommand(),
			new MyGateChatCommand(),
			new MyConstructChatCommand(),
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
				if (char.IsWhiteSpace(c) && split)
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
				foreach (KeyValuePair<string, MyChatCommand> pair in MyChatCommandHandler.Commands) MyChatCommandHandler.ShowCommandHelp(sb, pair.Value, 0);
				MyAPIGateway.Utilities.ShowMessage(MyJumpGateModSession.DISPLAYNAME, sb.ToString());
				return;
			}

			string command_name = arguments[1];
			MyChatCommand command = MyChatCommandHandler.Commands.GetValueOrDefault(command_name, null);
			MyChatCommand.MyCommandResult result;
			List<IMyPlayer> players = new List<IMyPlayer>();
			MyAPIGateway.Players.GetPlayers(players, (player) => player.SteamUserId == sender);
			IMyPlayer caller = players.FirstOrDefault();

			if (command == null) result = MyChatCommand.MyCommandResult.CommandNotFound(command_name);
			else if (command.RequiresAdmin && !MyAPIGateway.Session.IsUserAdmin(MyAPIGateway.Multiplayer.MyId)) result = MyChatCommand.MyCommandResult.InsufficientPermissions(command);
			else if (command.RequiresNonNullCaller && caller == null) result = MyChatCommand.MyCommandResult.NullCaller(command);
			else
			{
				int argument_index = 2;

				while (command.AutoExecuteSubCommand && argument_index < arguments.Count && command.HasSubCommand(arguments[argument_index]))
				{
					string subcommand_name = arguments[argument_index++];
					MyChatCommand subcommand = command.GetSubCommand(subcommand_name);
					if (subcommand == null) result = MyChatCommand.MyCommandResult.CommandNotFound($"{command.FullCommandName} > {subcommand_name}");
					else if (subcommand.RequiresAdmin && !MyAPIGateway.Session.IsUserAdmin(MyAPIGateway.Multiplayer.MyId)) result = MyChatCommand.MyCommandResult.InsufficientPermissions(subcommand);
					else if (subcommand.RequiresNonNullCaller && caller == null) result = MyChatCommand.MyCommandResult.NullCaller(subcommand);
					command = subcommand;
				}

				try { result = command.Execute(caller, arguments.Skip(argument_index).ToList()); }
				catch (Exception err)
				{
					result = MyChatCommand.MyCommandResult.Error(command, err);
					Logger.Error($"Error during command callback: \"{command.FullCommandName}\"\n  ...\n[ {err.GetType().Name} ]: {err.Message}\n{err.StackTrace}\n{err.InnerException}");
				}
			}

			string outcome = (result.Successfull) ? MyTexts.GetString("ChatCommandHandler_Success") : MyTexts.GetString("ChatCommandHandler_Fail");
			if (result.ResultMessage == null) MyAPIGateway.Utilities.ShowMessage(MyJumpGateModSession.DISPLAYNAME, $"\"{result.CommandName}\" ({outcome})");
			else MyAPIGateway.Utilities.ShowMessage(MyJumpGateModSession.DISPLAYNAME, $"\"{result.CommandName}\" ({outcome}) > {result.ResultMessage}");
		}

		public static void ShowCommandHelp(StringBuilder sb, MyChatCommand command, byte indent)
		{
			if (command == null || sb == null) return;
			string msg = $"{string.Join("", Enumerable.Repeat(" -", indent))} - [{((command.RequiresAdmin) ? "*" : "")}{command.CommandName}] - {command.CommandDescription}";
			sb.AppendLine((msg.Length > 53) ? $"{msg.Substring(0, 50)}..." : msg);
			foreach (MyChatCommand subcommand in command.GetSubCommands()) MyChatCommandHandler.ShowCommandHelp(sb, subcommand, (byte)(indent + 1));
		}

		public static void Init()
		{
			if (MyChatCommandHandler.Initialized) return;
			MyChatCommandHandler.Initialized = true;
			MyAPIGateway.Utilities.MessageEnteredSender += MyChatCommandHandler.OnChatCommand;

			foreach (MyChatCommand command in MyChatCommandHandler.PreLoadCommands)
			{
				if (command == null || !command.Init()) continue;
				string name = command.CommandName;
				if (name != null && name.Length > 0 && !MyChatCommandHandler.Commands.ContainsKey(name)) MyChatCommandHandler.Commands[name] = command;
			}

			MyChatCommandHandler.PreLoadCommands.Clear();
			MyChatCommandHandler.PreLoadCommands = null;
		}

		public static void Close()
		{
			if (!MyChatCommandHandler.Initialized) return;
			MyChatCommandHandler.Initialized = false;
			MyAPIGateway.Utilities.MessageEnteredSender -= MyChatCommandHandler.OnChatCommand;
			foreach (KeyValuePair<string, MyChatCommand> pair in MyChatCommandHandler.Commands) pair.Value.Deinit();
			MyChatCommandHandler.Commands.Clear();
			MyChatCommandHandler.Commands = null;
		}

		/// <summary>
		/// Gets the command with the given name, or null if not found
		/// </summary>
		/// <param name="name">The command name</param>
		/// <returns>The command or null</returns>
		public static MyChatCommand GetCommand(string name)
		{
			return MyChatCommandHandler.Commands.GetValueOrDefault(name, null);
		}

		/// <summary>
		/// Gets all commands
		/// </summary>
		/// <returns></returns>
		public static IEnumerable<MyChatCommand> GetCommands()
		{
			return MyChatCommandHandler.Commands.Select((pair) => pair.Value);
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
			/// <param name="command">The calling command</param>
			/// <param name="message">The optional message</param>
			/// <returns></returns>
			public static MyCommandResult Success(MyChatCommand command, string message = null)
			{
				return new MyCommandResult(true, command.FullCommandName, message);
			}

			/// <summary>
			/// Creates a "Failure" command result
			/// </summary>
			/// <param name="command">The calling command</param>
			/// <param name="message">The optional message</param>
			/// <returns></returns>
			public static MyCommandResult Failure(MyChatCommand command, string message = null)
			{
				return new MyCommandResult(false, command.FullCommandName, message);
			}

			/// <summary>
			/// Creates an "Error" command result
			/// </summary>
			/// <param name="command">The calling command</param>
			/// <param name="message">The optional message</param>
			/// <returns></returns>
			public static MyCommandResult Error(MyChatCommand command, Exception error)
			{
				return new MyCommandResult(false, command.FullCommandName, MyTexts.GetString("ChatCommandHandler_ErrorUnknownException").Replace("{%0}", error.GetType().Name).Replace("{%1}", error.Message));
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
			/// <param name="command">The calling command</param>
			/// <param name="min_arguments">The minimum number of accepted arguments</param>
			/// <param name="max_arguments">The maximum number of accepted arguments</param>
			/// <param name="count">The number of supplied arguments</param>
			/// <returns></returns>
			public static MyCommandResult InvalidNumberArguments(MyChatCommand command, int? min_arguments, int? max_arguments, int count)
			{
				string message;
				if (min_arguments == null && max_arguments == null) throw new InvalidOperationException("Illegal error thrown");
				else if (max_arguments == null) message = MyTexts.GetString("ChatCommandHandler_ErrorInvalidNumberArguments0").Replace("{%0}", min_arguments.ToString()).Replace("{%1}", count.ToString());
				else if (min_arguments == null) message = MyTexts.GetString("ChatCommandHandler_ErrorInvalidNumberArguments1").Replace("{%0}", max_arguments.ToString()).Replace("{%1}", count.ToString());
				else if (min_arguments == max_arguments) message = MyTexts.GetString("ChatCommandHandler_ErrorInvalidNumberArguments2").Replace("{%0}", min_arguments.ToString()).Replace("{%1}", count.ToString());
				else message = MyTexts.GetString("ChatCommandHandler_ErrorInvalidNumberArguments3").Replace("{%0}", min_arguments.ToString()).Replace("{%1}", max_arguments.ToString()).Replace("{%2}", count.ToString());
				return new MyCommandResult(false, command.FullCommandName, message);
			}

			/// <summary>
			/// Creates an "InsufficientPermissions" command result
			/// </summary>
			/// <param name="command">The calling command</param>
			/// <returns></returns>
			public static MyCommandResult InsufficientPermissions(MyChatCommand command)
			{
				return new MyCommandResult(false, command.FullCommandName, MyTexts.GetString("ChatCommandHandler_ErrorInsufficientPermissions"));
			}

			/// <summary>
			/// Creates an "NullCaller" command result
			/// </summary>
			/// <param name="command">The calling command</param>
			/// <returns></returns>
			public static MyCommandResult NullCaller(MyChatCommand command)
			{
				return new MyCommandResult(false, command.FullCommandName, MyTexts.GetString("ChatCommandHandler_ErrorNullCaller"));
			}

			/// <summary>
			/// Creates an "InvalidSubCommand" command result
			/// </summary>
			/// <param name="command">The calling command</param>
			/// <returns></returns>
			public static MyCommandResult InvalidSubCommand(MyChatCommand command, bool list_commands = true)
			{
				return new MyCommandResult(false, command.FullCommandName, (list_commands) ? MyTexts.GetString("ChatCommandHandler_ErrorInvalidSubCommand1").Replace("{%0}", string.Join("\n", command.GetSubCommands().Select((subcommand) => $" > \"{subcommand.CommandName}\""))) : MyTexts.GetString("ChatCommandHandler_ErrorInvalidSubCommand0"));
			}

			private MyCommandResult(bool success, string name, string message)
			{
				this.Successfull = success;
				this.CommandName = name;
				this.ResultMessage = message;
			}
		}

		private readonly Dictionary<string, MyChatCommand> SubCommands = new Dictionary<string, MyChatCommand>();

		/// <summary>
		/// Whether this command requires admin priviledges
		/// </summary>
		public abstract bool RequiresAdmin { get; }

		/// <summary>
		/// Whether the caller must be non-null (i.e. not the server console)
		/// </summary>
		public abstract bool RequiresNonNullCaller { get; }

		/// <summary>
		/// Whether command handler should automatically consume arguments to execute sub-commands
		/// </summary>
		public virtual bool AutoExecuteSubCommand => false;

		/// <summary>
		/// The name of this command
		/// </summary>
		public abstract string CommandName { get; }

		/// <summary>
		/// This command's description
		/// </summary>
		public abstract string CommandDescription { get; }

		/// <summary>
		/// This command's help string
		/// </summary>
		public virtual string CommandHelp => null;

		/// <summary>
		/// The name of this command, prefixed by parent commands
		/// </summary>
		public string FullCommandName => string.Join(" > ", this.ParentStack().Reverse().Select((cmd) => cmd.CommandName));

		/// <summary>
		/// This command's parent command, or null if this is a root command
		/// </summary>
		public MyChatCommand Parent { get; private set; } = null;

		public void AddSubCommand(MyChatCommand command)
		{
			string name = command?.CommandName;
			if (name == null || name.Length == 0 || command == this || this.SubCommands.ContainsKey(name) || this.IsInStack(command)) throw new ArgumentException("Invalid or duplicate sub-command name");
			this.SubCommands[name] = command;
			command.Parent = this;
		}

		/// <summary>
		/// Called when deinitializing this command<br />
		/// Called on session unload
		/// </summary>
		public virtual void Deinit()
		{
			foreach (KeyValuePair<string, MyChatCommand> pair in this.SubCommands) pair.Value.Deinit();
			this.SubCommands.Clear();
		}

		/// <summary>
		/// Called when initializing this command, return false to not register this command<br />
		/// Called on session before-start<br />
		/// Call to init sub-commands <b>AFTER</b> your init code
		/// </summary>
		/// <returns>Whether to register command</returns>
		public virtual bool Init()
		{
			List<string> closed = new List<string>();
			foreach (KeyValuePair<string, MyChatCommand> pair in this.SubCommands) if (!pair.Value.Init()) closed.Add(pair.Key);
			foreach (string name in closed) this.SubCommands.Remove(name);
			return true;
		}

		/// <summary>
		/// Whether this command has a sub-command with the given name
		/// </summary>
		/// <param name="name">The sub-command name</param>
		/// <returns></returns>
		public bool HasSubCommand(string name)
		{
			return this.SubCommands.ContainsKey(name);
		}

		/// <summary>
		/// Whether the given command is in this command's parent stack
		/// </summary>
		/// <param name="command">The sub-command</param>
		/// <returns></returns>
		public bool IsInStack(MyChatCommand command)
		{
			MyChatCommand src = this.Parent;

			while (src != null)
			{
				if (src == command) return true;
				src = src.Parent;
			}

			return false;
		}

		/// <summary>
		/// Whether a command with the given name is in this command's parent stack
		/// </summary>
		/// <param name="command">The sub-command name</param>
		/// <returns></returns>
		public bool IsInStack(string command)
		{
			MyChatCommand src = this.Parent;

			while (src != null)
			{
				if (src.CommandName == command) return true;
				src = src.Parent;
			}

			return false;
		}

		/// <summary>
		/// Called when executing this command
		/// </summary>
		/// <param name="arguments">The arguments supplied</param>
		/// <returns></returns>
		public abstract MyCommandResult Execute(IMyPlayer caller, List<string> arguments);

		/// <summary>
		/// Gets the sub-command with the given name, or null if not found
		/// </summary>
		/// <param name="name">The sub-command name</param>
		/// <returns>The sub-command or null</returns>
		public MyChatCommand GetSubCommand(string name)
		{
			return this.SubCommands.GetValueOrDefault(name, null);
		}

		/// <summary>
		/// Gets the stack of parent commands, starting with this command
		/// </summary>
		/// <returns>The parent stack</returns>
		public IEnumerable<MyChatCommand> ParentStack()
		{
			MyChatCommand command = this;

			while (command != null)
			{
				yield return command;
				command = command.Parent;
			}
		}

		/// <summary>
		/// Gets all sub-commands of this command
		/// </summary>
		/// <returns></returns>
		public IEnumerable<MyChatCommand> GetSubCommands()
		{
			return this.SubCommands.Select((pair) => pair.Value);
		}
	}
}
