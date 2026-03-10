using IOTA.ModularJumpGates.Commands;
using IOTA.ModularJumpGates.ModConfiguration;
using IOTA.ModularJumpGates.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game.ModAPI;

namespace IOTA.ModularJumpGates.ChatCommands
{
	internal class MyReloadChatCommand : MyChatCommand
	{
		public override bool RequiresAdmin => true;

		public override bool RequiresNonNullCaller => false;

		public override string CommandName => MyTexts.GetString("ChatCommandHandler_ReloadCommand_Name");

		public override string CommandDescription => MyTexts.GetString("ChatCommandHandler_ReloadCommand_Description");

		public override string CommandHelp => $"{this.CommandName}";

		public override MyCommandResult Execute(IMyPlayer caller, List<string> arguments)
		{
			if (arguments.Count != 0) return MyCommandResult.InvalidNumberArguments(this, 0, 0, arguments.Count);
			else if (MyNetworkInterface.IsServerLike)
			{
				bool local_config_exists, global_config_exists;
				Exception local_load_err, global_load_err;
				MyModConfigurationV1 configuration = MyModConfigurationV1.Load(MyJumpGateModSession.Instance, out global_config_exists, out local_config_exists, out global_load_err, out local_load_err);
				if (global_config_exists) Logger.Log("Found global mod config file");
				else Logger.Warn($"Failed to locate global mod config file; SKIPPED");
				if (local_config_exists) Logger.Log("Found local mod config file");
				else Logger.Warn($"Failed to locate local mod config file; SKIPPED");
				if (global_load_err != null) Logger.Error($"Error loading global config file:\n  ...\n[ {global_load_err.GetType().Name} ]: {global_load_err.Message}\n{global_load_err.StackTrace}\n{global_load_err.InnerException}");
				if (local_load_err != null) Logger.Error($"Error loading local config file:\n  ...\n[ {local_load_err.GetType().Name} ]: {local_load_err.Message}\n{local_load_err.StackTrace}\n{local_load_err.InnerException}");
				if (!local_config_exists || !global_config_exists || local_load_err != null || global_load_err != null) return MyCommandResult.Error(this, local_load_err ?? global_load_err ?? new FileNotFoundException("One or more config files do not exist"));
				MyJumpGateModSession.Instance.UpdateConfiguration(configuration);
				return MyCommandResult.Success(this, MyTexts.GetString("ChatCommandHandler_ReloadCommand_OnSuccess"));
			}
			else
			{
				MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet {
					PacketType = MyPacketTypeEnum.UPDATE_CONFIG,
					Broadcast = false,
					TargetID = 0,
				};

				packet.Send();
				return MyCommandResult.Success(this, MyTexts.GetString("ChatCommandHandler_ReloadCommand_OnMPSuccess"));
			}
		}
	}

	internal class MyDebugChatCommand : MyChatCommand
	{
		public override bool RequiresAdmin => false;

		public override bool RequiresNonNullCaller => true;

		public override string CommandName => MyTexts.GetString("ChatCommandHandler_DebugCommand_Name");

		public override string CommandDescription => MyTexts.GetString("ChatCommandHandler_DebugCommand_Description");

		public override string CommandHelp => $"{this.CommandName} [true|false]";

		public override MyCommandResult Execute(IMyPlayer caller, List<string> arguments)
		{
			if (arguments.Count > 1) return MyCommandResult.InvalidNumberArguments(this, 0, 1, arguments.Count);
			else
			{
				bool value = !MyJumpGateModSession.Instance.DebugMode;
				if (arguments.Count == 1 && !bool.TryParse(arguments[0], out value)) return MyCommandResult.Failure(this, $"Invalid boolean value \"{arguments[0]}\", expected either 'true' or 'false'");
				MyJumpGateModSession.Instance.DebugMode = value;
				return MyCommandResult.Success(this, MyTexts.GetString((value) ? "ChatCommandHandler_DebugCommand_OnEnableSuccess" : "ChatCommandHandler_DebugCommand_OnDisableSuccess"));
			}
		}
	}

	internal class MyHelpChatCommand : MyChatCommand
	{
		public override bool RequiresAdmin => false;

		public override bool RequiresNonNullCaller => false;

		public override string CommandName => MyTexts.GetString("ChatCommandHandler_HelpCommand_Name");

		public override string CommandDescription => MyTexts.GetString("ChatCommandHandler_HelpCommand_Description");

		public override string CommandHelp => $"{this.CommandName} [command [subcommands...]]";

		public override MyCommandResult Execute(IMyPlayer caller, List<string> arguments)
		{
			StringBuilder sb = new StringBuilder();
			if (arguments.Count > 0)
			{
				MyChatCommand command = MyChatCommandHandler.GetCommand(arguments[0]);
				if (command == null) return MyChatCommand.MyCommandResult.CommandNotFound(arguments[0]);

				for (int i = 1; i < arguments.Count; ++i)
				{
					MyChatCommand subcommand = command.GetSubCommand(arguments[i]);
					if (subcommand == null) return MyChatCommand.MyCommandResult.CommandNotFound($"{command.FullCommandName} > {arguments[i]}");
					command = subcommand;
				}

				string help = command.CommandHelp;
				List<MyChatCommand> subcommands = command.GetSubCommands().ToList();
				sb.Append($"\n - [{((command.RequiresAdmin) ? "*" : "")}{command.CommandName}] - {command.CommandDescription}\n");
				if (help != null && help.Length > 0) sb.Append($"    ... HELP: {help}\n");

				if (subcommands.Count > 0)
				{
					sb.Append($"    ... {subcommands.Count} SUBCOMMANDS:\n");

					foreach (MyChatCommand subcommand in subcommands)
					{
						string msg = $"        - [{((subcommand.RequiresAdmin) ? "*" : "")}{subcommand.CommandName}] - {subcommand.CommandDescription}";
						sb.AppendLine((msg.Length > 53) ? $"{msg.Substring(0, 50)}..." : msg);
					}
				}
			}
			else
			{
				sb.Append($" >>\n-=[ {MyTexts.GetString("ChatCommandHandler_CommandListing")} ]=-\n");
				foreach (MyChatCommand command in MyChatCommandHandler.GetCommands()) MyChatCommandHandler.ShowCommandHelp(sb, command, 0);
			}

			return MyCommandResult.Success(this, sb.ToString());
		}
	}
}
