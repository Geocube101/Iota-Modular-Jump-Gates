using IOTA.ModularJumpGates.Commands;
using IOTA.ModularJumpGates.Util;
using Sandbox.ModAPI;
using System.Collections.Generic;
using System.Linq;
using VRage;
using VRage.Game.ModAPI;
using VRageMath;

namespace IOTA.ModularJumpGates.ChatCommands
{
	internal class MyGateChatCommand : MyChatCommand
	{
		public override bool RequiresAdmin => false;

		public override bool RequiresNonNullCaller => false;

		public override bool AutoExecuteSubCommand => true;

		public override string CommandName => MyTexts.GetString("ChatCommandHandler_GateCommand_Name");

		public override string CommandDescription => MyTexts.GetString("ChatCommandHandler_GateCommand_Description");

		public override string CommandHelp => $"{this.CommandName} <subcommand> [args...]";

		public override bool Init()
		{
			this.AddSubCommand(new MyGateInfoChatCommand());
			this.AddSubCommand(new MyGateRescanChatCommand());
			this.AddSubCommand(new MyGateJumpChatCommand());
			this.AddSubCommand(new MyGateNoJumpChatCommand());
			base.Init();
			return true;
		}

		public override MyCommandResult Execute(IMyPlayer caller, List<string> arguments)
		{
			return (arguments.Count == 0) ? MyCommandResult.InvalidNumberArguments(this, 1, null, 0) : MyCommandResult.InvalidSubCommand(this, true);
		}
	}

	internal class MyGateInfoChatCommand : MyChatCommand
	{
		public override bool RequiresAdmin => false;

		public override bool RequiresNonNullCaller => true;

		public bool AwaitingResponse = false;

		public override string CommandName => MyTexts.GetString("ChatCommandHandler_GateInfoCommand_Name");

		public override string CommandDescription => MyTexts.GetString("ChatCommandHandler_GateInfoCommand_Description");

		public override string CommandHelp => $"{this.CommandName}";

		private void OnGateInfoPacket(MyNetworkInterface.Packet packet)
		{
			if (packet == null || packet.PacketType != MyPacketTypeEnum.GENERAL) return;
			string eid = packet.GenericEventID;

			if (eid == "gateinfo_request" && packet.PhaseFrame == 1)
			{
				JumpGateUUID uuid;
				packet.GeneralPayload(out uuid);
				MyJumpGate gate = MyJumpGateModSession.Instance.GetJumpGate(uuid);
				string response = null;
				if (gate == null || gate.Closed) response = MyTexts.GetString("ChatCommandHandler_GateInfoCommand_GateNotFound");
				else response = this.GenerateGateInfo(packet.SenderID, gate);
				packet = packet.Forward(packet.SenderID, false);
				packet.GeneralPayload("gateinfo_response", response);
				packet.Send();
			}
			else if (eid == "gateinfo_response" && packet.PhaseFrame == 2)
			{
				string response;
				packet.GeneralPayload<string>(out response);
				MyAPIGateway.Utilities.ShowMessage(MyJumpGateModSession.DISPLAYNAME, response);
			}
		}

		public override void Deinit()
		{
			base.Deinit();
			if (!(MyJumpGateModSession.Network?.Registered ?? false)) return;
			MyJumpGateModSession.Network.Off(MyPacketTypeEnum.GENERAL, this.OnGateInfoPacket);
		}

		public override bool Init()
		{
			base.Init();
			if (!(MyJumpGateModSession.Network?.Registered ?? false)) return true;
			MyJumpGateModSession.Network.On(MyPacketTypeEnum.GENERAL, this.OnGateInfoPacket);
			return true;
		}

