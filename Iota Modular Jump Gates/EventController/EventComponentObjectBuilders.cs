using ProtoBuf;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.ObjectBuilders;

namespace IOTA.ModularJumpGates.EventController.ObjectBuilders
{
	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_EventCapacitorChargePercentChanged : MyObjectBuilder_ComponentBase { }

	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_EventJumpGateControllerChanged : MyObjectBuilder_ComponentBase { }

	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_EventJumpGateDriveCountChanged : MyObjectBuilder_ComponentBase { }

	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_EventJumpGateEffectiveRadiusChanged : MyObjectBuilder_ComponentBase { }

	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_EventJumpGateEntityCountChanged : MyObjectBuilder_ComponentBase { }

	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_EventJumpGateEntityEntered : MyObjectBuilder_ComponentBase { }

	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_EventJumpGateEntityMassChanged : MyObjectBuilder_ComponentBase { }

	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_EventJumpGateNodeVelocityChanged : MyObjectBuilder_ComponentBase { }

	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_EventJumpGatePhaseChanged : MyObjectBuilder_ComponentBase { }

	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_EventJumpGatePowerFactorChanged : MyObjectBuilder_ComponentBase { }

	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_EventJumpGateRadiusChanged : MyObjectBuilder_ComponentBase { }

	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_EventJumpGateRequiredPowerChanged : MyObjectBuilder_ComponentBase { }

	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_EventJumpGateStatusChanged : MyObjectBuilder_ComponentBase { }

	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_EventJumpGateTargetDistanceChanged : MyObjectBuilder_ComponentBase { }

	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_EventJumpGateDetonationStarted : MyObjectBuilder_ComponentBase { }

	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_EventJumpGateDetonatorArmed : MyObjectBuilder_ComponentBase { }

	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_EventJumpGateDetonatorCountdownChanged : MyObjectBuilder_ComponentBase { }

	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_EventRemoteAntennaConnected : MyObjectBuilder_ComponentBase { }
}
