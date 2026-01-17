using IOTA.ModularJumpGates.Commands;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage;
using VRage.Game.ModAPI;

namespace IOTA.ModularJumpGates.ChatCommands
{
	internal class MyLogFileChatCommand : MyChatCommand
	{
		public override bool RequiresAdmin => false;

		public override bool RequiresNonNullCaller => false;

		public override bool AutoExecuteSubCommand => true;

		public override string CommandName => MyTexts.GetString("ChatCommandHandler_LogFileCommand_Name");

		public override string CommandDescription => MyTexts.GetString("ChatCommandHandler_LogFileCommand_Description");

		public override string CommandHelp => $"{this.CommandName} <subcommand> [args...]";

		public override bool Init()
		{
			this.AddSubCommand(new MyLogFileCountChatCommand());
			this.AddSubCommand(new MyLogFileDeleteChatCommand());
			base.Init();
			return true;
		}

		public override MyCommandResult Execute(IMyPlayer caller, List<string> arguments)
		{
			return (arguments.Count == 0) ? MyCommandResult.InvalidNumberArguments(this, 1, null, 0) : MyCommandResult.InvalidSubCommand(this, true);
		}
	}

	internal class MyLogFileCountChatCommand : MyChatCommand
	{
		public override bool RequiresAdmin => false;

		public override bool RequiresNonNullCaller => false;

		public override string CommandName => MyTexts.GetString("ChatCommandHandler_LogFileCountCommand_Name");

		public override string CommandDescription => MyTexts.GetString("ChatCommandHandler_LogFileCountCommand_Description");

		public override string CommandHelp => $"{this.CommandName} [true|false]";

		private void OnLogFileCountPacket(MyNetworkInterface.Packet packet)
		{
			if (packet == null || packet.PacketType != MyPacketTypeEnum.GENERAL) return;
			string eid = packet.GenericEventID;

			if (eid == "logfilecount_request" && packet.PhaseFrame == 1)
			{
				long id;
				packet.GeneralPayload(out id);
				MyJumpGateConstruct construct = MyJumpGateModSession.Instance.GetJumpGateGrid(id);
				packet = packet.Forward(packet.SenderID, false);
				packet.GeneralPayload("logfilecount_response", MyJumpGateModSession.Instance.ModSpecificLogFileCount);
				packet.Send();
			}
			else if (eid == "logfilecount_response" && packet.PhaseFrame == 2)
			{
				int response;
				packet.GeneralPayload(out response);
				MyAPIGateway.Utilities.ShowMessage(MyJumpGateModSession.DISPLAYNAME, MyTexts.GetString("ChatCommandHandler_LogFileCountCommand_OnSuccess").Replace("{%0}", response.ToString()));
			}
		}

		public override void Deinit()
		{
			base.Deinit();
			if (!(MyJumpGateModSession.Network?.Registered ?? false)) return;
			MyJumpGateModSession.Network.Off(MyPacketTypeEnum.GENERAL, this.OnLogFileCountPacket);
		}

		public override bool Init()
		{
			base.Init();
			if (!(MyJumpGateModSession.Network?.Registered ?? false)) return true;
			MyJumpGateModSession.Network.On(MyPacketTypeEnum.GENERAL, this.OnLogFileCountPacket);
			return true;
		}

		public override MyCommandResult Execute(IMyPlayer caller, List<string> arguments)
		{
			if (arguments.Count > 1) return MyCommandResult.InvalidNumberArguments(this, 0, 1, arguments.Count);
			bool is_admin = MyAPIGateway.Session.IsUserAdmin(caller.SteamUserId);
			bool do_server = false;
			if (arguments.Count > 0 && !bool.TryParse(arguments[0], out do_server)) return MyCommandResult.InvalidCommandArgument(this, 0, arguments[0], "true", "false");

			if (do_server && is_admin && MyJumpGateModSession.Network.Registered)
			{
				MyNetworkInterface.Packet packet = MyJumpGateModSession.Network.CreateGeneralPacket("logfilecount_request");
				packet.Send();
				return MyCommandResult.Success(this, MyTexts.GetString("ChatCommandHandler_LogFileCountCommand_OnMPSuccess"));
			}
			else if (do_server && MyJumpGateModSession.Network.Registered)
			{
				return MyCommandResult.InsufficientPermissions(this);
			}
			else
			{
				return MyCommandResult.Success(this, MyTexts.GetString("ChatCommandHandler_LogFileCountCommand_OnSuccess").Replace("{%0}", MyJumpGateModSession.Instance.ModSpecificLogFileCount.ToString()));
			}
		}
	}

