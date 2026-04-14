using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRageMath;

namespace IOTA.ModularJumpGates.Util
{
	/// <summary>
	/// Physical detector class using the pruning structure to check collisions
	/// </summary>
	internal class MyPhysicalDetector : MyGameLogicComponent
	{
		private BoundingSphereD PruningSphere;
		private readonly List<MyEntity> PruningCheck = new List<MyEntity>();
		private readonly List<MyEntity> DetectedEntities = new List<MyEntity>();
		private readonly Action<IMyEntity, bool> Callback;

		public double Radius
		{
			get { return this.PruningSphere.Radius; }
			set { this.PruningSphere.Radius = value; }
		}

		public MyPhysicalDetector(MyEntity entity, double radius, Action<IMyEntity, bool> callback = null)
		{
			entity.Components.Add(this);
			this.NeedsUpdate = MyEntityUpdateEnum.EACH_10TH_FRAME;
			this.PruningSphere = new BoundingSphereD(entity.WorldMatrix.Translation, radius);
			this.Callback = callback;
		}

		public override void UpdateAfterSimulation10()
		{
			if (this.Entity == null || this.Entity.MarkedForClose || !this.Entity.InScene) return;
			base.UpdateAfterSimulation10();
			this.PruningSphere.Center = this.Entity.WorldMatrix.Translation;
			MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref this.PruningSphere, this.PruningCheck);

			foreach (MyEntity entity in this.PruningCheck)
			{
				if (this.DetectedEntities.Contains(entity)) continue;
				this.DetectedEntities.Add(entity);
				this.Callback?.Invoke(entity, true);
			}

			for (int i = 0; i < this.DetectedEntities.Count; ++i)
			{
				MyEntity entity = this.DetectedEntities[i];
				if (this.PruningCheck.Contains(entity)) continue;
				this.Callback?.Invoke(entity, false);
				this.DetectedEntities[i] = null;
			}

			this.DetectedEntities.RemoveAll(e => e == null);
			this.PruningCheck.Clear();
		}
	}
}
