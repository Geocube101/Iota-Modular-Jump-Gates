using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI;
using VRageMath;

namespace IOTA.ModularJumpGates.API.AnimationAPI
{
	public class MyAnimationAPISession
	{
		private static IMyModContext ModContext;
		private static Action<IMyModContext, byte[]> AnimationAdder;
		private static List<Definitions.AnimationDef> AnimationDefinitions = new List<Definitions.AnimationDef>();
		public static readonly long ModAPIID = 3313236685;
		public static readonly int[] ModAPIVersion = new int[2] { 1, 1 };
		public static bool Initialized { get; private set; } = false;

		/// <summary>
		/// The world's world matrix
		/// </summary>
		public static readonly MatrixD WorldMatrix = MatrixD.CreateWorld(Vector3D.Zero, new Vector3D(0, 0, -1), new Vector3D(0, 1, 0));

		/// <summary>
		/// Initializes the Mod API Session
		/// </summary>
		/// <param name="context">Your mod context</param>
		/// <returns>Whether the API was initialized</returns>
		public static bool Init(IMyModContext context)
		{
			MyAPIGateway.Utilities.SendModMessage(MyAnimationAPISession.ModAPIID, new Dictionary<string, object>()
			{
				["Type"] = "animationapi",
				["Callback"] = (Action<Action<IMyModContext, byte[]>>) ((adder) => {
					if (MyAnimationAPISession.Initialized = adder != null) new MyAnimationAPISession(context, adder);
				}),
				["Version"] = MyAnimationAPISession.ModAPIVersion,
				["ModContext"] = context,
			});
			return MyAnimationAPISession.Initialized;
		}

		private MyAnimationAPISession(IMyModContext context, Action<IMyModContext, byte[]> adder)
		{
			MyAnimationAPISession.AnimationAdder = adder;
			MyAnimationAPISession.ModContext = context;
			foreach (Definitions.AnimationDef animation in MyAnimationAPISession.AnimationDefinitions) MyAnimationAPISession.AnimationAdder(MyAnimationAPISession.ModContext, MyAPIGateway.Utilities.SerializeToBinary(animation));
			MyAnimationAPISession.AnimationDefinitions.Clear();
		}

		public static void AddAnimation(Definitions.AnimationDef animation)
		{
			if (MyAnimationAPISession.Initialized) MyAnimationAPISession.AnimationAdder(MyAnimationAPISession.ModContext, MyAPIGateway.Utilities.SerializeToBinary(animation));
			else MyAnimationAPISession.AnimationDefinitions.Add(animation);
		}
	}
}
