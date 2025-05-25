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
}
