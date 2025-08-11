using IOTA.ModularJumpGates.Commands;
using Sandbox.ModAPI;
using System.Collections.Generic;
using System.Linq;
using VRage;
using VRage.Game.ModAPI;
using VRageMath;

namespace IOTA.ModularJumpGates.ChatCommands
{
	internal class MyConstructChatCommand : MyChatCommand
	{
		public override bool RequiresAdmin => false;

		public override bool RequiresNonNullCaller => false;

		public override bool AutoExecuteSubCommand => true;

		public override string CommandName => MyTexts.GetString("ChatCommandHandler_GridCommand_Name");

		public override string CommandDescription => MyTexts.GetString("ChatCommandHandler_GridCommand_Description");

		public override string CommandHelp => $"{this.CommandName} <subcommand> [args...]";

		public override bool Init()
		{
			this.AddSubCommand(new MyConstructInfoChatCommand());
			this.AddSubCommand(new MyConstructReconstructChatCommand());
			base.Init();
			return true;
		}

		public override MyCommandResult Execute(IMyPlayer caller, List<string> arguments)
		{
			return (arguments.Count == 0) ? MyCommandResult.InvalidNumberArguments(this, 1, null, 0) : MyCommandResult.InvalidSubCommand(this, true);
		}
	}

	internal class MyConstructInfoChatCommand : MyChatCommand
	{
		public override bool RequiresAdmin => false;

		public override bool RequiresNonNullCaller => true;

		public bool AwaitingResponse = false;

		public override string CommandName => MyTexts.GetString("ChatCommandHandler_GridInfoCommand_Name");

		public override string CommandDescription => MyTexts.GetString("ChatCommandHandler_GridInfoCommand_Description");

		public override string CommandHelp => $"{this.CommandName}";

		private void OnGridInfoPacket(MyNetworkInterface.Packet packet)
		{
			if (packet == null || packet.PacketType != MyPacketTypeEnum.GENERAL) return;
			string eid = packet.GenericEventID;

			if (eid == "gridinfo_request" && packet.PhaseFrame == 1)
			{
				long id;
				packet.GeneralPayload(out id);
				MyJumpGateConstruct construct = MyJumpGateModSession.Instance.GetJumpGateGrid(id);
				string response = null;
				if (construct == null || construct.Closed) response = MyTexts.GetString("ChatCommandHandler_GridInfoCommand_GridNotFound");
				else response = this.GenerateConstructInfo(packet.SenderID, construct);
				packet = packet.Forward(packet.SenderID, false);
				packet.GeneralPayload("gridinfo_response", response);
				packet.Send();
			}
			else if (eid == "gridinfo_response" && packet.PhaseFrame == 2)
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
			MyJumpGateModSession.Network.Off(MyPacketTypeEnum.GENERAL, this.OnGridInfoPacket);
		}

		public override bool Init()
		{
			base.Init();
			if (!(MyJumpGateModSession.Network?.Registered ?? false)) return true;
			MyJumpGateModSession.Network.On(MyPacketTypeEnum.GENERAL, this.OnGridInfoPacket);
			return true;
		}

		private string GenerateConstructInfo(ulong caller, MyJumpGateConstruct construct)
		{
			if (construct == null) return "";
			bool admin = MyAPIGateway.Session.IsUserAdmin(caller);
			int gridcount = 0;
			int staticcount = 0;

			foreach (IMyCubeGrid grid in construct.GetCubeGrids())
			{
				if (grid == null || grid.Closed) continue;
				++gridcount;
				if (grid.IsStatic) ++staticcount;
			}

			return MyTexts.GetString("ChatCommandHandler_GridInfoCommand_GridInfo")
				.Replace("{%0}", construct.CubeGridID.ToString() ?? "N/A")
				.Replace("{%1}", construct.PrimaryCubeGridCustomName ?? "N/A")
				.Replace("{%2}", construct.FullyInitialized.ToString())
				.Replace("{%3}", construct.FailedTickCount.ToString())
				.Replace("{%4}", construct.FailedReinitializationCount.ToString())
				.Replace("{%5}", construct.HasCommLink().ToString())
				.Replace("{%6}", construct.GetCommLinkedJumpGateGrids().Count().ToString())
				.Replace("{%7}", construct.GetBeaconsWithinReverseBroadcastSphere().Count().ToString())
				.Replace("{%8}", MyJumpGateModSession.AutoconvertMetricUnits(construct.ConstructMass() * 1000, "g", 4))
				.Replace("{%9}", (admin) ? Vector3D.Round(construct.ConstructVolumeCenter(), 2).ToString() : "//////")
				.Replace("{%10}", (admin) ? Vector3D.Round(construct.ConstructMassCenter(), 2).ToString() : "//////")
				.Replace("{%11}", (construct.GetCubeGridPhysics() != null).ToString())
				.Replace("{%12}", construct.GetAttachedJumpGateControllers().Count().ToString())
				.Replace("{%13}", construct.GetAttachedJumpGateDrives().Count().ToString())
				.Replace("{%14}", construct.GetAttachedJumpGateCapacitors().Count().ToString())
				.Replace("{%15}", construct.GetAttachedJumpGateRemoteAntennas().Count().ToString())
				.Replace("{%16}", construct.GetAttachedJumpGateServerAntennas().Count().ToString())
				.Replace("{%17}", construct.GetAttachedJumpGateRemoteLinks().Count().ToString())
				.Replace("{%18}", construct.GetAttachedLaserAntennas().Count().ToString())
				.Replace("{%19}", construct.GetAttachedRadioAntennas().Count().ToString())
				.Replace("{%20}", construct.GetAttachedBeacons().Count().ToString())
				.Replace("{%21}", construct.GetJumpGates().Count().ToString())
				.Replace("{%22}", gridcount.ToString())
				.Replace("{%23}", staticcount.ToString())
				.Replace("{%24}", gridcount.ToString());
		}

