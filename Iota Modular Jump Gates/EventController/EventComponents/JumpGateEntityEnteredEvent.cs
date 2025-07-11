﻿using IOTA.ModularJumpGates.EventController.ObjectBuilders;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Utils;

namespace IOTA.ModularJumpGates.EventController.EventComponents
{
	[MyComponentBuilder(typeof(MyObjectBuilder_EventJumpGateEntityEntered))]
	[MyComponentType(typeof(JumpGateEntityEnteredEvent))]
	[MyEntityDependencyType(typeof(IMyEventControllerBlock))]
	internal class JumpGateEntityEnteredEvent : MyJumpGateEventBase<bool>
	{
		private bool? TriggerIndex = null;
		private readonly List<MyJumpGate> ListeningJumpGates = new List<MyJumpGate>();

		public override bool IsThresholdUsed => false;
		public override bool IsConditionSelectionUsed => false;
		public override bool IsBlocksListUsed => false;
		public override bool IsJumpGateSelectionUsed => true;
		public override long UniqueSelectionId => 0x7FFFFFFFFFFFFFFC;
		public override MyStringId EventDisplayName => MyStringId.GetOrCompute(MyTexts.GetString("DisplayName_JumpGateEntityEnteredEvent"));
		public override string ComponentTypeDebugString => nameof(JumpGateEntityEnteredEvent);
		public override string YesNoToolbarYesDescription => MyTexts.GetString("DisplayName_JumpGateEntityEnteredEvent_YesDescription");
		public override string YesNoToolbarNoDescription => MyTexts.GetString("DisplayName_JumpGateEntityEnteredEvent_NoDescription");

		private void OnEntityCollision(MyJumpGate caller, MyEntity entity, bool is_entering)
		{
			if (!this.IsListeningToJumpGate(caller))
			{
				caller.EntityEnterered -= this.OnEntityCollision;
				return;
			}
			
			this.TriggerIndex = is_entering;
		}

		protected override void Update()
		{
			base.Update();

			if (this.TriggerIndex != null)
			{
				this.TriggerAction((this.TriggerIndex.Value) ? 0 : 1);
				this.TriggerIndex = null;
			}
		}

		protected override void CheckValueAgainstTarget(bool new_value, bool old_value, bool target)
		{
			
		}

		protected override bool GetValueFromJumpGate(MyJumpGate jump_gate)
		{
			if (this.ListeningJumpGates.Contains(jump_gate)) return true;
			this.OnJumpGateAdded(jump_gate);
			return true;
		}

		protected override bool IsJumpGateValidForList(MyJumpGate jump_gate)
		{
			return base.IsJumpGateValidForList(jump_gate) && jump_gate.IsValid();
		}

		protected override void OnJumpGateAdded(MyJumpGate jump_gate)
		{
			MyAPIGateway.Utilities.ShowNotification("GATE_ADDED_0");
			base.OnJumpGateAdded(jump_gate);
			if (!MyNetworkInterface.IsServerLike || this.ListeningJumpGates.Contains(jump_gate)) return;
			jump_gate.EntityEnterered += this.OnEntityCollision;
			this.ListeningJumpGates.Add(jump_gate);
			MyAPIGateway.Utilities.ShowNotification("GATE_ADDED_1");
		}

		protected override void OnJumpGateRemoved(MyJumpGate jump_gate)
		{
			MyAPIGateway.Utilities.ShowNotification("GATE_REMOVED_0");
			base.OnJumpGateAdded(jump_gate);
			if (!MyNetworkInterface.IsServerLike) return;
			jump_gate.EntityEnterered -= this.OnEntityCollision;
			this.ListeningJumpGates.Remove(jump_gate);
			MyAPIGateway.Utilities.ShowNotification("GATE_REMOVED_1");
		}

		protected override void OnSelected()
		{
			base.OnSelected();
			foreach (MyJumpGate listener in this.ListeningJumpGates) listener.EntityEnterered += this.OnEntityCollision;
		}

		protected override void OnUnselected()
		{
			base.OnUnselected();
			foreach (MyJumpGate listener in this.ListeningJumpGates) listener.EntityEnterered -= this.OnEntityCollision;
		}

		public override void CreateTerminalInterfaceControls<T>()
		{
			base.CreateTerminalControls<T, JumpGateEntityEnteredEvent>();
		}
	}
}
