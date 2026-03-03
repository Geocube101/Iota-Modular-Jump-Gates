using VRageMath;

namespace IOTA.ModularJumpGates.Animations
{
	public partial class AnimationDefinitions
	{
		public AnimationDef WormholeStargate = new AnimationDef("Stargate", "Wormhole jump effect mimicing a stargate\nYes the kawoosh works")
		{
			Enabled = true,
			ImmediateCancel = false,

			JumpedAnimationDef = new JumpGateJumpedAnimationDef {
				Duration = 600,
				TravelTime = 420,

				TravelEffects = new ParticleDef[] {
					new ParticleDef {
						Duration = 420,
						ParticleNames = new string[] { "IOTA.TravelEffect.StargateTransit" },
						ParticleOrientation = new ParticleOrientationDef(ParticleOrientationEnum.GATE_TRUE_ENDPOINT_NORMAL),

						Animations = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(1, 1, 1, 1)),
								new VectorKeyframe(390, new Vector4D(1, 1, 1, 1), EasingCurveEnum.EXPONENTIAL, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(420, new Vector4D(1, 1, 1, 0)),
							},
						},
					},
				},

				TravelSounds = new SoundDef[] {
					new SoundDef {
						Duration = 420,
						SoundNames = new string[] { "IOTA.TravelEffects.Stargate_0" },
						Distance = 1000,
					},
				},

				NodeSounds = new SoundDef[] {
					new SoundDef {
						SoundNames = new string[] { "IOTA.Stargate.JumpGateJumped" },
						Duration = 90,
					},
				},

				AntiNodeSounds = new SoundDef[] {
					new SoundDef {
						SoundNames = new string[] { "IOTA.Stargate.JumpGateJumped" },
						StartTime = 330,
						Duration = 90,
					},
				},
			},

