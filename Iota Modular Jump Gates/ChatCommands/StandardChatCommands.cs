using IOTA.ModularJumpGates.Commands;
using System.Collections.Generic;
using VRage;

namespace IOTA.ModularJumpGates.ChatCommands
{
	internal class MyReloadChatCommand : MyChatCommand
	{
		public override bool RequiresAdmin => true;

		public override string CommandName => MyTexts.GetString("ChatCommandHandler_ReloadCommand_Name");

		public override string CommandDescription => MyTexts.GetString("ChatCommandHandler_ReloadCommand_Description");

		public override MyCommandResult Execute(List<string> arguments)
		{
			if (arguments.Count != 0) return MyCommandResult.InvalidNumberArguments(this.CommandName, 0, 0, arguments.Count);
			else if (MyNetworkInterface.IsServerLike)
			{
				Configuration config = Configuration.Load();
				MyJumpGateModSession.Instance.ReloadConfigurations(config);
				return MyCommandResult.Success(this.CommandName, MyTexts.GetString("ChatCommandHandler_ReloadCommand_OnSuccess"));
			}
			else
			{
				MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet {
					PacketType = MyPacketTypeEnum.UPDATE_CONFIG,
					Broadcast = false,
					TargetID = 0,
				};

				packet.Send();
				return MyCommandResult.Success(this.CommandName, MyTexts.GetString("ChatCommandHandler_ReloadCommand_OnMPSuccess"));
			}
		}
	}

	internal class MyDebugChatCommand : MyChatCommand
	{
		public override bool RequiresAdmin => false;

		public override string CommandName => MyTexts.GetString("ChatCommandHandler_DebugCommand_Name");

		public override string CommandDescription => MyTexts.GetString("ChatCommandHandler_DebugCommand_Description");

		public override MyCommandResult Execute(List<string> arguments)
		{
			if (arguments.Count > 1) return MyCommandResult.InvalidNumberArguments(this.CommandName, 0, 1, arguments.Count);
			else
			{
				bool value = !MyJumpGateModSession.DebugMode;
				if (arguments.Count == 1 && !bool.TryParse(arguments[0], out value)) return MyCommandResult.Failure(this.CommandName, $"Invalid boolean value \"{arguments[0]}\", expected either 'true' or 'false'");
				MyJumpGateModSession.DebugMode = value;
				return MyCommandResult.Success(this.CommandName, MyTexts.GetString((value) ? "ChatCommandHandler_DebugCommand_OnEnableSuccess" : "ChatCommandHandler_DebugCommand_OnDisableSuccess"));
			}
		}
	}
}
