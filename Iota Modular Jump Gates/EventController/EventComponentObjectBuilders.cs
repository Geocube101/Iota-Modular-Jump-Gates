using ProtoBuf;
using System.Collections.Generic;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.ObjectBuilders;

namespace IOTA.ModularJumpGates.EventController.ObjectBuilders
{
	[ProtoContract]
	[ProtoInclude(10, typeof(MyObjectBuilder_EventJumpGatePhaseChanged))]
	[ProtoInclude(20, typeof(MyObjectBuilder_EventJumpGateStatusChanged))]
	[ProtoInclude(30, typeof(MyObjectBuilder_EventJumpGateEntityCountChanged))]
	[ProtoInclude(40, typeof(MyObjectBuilder_EventJumpGateEntityEntered))]
	[ProtoInclude(50, typeof(MyObjectBuilder_EventJumpGateEntityMassChanged))]
	[ProtoInclude(60, typeof(MyObjectBuilder_EventJumpGateRequiredPowerChanged))]
	[ProtoInclude(70, typeof(MyObjectBuilder_EventJumpGatePowerFactorChanged))]
	[ProtoInclude(80, typeof(MyObjectBuilder_EventJumpGateRadiusChanged))]
	[ProtoInclude(90, typeof(MyObjectBuilder_EventJumpGateEffectiveRadiusChanged))]
	[ProtoInclude(100, typeof(MyObjectBuilder_EventJumpGateNodeVelocityChanged))]
	[ProtoInclude(110, typeof(MyObjectBuilder_EventJumpGateTargetDistanceChanged))]
	[ProtoInclude(120, typeof(MyObjectBuilder_EventJumpGateDriveCountChanged))]
	[ProtoInclude(130, typeof(MyObjectBuilder_EventJumpGateControllerChanged))]
	[ProtoInclude(140, typeof(MyObjectBuilder_EventJumpGateDetonationStarted))]
	[ProtoInclude(150, typeof(MyObjectBuilder_EventJumpGateDetonatorArmed))]
	[ProtoInclude(160, typeof(MyObjectBuilder_EventJumpGateDetonatorCountdownChanged))]
	[ProtoInclude(170, typeof(MyObjectBuilder_EventRemoteAntennaConnected))]
	[ProtoInclude(180, typeof(MyObjectBuilder_EventCapacitorChargePercentChanged))]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_JumpGateEvent : MyObjectBuilder_ComponentBase
	{
		[ProtoMember(1)]
		public string SerializedTargetValue;
		[ProtoMember(2)]
		public List<long> SelectedJumpGates;
		[ProtoMember(3)]
		public List<KeyValuePair<long, byte>> SelectedRemoteJumpGates;
	}

	
	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_EventCapacitorChargePercentChanged : MyObjectBuilder_JumpGateEvent { }

	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_EventJumpGateControllerChanged : MyObjectBuilder_JumpGateEvent
	{
		public enum MyControllerConnectionType : byte { ALL, DIRECT, REMOTE };

		[ProtoMember(1)]
		public MyControllerConnectionType ControllerConnectionType;
	}

	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_EventJumpGateDriveCountChanged : MyObjectBuilder_JumpGateEvent
	{
		[ProtoMember(1)]
		public bool TargetWorkingOnly;
	}

	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_EventJumpGateEffectiveRadiusChanged : MyObjectBuilder_JumpGateEvent { }

	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_EventJumpGateEntityCountChanged : MyObjectBuilder_JumpGateEvent
	{
		[ProtoMember(1)]
		public bool UseControllerEntityFilter;
	}

	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_EventJumpGateEntityEntered : MyObjectBuilder_JumpGateEvent { }

	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_EventJumpGateEntityMassChanged : MyObjectBuilder_JumpGateEvent
	{
		[ProtoMember(1)]
		public bool UseControllerEntityFilter;
	}

	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_EventJumpGateNodeVelocityChanged : MyObjectBuilder_JumpGateEvent { }

	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_EventJumpGatePhaseChanged : MyObjectBuilder_JumpGateEvent { }

	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_EventJumpGatePowerFactorChanged : MyObjectBuilder_JumpGateEvent { }

	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_EventJumpGateRadiusChanged : MyObjectBuilder_JumpGateEvent { }

	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_EventJumpGateRequiredPowerChanged : MyObjectBuilder_JumpGateEvent { }

	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_EventJumpGateStatusChanged : MyObjectBuilder_JumpGateEvent { }

	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_EventJumpGateTargetDistanceChanged : MyObjectBuilder_JumpGateEvent { }

	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_EventJumpGateDetonationStarted : MyObjectBuilder_JumpGateEvent { }

	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_EventJumpGateDetonatorArmed : MyObjectBuilder_JumpGateEvent { }

	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_EventJumpGateDetonatorCountdownChanged : MyObjectBuilder_JumpGateEvent { }

	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_EventRemoteAntennaConnected : MyObjectBuilder_JumpGateEvent { }
}