		private string GenerateGateInfo(ulong caller, MyJumpGate gate)
		{
			if (gate == null) return "";
			bool admin = MyAPIGateway.Session.IsUserAdmin(caller);
			MyJumpGateControlObject control = gate.ControlObject;
			Vector3D? collider_position = gate.GetColliderPosition();
			return MyTexts.GetString("ChatCommandHandler_GateInfoCommand_GateInfo")
				.Replace("{%0}", gate.JumpGateGrid?.CubeGridID.ToString() ?? "N/A")
				.Replace("{%1}", gate.JumpGateGrid?.PrimaryCubeGridCustomName.ToString() ?? "N/A")
				.Replace("{%2}", gate.JumpGateID.ToString())
				.Replace("{%3}", gate.GetPrintableName())
				.Replace("{%4}", gate.CubeGridSize().ToString())
				.Replace("{%5}", gate.Status.ToString())
				.Replace("{%6}", Vector3D.Round(gate.JumpNodeVelocity, 2).ToString())
				.Replace("{%7}", MyJumpGateModSession.AutoconvertMetricUnits(gate.JumpNodeVelocity.Length(), "m/s", 4))
				.Replace("{%8}", (admin) ? Vector3D.Round(gate.WorldJumpNode, 2).ToString() : "//////")
				.Replace("{%9}", gate.JumpNodeRadius().ToString())
				.Replace("{%10}", gate.GetEffectiveJumpEllipse().Radii.X.ToString())
				.Replace("{%11}", gate.GetWorkingJumpGateDrives().Count().ToString())
				.Replace("{%12}", gate.GetJumpGateDrives().Count().ToString())
				.Replace("{%13}", gate.JumpSpaceColliderStatus().ToString())
				.Replace("{%14}", (admin) ? ((collider_position == null) ? "N/A" : Vector3D.Round(collider_position.Value, 2).ToString()) : "//////")
				.Replace("{%15}", control?.BlockID.ToString() ?? "N/A")
				.Replace("{%16}", (control == null) ? "N/A" : (!control.IsController).ToString())
				.Replace("{%17}", (control == null) ? "N/A" : (control.JumpGateGrid != gate.JumpGateGrid).ToString());
		}

		public override MyCommandResult Execute(IMyPlayer caller, List<string> arguments)
		{
			if (arguments.Count != 0) return MyCommandResult.InvalidNumberArguments(this, 0, 0, arguments.Count);
			double scan_distance = 1500 * 1500;
			Vector3D position = caller.GetPosition();
			MyJumpGate closest = MyJumpGateModSession.Instance.GetAllJumpGates().Where((gate) => gate != null && !gate.Closed && Vector3D.DistanceSquared(position, gate.WorldJumpNode) <= scan_distance).OrderBy((gate) => Vector3D.DistanceSquared(position, gate.WorldJumpNode)).FirstOrDefault();
			if (closest == null) return MyCommandResult.Failure(this, MyTexts.GetString("ChatCommandHandler_GateInfoCommand_GateNotFound"));

			if (MyNetworkInterface.IsServerLike)
			{
				MyAPIGateway.Utilities.ShowMessage(MyJumpGateModSession.DISPLAYNAME, this.GenerateGateInfo(MyAPIGateway.Multiplayer.MyId, closest));
				return MyCommandResult.Success(this);
			}
			else
			{
				MyNetworkInterface.Packet packet = MyJumpGateModSession.Network.CreateGeneralPacket("gateinfo_request", JumpGateUUID.FromJumpGate(closest));
				packet.Send();
				return MyCommandResult.Success(this, MyTexts.GetString("ChatCommandHandler_GateInfoCommand_OnMPSuccess"));
			}
		}
	}

	internal class MyGateRescanChatCommand : MyChatCommand
	{
		public override bool RequiresAdmin => true;

		public override bool RequiresNonNullCaller => false;

		public override string CommandName => MyTexts.GetString("ChatCommandHandler_GateRescanCommand_Name");

		public override string CommandDescription => MyTexts.GetString("ChatCommandHandler_GateRescanCommand_Description");

		public override string CommandHelp => $"{this.CommandName} [all|nearest]";