	internal class MyLogFileDeleteChatCommand : MyChatCommand
	{
		public override bool RequiresAdmin => false;

		public override bool RequiresNonNullCaller => false;

		public override string CommandName => MyTexts.GetString("ChatCommandHandler_LogFileDeleteCommand_Name");

		public override string CommandDescription => MyTexts.GetString("ChatCommandHandler_LogFileDeleteCommand_Description");

		public override string CommandHelp => $"{this.CommandName} [true|false] [keep_count]";

		private void OnLogFileDeletePacket(MyNetworkInterface.Packet packet)
		{
			if (packet == null || packet.PacketType != MyPacketTypeEnum.GENERAL) return;
			string eid = packet.GenericEventID;

			if (eid == "logfiledelete_request" && packet.PhaseFrame == 1)
			{
				uint count;
				packet.GeneralPayload(out count);
				MyJumpGateModSession.Instance.PurgeStoredLogFiles(count);
				packet = packet.Forward(packet.SenderID, false);
				packet.GeneralPayload("logfiledelete_response", MyJumpGateModSession.Instance.ModSpecificLogFileCount);
				packet.Send();
			}
			else if (eid == "logfiledelete_response" && packet.PhaseFrame == 2)
			{
				int response;
				packet.GeneralPayload(out response);
				MyAPIGateway.Utilities.ShowMessage(MyJumpGateModSession.DISPLAYNAME, MyTexts.GetString("ChatCommandHandler_LogFileDeleteCommand_OnSuccess"));
			}
		}

		public override void Deinit()
		{
			base.Deinit();
			if (!(MyJumpGateModSession.Network?.Registered ?? false)) return;
			MyJumpGateModSession.Network.Off(MyPacketTypeEnum.GENERAL, this.OnLogFileDeletePacket);
		}

		public override bool Init()
		{
			base.Init();
			if (!(MyJumpGateModSession.Network?.Registered ?? false)) return true;
			MyJumpGateModSession.Network.On(MyPacketTypeEnum.GENERAL, this.OnLogFileDeletePacket);
			return true;
		}

		public override MyCommandResult Execute(IMyPlayer caller, List<string> arguments)
		{
			if (arguments.Count > 2) return MyCommandResult.InvalidNumberArguments(this, 0, 2, arguments.Count);
			bool is_admin = MyAPIGateway.Session.IsUserAdmin(caller.SteamUserId);
			bool do_server = false;
			uint count = MyJumpGateModSession.Instance.MaxModSpecificLogFiles;
			if (arguments.Count > 0 && !bool.TryParse(arguments[0], out do_server)) return MyCommandResult.InvalidCommandArgument(this, 0, arguments[0], "true", "false");
			if (arguments.Count > 1 && !uint.TryParse(arguments[1], out count)) return MyCommandResult.InvalidCommandArgument(this, 0, arguments[0], "*UINT32");

			if (do_server && is_admin && MyJumpGateModSession.Network.Registered)
			{
				MyNetworkInterface.Packet packet = MyJumpGateModSession.Network.CreateGeneralPacket("logfiledelete_request", count);
				packet.Send();
				return MyCommandResult.Success(this, MyTexts.GetString("ChatCommandHandler_LogFileDeleteCommand_OnMPSuccess"));
			}
			else if (do_server && MyJumpGateModSession.Network.Registered)
			{
				return MyCommandResult.InsufficientPermissions(this);
			}
			else
			{
				MyJumpGateModSession.Instance.PurgeStoredLogFiles(count);
				return MyCommandResult.Success(this, MyTexts.GetString("ChatCommandHandler_LogFileDeleteCommand_OnSuccess"));
			}
		}
	}
}
