using IOTA.ModularJumpGates.EventController.ObjectBuilders;
using Sandbox.ModAPI;
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

		public override bool IsThresholdUsed => false;
		public override bool IsConditionSelectionUsed => false;
		public override bool IsBlocksListUsed => false;
		public override long UniqueSelectionId => 0x7FFFFFFFFFFFFFFC;
		public override MyStringId EventDisplayName => MyStringId.GetOrCompute("Jump Gate Entity Entered");
		public override string ComponentTypeDebugString => nameof(JumpGateEntityEnteredEvent);

		private void OnEntityCollision(MyJumpGate caller, MyEntity entity, bool is_entering)
		{
			if (!this.IsListeningToJumpGate(caller)) return;
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
			return jump_gate.IsEntityCollisionCallbackRegistered(this.OnEntityCollision);
		}

		protected override bool IsJumpGateValidForList(MyJumpGate jump_gate)
		{
			return base.IsJumpGateValidForList(jump_gate) && jump_gate.IsValid();
		}

		protected override void OnJumpGateAdded(MyJumpGate jump_gate)
		{
			base.OnJumpGateAdded(jump_gate);
			jump_gate.OnEntityCollision(this.OnEntityCollision);
		}

		protected override void OnJumpGateRemoved(MyJumpGate jump_gate)
		{
			base.OnJumpGateAdded(jump_gate);
			jump_gate.OffEntityCollision(this.OnEntityCollision);
		}

		public override void CreateTerminalInterfaceControls<T>()
		{
			base.CreateTerminalControls<T, JumpGateEntityEnteredEvent>();
		}
	}
}