		public override MyCommandResult Execute(IMyPlayer caller, List<string> arguments)
		{
			if (arguments.Count != 0) return MyCommandResult.InvalidNumberArguments(this, 0, 0, arguments.Count);
			double scan_distance = 1500 * 1500;
			Vector3D position =	caller.GetPosition();
			MyJumpGateConstruct closest = MyJumpGateModSession.Instance.GetAllJumpGateGrids().Where((grid) => grid != null && !grid.Closed && Vector3D.DistanceSquared(position, grid.ConstructMassCenter()) <= scan_distance).OrderBy((grid) => Vector3D.DistanceSquared(position, grid.ConstructMassCenter())).FirstOrDefault();
			if (closest == null) return MyCommandResult.Failure(this, MyTexts.GetString("ChatCommandHandler_GridInfoCommand_GridNotFound"));

			if (MyNetworkInterface.IsServerLike)
			{
				MyAPIGateway.Utilities.ShowMessage(MyJumpGateModSession.DISPLAYNAME, this.GenerateConstructInfo(MyAPIGateway.Multiplayer.MyId, closest));
				return MyCommandResult.Success(this);
			}
			else
			{
				MyNetworkInterface.Packet packet = MyJumpGateModSession.Network.CreateGeneralPacket("gridinfo_request", closest.CubeGridID);
				packet.Send();
				return MyCommandResult.Success(this, MyTexts.GetString("ChatCommandHandler_GridInfoCommand_OnMPSuccess"));
			}
		}
	}

	internal class MyConstructReconstructChatCommand : MyChatCommand
	{
		public override bool RequiresAdmin => true;

		public override bool RequiresNonNullCaller => false;

		public override string CommandName => MyTexts.GetString("ChatCommandHandler_GridRebuildCommand_Name");

		public override string CommandDescription => MyTexts.GetString("ChatCommandHandler_GridRebuildCommand_Description");

		public override string CommandHelp => $"{this.CommandName} [all|nearest]";

		public override MyCommandResult Execute(IMyPlayer caller, List<string> arguments)
		{
			string target_type = arguments.FirstOrDefault() ?? "all";
			
			if (target_type == "all")
			{
				if (MyNetworkInterface.IsStandaloneMultiplayerClient)
				{
					MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
					{
						PacketType = MyPacketTypeEnum.MARK_GATES_DIRTY,
						TargetID = 0,
						Broadcast = false,
					};
					packet.Payload(-1L);
					packet.Send();
					return MyCommandResult.Success(this, MyTexts.GetString("ChatCommandHandler_GridRebuildCommand_OnMPSuccess"));
				}
				else
				{
					int count = 0;

					foreach (MyJumpGateConstruct construct in MyJumpGateModSession.Instance.GetAllJumpGateGrids())
					{
						if (construct == null || construct.Closed) continue;
						construct.MarkGatesForUpdate();
						++count;
					}

					return MyCommandResult.Success(this, MyTexts.GetString("ChatCommandHandler_GridRebuildCommand_OnSuccess").Replace("{%0}", count.ToString()));
				}
			}
			else if (target_type == "nearest")
			{
				if (caller == null) return MyCommandResult.NullCaller(this);
				double scan_distance = 1500 * 1500;
				Vector3D position = caller.GetPosition();
				MyJumpGateConstruct closest = MyJumpGateModSession.Instance.GetAllJumpGateGrids().Where((grid) => grid != null && !grid.Closed && Vector3D.DistanceSquared(position, grid.ConstructMassCenter()) <= scan_distance).OrderBy((grid) => Vector3D.DistanceSquared(position, grid.ConstructMassCenter())).FirstOrDefault();
				if (closest == null) return MyCommandResult.Failure(this, MyTexts.GetString("ChatCommandHandler_GridInfoCommand_GridNotFound"));

				if (MyNetworkInterface.IsStandaloneMultiplayerClient)
				{
					MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet {
						PacketType = MyPacketTypeEnum.MARK_GATES_DIRTY,
						TargetID = 0,
						Broadcast = false,
					};
					packet.Payload(closest.CubeGridID);
					packet.Send();
					return MyCommandResult.Success(this, MyTexts.GetString("ChatCommandHandler_GridRebuildCommand_OnMPSuccess"));
				}
				else
				{
					closest.MarkGatesForUpdate();
					return MyCommandResult.Success(this, MyTexts.GetString("ChatCommandHandler_GridRebuildCommand_OnSuccess").Replace("{%0}", "1"));
				}
			}
			else
			{
				return MyCommandResult.Failure(this, MyTexts.GetString("ChatCommandHandler_GateRescanCommand_InvalidTarget").Replace("{%0}", target_type));
			}
		}
	}
}