		public override MyCommandResult Execute(IMyPlayer caller, List<string> arguments)
		{
			string target_type = arguments.FirstOrDefault() ?? "all";
			
			if (target_type == "all")
			{
				int count = 0;

				foreach (MyJumpGate gate in MyJumpGateModSession.Instance.GetAllJumpGates())
				{
					if (gate == null || gate.Closed) continue;
					gate.SetColliderDirtyMP();
					++count;
				}

				return MyCommandResult.Success(this, (MyNetworkInterface.IsStandaloneMultiplayerClient) ? MyTexts.GetString("ChatCommandHandler_GateRescanCommand_OnMPSuccess") : MyTexts.GetString("ChatCommandHandler_GateRescanCommand_OnSuccess").Replace("{%0}", count.ToString()));
			}
			else if (target_type == "nearest")
			{
				if (caller == null) return MyCommandResult.NullCaller(this);
				double scan_distance = 1500 * 1500;
				Vector3D position = caller.GetPosition();
				MyJumpGate closest = MyJumpGateModSession.Instance.GetAllJumpGates().Where((gate) => gate != null && !gate.Closed && Vector3D.DistanceSquared(position, gate.WorldJumpNode) <= scan_distance).OrderBy((gate) => Vector3D.DistanceSquared(position, gate.WorldJumpNode)).FirstOrDefault();
				if (closest == null) return MyCommandResult.Failure(this, MyTexts.GetString("ChatCommandHandler_GateInfoCommand_GateNotFound"));
				closest.SetColliderDirtyMP();
				return MyCommandResult.Success(this, (MyNetworkInterface.IsStandaloneMultiplayerClient) ? MyTexts.GetString("ChatCommandHandler_GateRescanCommand_OnMPSuccess") : MyTexts.GetString("ChatCommandHandler_GateRescanCommand_OnSuccess").Replace("{%0}", "1"));
			}
			else
			{
				return MyCommandResult.Failure(this, MyTexts.GetString("ChatCommandHandler_GateRescanCommand_InvalidTarget").Replace("{%0}", target_type));
			}
		}
	}

	internal class MyGateJumpChatCommand : MyChatCommand
	{
		public override bool RequiresAdmin => true;

		public override bool RequiresNonNullCaller => true;

		public override string CommandName => MyTexts.GetString("ChatCommandHandler_GateJumpCommand_Name");

		public override string CommandDescription => MyTexts.GetString("ChatCommandHandler_GateJumpCommand_Description");

		public override string CommandHelp => $"{this.CommandName}";

		public override MyCommandResult Execute(IMyPlayer caller, List<string> arguments)
		{
			if (arguments.Count != 0) return MyCommandResult.InvalidNumberArguments(this, 0, 0, arguments.Count);
			double scan_distance = 1500 * 1500;
			Vector3D position = caller.GetPosition();
			MyJumpGate closest = MyJumpGateModSession.Instance.GetAllJumpGates().Where((gate) => gate != null && !gate.Closed && Vector3D.DistanceSquared(position, gate.WorldJumpNode) <= scan_distance).OrderBy((gate) => Vector3D.DistanceSquared(position, gate.WorldJumpNode)).FirstOrDefault();
			if (closest == null) return MyCommandResult.Failure(this, MyTexts.GetString("ChatCommandHandler_GateInfoCommand_GateNotFound"));
			closest.Jump(closest.Controller?.BlockSettings);
			return MyCommandResult.Success(this, (MyNetworkInterface.IsStandaloneMultiplayerClient) ? MyTexts.GetString("ChatCommandHandler_GateJumpCommand_OnMPSuccess") : MyTexts.GetString("ChatCommandHandler_GateJumpCommand_OnSuccess").Replace("{%0}", closest.GetPrintableName()));
		}
	}

	internal class MyGateNoJumpChatCommand : MyChatCommand
	{
		public override bool RequiresAdmin => true;

		public override bool RequiresNonNullCaller => true;

		public override string CommandName => MyTexts.GetString("ChatCommandHandler_GateNoJumpCommand_Name");

		public override string CommandDescription => MyTexts.GetString("ChatCommandHandler_GateNoJumpCommand_Description");

		public override string CommandHelp => $"{this.CommandName}";

		public override MyCommandResult Execute(IMyPlayer caller, List<string> arguments)
		{
			if (arguments.Count != 0) return MyCommandResult.InvalidNumberArguments(this, 0, 0, arguments.Count);
			double scan_distance = 1500 * 1500;
			Vector3D position = caller.GetPosition();
			MyJumpGate closest = MyJumpGateModSession.Instance.GetAllJumpGates().Where((gate) => gate != null && !gate.Closed && Vector3D.DistanceSquared(position, gate.WorldJumpNode) <= scan_distance).OrderBy((gate) => Vector3D.DistanceSquared(position, gate.WorldJumpNode)).FirstOrDefault();
			if (closest == null) return MyCommandResult.Failure(this, MyTexts.GetString("ChatCommandHandler_GateInfoCommand_GateNotFound"));
			closest.CancelJump();
			return MyCommandResult.Success(this, (MyNetworkInterface.IsStandaloneMultiplayerClient) ? MyTexts.GetString("ChatCommandHandler_GateNoJumpCommand_OnMPSuccess") : MyTexts.GetString("ChatCommandHandler_GateNoJumpCommand_OnSuccess").Replace("{%0}", closest.GetPrintableName()));
		}
	}
}