			WormholeOpenAnimationDef = new JumpGateWormholeAnimationDef {
				Duration = 270,
				NodeSounds = new SoundDef[] {
					new SoundDef {
						SoundNames = new string[] { "IOTA.Stargate.JumpGateWormholeOpening" },
					},
				},
				AntiNodeSounds = new SoundDef[] {
					new SoundDef {
						SoundNames = new string[] { "IOTA.Stargate.JumpGateWormholeOpening" },
					},
				},
				DriveEmissiveColor = new DriveEmissiveColorDef
				{
					Duration = 60,
					EmissiveColor = new Vector4(0.5f, 0.625f, 1f, 1f),
					Brightness = 3,
				},
				NodeShapeColliders = new ShapeColliderDef[] {
					new ShapeColliderDef {
						CollisionShape = CollisionShapeEnum.CYLINDER,
						CollisionEffectType = CollisionEffectTypeEnum.DELETE,
						EffectArguments = new double[] { 15 },
						Animations = new AttributeAnimationDef {
							ShapeScaleAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, AnimationSourceEnum.JUMP_GATE_RADII),
							},
							ShapeInnerCutoutAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.One, EasingCurveEnum.SINE, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(60, Vector4D.UnitY),
							},
						},
					},
					new ShapeColliderDef {
						CollisionShape = CollisionShapeEnum.CYLINDER,
						CollisionEffectType = CollisionEffectTypeEnum.DELETE,
						EffectArguments = new double[] { 15 },
						StartTime = 60,
						Duration = 210,
						Animations = new AttributeAnimationDef {
							ShapeScaleAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, AnimationSourceEnum.JUMP_GATE_RADII, EasingCurveEnum.SINE, EasingTypeEnum.EASE_OUT) * new Vector4D(0.5, 0, 0.5, 0),
								new VectorKeyframe(105, AnimationSourceEnum.JUMP_GATE_RADII, EasingCurveEnum.SINE, EasingTypeEnum.EASE_IN) * new Vector4D(0.5, 6, 0.5, 0),
								new VectorKeyframe(210, AnimationSourceEnum.JUMP_GATE_RADII) * new Vector4D(0.5, 0, 0.5, 0),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.SINE, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(105, Vector4D.UnitY / 3, EasingCurveEnum.SINE, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(210, Vector4D.Zero),
							},
						},
					},
				},
				AntiNodeShapeColliders = new ShapeColliderDef[] {
					new ShapeColliderDef {
						CollisionShape = CollisionShapeEnum.CYLINDER,
						CollisionEffectType = CollisionEffectTypeEnum.DELETE,
						EffectArguments = new double[] { 15 },
						Duration = 210,
						Animations = new AttributeAnimationDef {
							ShapeScaleAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, AnimationSourceEnum.JUMP_ANTIGATE_RADII),
							},
							ShapeInnerCutoutAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.One, EasingCurveEnum.SINE, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(60, Vector4D.UnitY),
							},
						},
					},
					new ShapeColliderDef {
						CollisionShape = CollisionShapeEnum.CYLINDER,
						CollisionEffectType = CollisionEffectTypeEnum.DELETE,
						EffectArguments = new double[] { 15 },
						StartTime = 60,
						Duration = 210,
						Animations = new AttributeAnimationDef {
							ShapeScaleAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, AnimationSourceEnum.JUMP_ANTIGATE_RADII, EasingCurveEnum.SINE, EasingTypeEnum.EASE_OUT) * new Vector4D(0.5, 0, 0.5, 0),
								new VectorKeyframe(105, AnimationSourceEnum.JUMP_ANTIGATE_RADII, EasingCurveEnum.SINE, EasingTypeEnum.EASE_IN) * new Vector4D(0.5, 6, 0.5, 0),
								new VectorKeyframe(210, AnimationSourceEnum.JUMP_ANTIGATE_RADII) * new Vector4D(0.5, 0, 0.5, 0),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.SINE, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(105, Vector4D.UnitY / 2, EasingCurveEnum.SINE, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(210, Vector4D.Zero),
							},
						},
					},
				},
				NodeParticles = new ParticleDef[] {
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Stargate.EventHorizonOpening" },
						Animations = new AttributeAnimationDef {
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE),
							},
						},
					},
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Stargate.EventHorizonKawoosh" },
						StartTime = 60,
						Duration = 210,
						Animations = new AttributeAnimationDef {
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.SINE, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(105, AnimationSourceEnum.JUMP_GATE_HEIGHT, EasingCurveEnum.SINE, EasingTypeEnum.EASE_IN) * new Vector4D(0, 0, 2, 0),
								new VectorKeyframe(210, Vector4D.Zero),
							},
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(5, Vector4D.Zero),
								new VectorKeyframe(97, Vector4D.One),
								new VectorKeyframe(205, Vector4D.Zero),
							},
						},
					},
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Stargate.EventHorizonKawoosh" },
						StartTime = 60,
						Duration = 210,
						Animations = new AttributeAnimationDef {
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE) * 1.25,
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.SINE, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(105, AnimationSourceEnum.JUMP_GATE_HEIGHT, EasingCurveEnum.SINE, EasingTypeEnum.EASE_IN) * new Vector4D(0, 0, 4, 0),
								new VectorKeyframe(210, Vector4D.Zero),
							},
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(5, Vector4D.Zero),
								new VectorKeyframe(97, Vector4D.One),
								new VectorKeyframe(205, Vector4D.Zero),
							},
						},
					},
				},
				AntiNodeParticles = new ParticleDef[] {
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Stargate.EventHorizonOpening" },
						Animations = new AttributeAnimationDef {
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_ANTIGATE_SIZE),
							},
						},
					},
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Stargate.EventHorizonKawoosh" },
						StartTime = 60,
						Duration = 210,
						Animations = new AttributeAnimationDef {
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_ANTIGATE_SIZE),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.SINE, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(105, AnimationSourceEnum.JUMP_ANTIGATE_HEIGHT, EasingCurveEnum.SINE, EasingTypeEnum.EASE_IN) * new Vector4D(0, 0, 2, 0),
								new VectorKeyframe(210, Vector4D.Zero),
							},
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(5, Vector4D.Zero),
								new VectorKeyframe(97, Vector4D.One),
								new VectorKeyframe(205, Vector4D.Zero),
							},
						},
					},
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Stargate.EventHorizonKawoosh" },
						StartTime = 60,
						Duration = 210,
						Animations = new AttributeAnimationDef {
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_ANTIGATE_SIZE) * 1.25,
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.SINE, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(105, AnimationSourceEnum.JUMP_ANTIGATE_HEIGHT, EasingCurveEnum.SINE, EasingTypeEnum.EASE_IN) * new Vector4D(0, 0, 4, 0),
								new VectorKeyframe(210, Vector4D.Zero),
							},
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(5, Vector4D.Zero),
								new VectorKeyframe(97, Vector4D.One),
								new VectorKeyframe(205, Vector4D.Zero),
							},
						},
					},
				},
			},

			WormholeLoopAnimationDef = new JumpGateWormholeAnimationDef {
				Duration = 403,
				NodeParticles = new ParticleDef[] {
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Stargate.EventHorizon" },
						Animations = new AttributeAnimationDef {
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE),
							},
						},
					},
				},
				AntiNodeParticles = new ParticleDef[] {
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Stargate.EventHorizon" },
						Animations = new AttributeAnimationDef {
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_ANTIGATE_SIZE),
							},
						},
					},
				},
				NodeShapeColliders = new ShapeColliderDef[] {
					new ShapeColliderDef {
						CollisionShape = CollisionShapeEnum.CYLINDER,
						CollisionEffectType = CollisionEffectTypeEnum.JUMP,
						EffectArguments = new double[] { 60 },
						Animations = new AttributeAnimationDef {
							ShapeScaleAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, AnimationSourceEnum.JUMP_GATE_RADII)
							},
						},
					},
				},
				NodeSounds = new SoundDef[] {
					new SoundDef {
						SoundNames = new string[] { "IOTA.Stargate.JumpGateWormholeLoop" }
					},
				},
				AntiNodeSounds = new SoundDef[] {
					new SoundDef {
						SoundNames = new string[] { "IOTA.Stargate.JumpGateWormholeLoop" }
					},
				},
			},

			WormholeCloseAnimationDef = new JumpGateWormholeAnimationDef {
				Duration = 180,
				NodeSounds = new SoundDef[] {
					new SoundDef {
						SoundNames = new string[] { "IOTA.Stargate.JumpGateWormholeClosing" },
					},
				},
				AntiNodeSounds = new SoundDef[] {
					new SoundDef {
						SoundNames = new string[] { "IOTA.Stargate.JumpGateWormholeClosing" },
					},
				},
				NodeParticles = new ParticleDef[] {
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Stargate.EventHorizonClosing" },
						Animations = new AttributeAnimationDef {
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE),
							},
						},
					},
				},
				AntiNodeParticles = new ParticleDef[] {
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Stargate.EventHorizonClosing" },
						Animations = new AttributeAnimationDef {
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_ANTIGATE_SIZE),
							},
						},
					},
				},
			},
		};
	}
}
