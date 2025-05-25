using System;
using VRageMath;

namespace IOTA.ModularJumpGates.Animations
{
	public partial class AnimationDefinitions
	{
		static ParticleDef[] IMJG_ProtoWarp_DriveParticles;

		public AnimationDef StandardWarpPrototech = new AnimationDef("Prototech Warp", "The prototech SE warp effect") {
			Enabled = true,

			JumpingAnimationDef = new JumpGateJumpingAnimationDef {
				Duration = 600,

				PerEntityParticles = new ParticleDef[] {
					new ParticleDef {
						ParticleNames = new string[] { "Warp_Prototech" },
						ParticleOrientation = new ParticleOrientationDef(ParticleOrientationEnum.GATE_ENDPOINT_NORMAL, MatrixD.CreateFromYawPitchRoll(0, Math.PI, 0)),
						CleanOnEffectEnd = false,
						Duration = 600,
						Animations = new AttributeAnimationDef {
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, 0.025) * AnimationExpression.VectorMax(AnimationSourceEnum.ENTITY_FACING_EXTENTS),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0, 0, 1.5, 0)) * AnimationExpression.VectorMax(AnimationSourceEnum.ENTITY_EXTENTS),
							},
						},
					},
				},

				PerDriveParticles = IMJG_ProtoWarp_DriveParticles = new ParticleDef[] {
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Warp.DriveParticleJumping.ShimmerRing" },
						CleanOnEffectEnd = false,
						Animations = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.678431, 0.517647, 0.137255, 1)),
							},
						},
					},
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Warp.DriveParticleJumping.SmokeCharge" },
						CleanOnEffectEnd = false,
						Animations = new AttributeAnimationDef {
						},
					},
				},

				PerAntiDriveParticles = IMJG_ProtoWarp_DriveParticles,

				AntiNodeParticles = new ParticleDef[] {
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Warp.NodeParticleJumping.NodeGlow" },
						CleanOnEffectEnd = false,
						Animations = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.678431, 0.517647, 0.137255, 1)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE),
							},
						},
					},
				},

				NodeSounds = new SoundDef[] {
					new SoundDef {
						SoundNames = new string[] { "ShipPrototechJumpDriveCharging" },
					},
				},

				AntiNodeSounds = new SoundDef[] {
					new SoundDef {
						SoundNames = new string[] { "ShipPrototechJumpDriveCharging" },
					},
				},

				DriveEmissiveColor = new DriveEmissiveColorDef {
					Duration = 600,
					EmissiveColor = new Color(new Vector4D(0.678431, 0.517647, 0.137255, 1)),
					Brightness = 5,
				},
			},

			JumpedAnimationDef = new JumpGateJumpedAnimationDef {
				Duration = 180,

				PerEntityParticles = new ParticleDef[] {
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Warp.NodeParticleJumped" },
						ParticleOrientation = new ParticleOrientationDef(ParticleOrientationEnum.GATE_ENDPOINT_NORMAL, MatrixD.CreateFromYawPitchRoll(0, Math.PI, 0)),
						CleanOnEffectEnd = false,
						Animations = new AttributeAnimationDef {
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, 0.1) * AnimationExpression.VectorMax(AnimationSourceEnum.ENTITY_FACING_EXTENTS),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0, 0, 1.5, 0)) * AnimationExpression.VectorMax(AnimationSourceEnum.ENTITY_EXTENTS),
							},
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.678431, 0.517647, 0.137255, 1)),
							},
						},
					},
				},

				AntiNodeParticles = new ParticleDef[] {
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Warp.NodeParticleJumping.NodeGlow" },
						CleanOnEffectEnd = false,
						Animations = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.678431, 0.517647, 0.137255, 1)),
								new VectorKeyframe(180, new Vector4D(0.678431, 0.517647, 0.137255, 0)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE),
								new DoubleKeyframe(180, 0d),
							},
						},
					},
				},

				NodeSounds = new SoundDef[] {
					new SoundDef {
						SoundNames = new string[]{ "ShipJumpDriveJumpOut" },
					},
				},

				AntiNodeSounds = new SoundDef[] {
					new SoundDef {
						SoundNames = new string[]{ "ShipPrototechJumpDriveJumpIn" },
					},
				},

				DriveEmissiveColor = new DriveEmissiveColorDef {
					EmissiveColor = Color.Black,
					Brightness = 10,
				},
			},

			FailedAnimationDef = new JumpGateFailedAnimationDef {
				Duration = 300,

				AntiNodeParticles = new ParticleDef[] {
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Warp.NodeParticleJumping.NodeGlow" },
						CleanOnEffectEnd = false,
						Animations = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.678431, 0.517647, 0.137255, 1)),
								new VectorKeyframe(180, new Vector4D(0.678431, 0.517647, 0.137255, 0)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE),
								new DoubleKeyframe(180, 0d),
							},
						},
					},
				},

				NodeSounds = new SoundDef[] {
					new SoundDef {
						SoundNames = new string[]{ "IOTA.ProtoWarp.JumpGateFailed" },
					},
				},

				AntiNodeSounds = new SoundDef[] {
					new SoundDef {
						SoundNames = new string[]{ "IOTA.ProtoWarp.JumpGateFailed" },
					},
				},

				DriveEmissiveColor = new DriveEmissiveColorDef {
					EmissiveColor = Color.Black,
					Brightness = 10,
				},
			},
		};
	}
}
