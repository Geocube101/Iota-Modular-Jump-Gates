using IOTA.ModularJumpGates.Terminal;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI;

namespace IOTA.ModularJumpGates.Session
{
	internal partial class MyJumpGateModSession
	{
		#region Private Variables
		/// <summary>
		/// Whether to force a redraw of all terminal controls
		/// </summary>
		private bool __RedrawAllTerminalControls = false;

		/// <summary>
		/// Stores the active terminal block
		/// </summary>
		private long InteractedBlock = -1;

		/// <summary>
		/// Stores the last opened terminal page
		/// </summary>
		private MyTerminalPageEnum LastTerminalPage = MyTerminalPageEnum.None;
		#endregion

		#region Private Methods
		private void UnloadTerminal()
		{
			MyCubeBlockTerminal.Unload();
			MyCockpitTerminal.Unload();
			MyJumpGateCapacitorTerminal.Unload();
			MyJumpGateControllerTerminal.Unload();
			MyJumpGateDriveTerminal.Unload();
			MyJumpGateRemoteAntennaTerminal.Unload();
			MyJumpGateRemoteLinkTerminal.Unload();
			MyAPIGateway.TerminalControls.CustomControlGetter -= this.OnTerminalSelector;
		}

		/// <summary>
		/// Actually redraws all controls on the terminal<br />
		/// Resets closed terminal menu search bars
		/// </summary>
		private void DoRedrawTerminalControls()
		{
			if (!MyNetworkInterface.IsClientLike) return;

			if (this.InitializationComplete && (this.GameTick % 60 == 0 || this.__RedrawAllTerminalControls))
			{
				MyCubeBlockTerminal.UpdateRedrawControls();
				MyCockpitTerminal.UpdateRedrawControls();
				MyJumpGateCapacitorTerminal.UpdateRedrawControls();
				MyJumpGateControllerTerminal.UpdateRedrawControls();
				MyJumpGateDriveTerminal.UpdateRedrawControls();
				MyJumpGateRemoteAntennaTerminal.UpdateRedrawControls();
				MyJumpGateRemoteLinkTerminal.UpdateRedrawControls();
				this.__RedrawAllTerminalControls = false;
			}

			if (MyAPIGateway.Gui.GetCurrentScreen != MyTerminalPageEnum.ControlPanel)
			{
				MyJumpGateControllerTerminal.ResetSearchInputs();
				MyJumpGateRemoteAntennaTerminal.ResetSearchInputs();
			}
		}

		/// <summary>
		/// Event handler for when a block's terminal controls are iterated
		/// </summary>
		/// <param name="block">The block</param>
		/// <param name="controls">The block's controls</param>
		private void OnTerminalSelector(IMyTerminalBlock block, List<IMyTerminalControl> controls)
		{
			this.InteractedBlock = (MyJumpGateModSession.IsBlockCubeBlockBase(block)) ? block.EntityId : -1;
			List<string> standard_control_ids = new List<string>() { "OnOff", "ShowInTerminal", "ShowInInventory", "ShowInToolbarConfig", "Name", "ShowOnHUD", "CustomData" };

			if (MyJumpGateModSession.IsBlockJumpGateController(block))
			{
				List<IMyTerminalControl> lcd_controls = controls.Where((control) => !standard_control_ids.Contains(control.Id) && !MyJumpGateControllerTerminal.IsControl(control)).ToList();
				foreach (IMyTerminalControl control in lcd_controls) controls.Remove(control);
				if (MyJumpGateControllerTerminal.TerminalSection == MyJumpGateControllerTerminal.MyTerminalSection.SCREENS) controls.AddList(lcd_controls);
			}
			else if (MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block))
			{
				List<IMyTerminalControl> removed = new List<IMyTerminalControl>();

				foreach (IMyTerminalControl control in controls)
				{
					if (control.Id.Length == 0 || MyJumpGateRemoteAntennaTerminal.IsControl(control) || standard_control_ids.Contains(control.Id)) continue;
					removed.Add(control);
				}

				controls.RemoveAll(removed.Contains);
			}
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Marks this session to redraw JumpGateController terminal controls
		/// </summary>
		public void RedrawAllTerminalControls()
		{
			this.__RedrawAllTerminalControls = true;
		}
		#endregion
	}
}
