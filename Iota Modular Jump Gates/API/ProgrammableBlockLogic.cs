using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Collections.Generic;
using VRage.Game.Components;

namespace IOTA.ModularJumpGates.API
{
	/// <summary>
	/// Game logic for programmable blocks
	/// </summary>
	[MyEntityComponentDescriptor(typeof(MyObjectBuilder_MyProgrammableBlock), false, "LargeProgrammableBlock", "SmallProgrammableBlock")]
	internal class ProgrammableBlockLogic : MyGameLogicComponent
	{
		public override void UpdateOnceBeforeFrame()
		{
			base.UpdateOnceBeforeFrame();
			IMyTerminalControlProperty<Func<int[], Action<Dictionary<string, object>>, KeyValuePair<bool, bool>>> api = MyAPIGateway.TerminalControls.CreateProperty<Func<int[], Action<Dictionary<string, object>>, KeyValuePair<bool, bool>>, IMyProgrammableBlock>("IMGJScriptingAPI");
			api.Getter = (block) => MyJumpGateModSession.Instance.ModAPIInterface.HandleModApiScriptingInit;
			MyAPIGateway.TerminalControls.AddControl<IMyProgrammableBlock>(api);
		}
	}
}
