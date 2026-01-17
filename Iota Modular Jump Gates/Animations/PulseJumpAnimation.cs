using VRageMath;

namespace IOTA.ModularJumpGates.Animations
{
	public partial class AnimationDefinitions
	{
		public AnimationDef PulseJump = new AnimationDef("Pulse Jump", "Jump effect with instant activation")
		{
			Enabled = true,
			ImmediateCancel = false,

			JumpedAnimationDef = new JumpGateJumpedAnimationDef {
				Duration = 600,
				TravelTime = 300,

				DriveEmissiveColor = new DriveEmissiveColorDef {
					Duration = 300,
					EmissiveColor = new Vector4(0.5f, 0.625f, 1f, 1f),
					Brightness = 3,
					Animations = new AttributeAnimationDef {
						ParticleColorAnimation = new VectorKeyframe[] {
							new VectorKeyframe(0, new Vector4D(0, 0, 0, 0)),
							new VectorKeyframe(15, new Vector4D(0.5, 0.625, 1, 1)),
							new VectorKeyframe(45, new Vector4D(0.5, 0.625, 1, 1)),
							new VectorKeyframe(60, new Vector4D(0, 0, 0, 0)),
						},
					},
				},

				TravelEffects = new ParticleDef[] {
					new ParticleDef {
						Duration = 300,
						ParticleNames = new string[] { "IOTA.TravelEffect.WarpField" },
						ParticleOrientation = new ParticleOrientationDef(ParticleOrientationEnum.GATE_TRUE_ENDPOINT_NORMAL),

						Animations = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(1, 1, 1, 1)),
								new VectorKeyframe(210, new Vector4D(1, 1, 1, 1), EasingCurveEnum.EXPONENTIAL, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(240, new Vector4D(1, 1, 1, 0)),
							},
						},
					},
				},

				TravelSounds = new SoundDef[] {
					new SoundDef {
						Duration = 300,
						SoundNames = new string[] { "IOTA.TravelEffects.Standard_0" },
						Distance = 1000,
					},
				},

				BeamPulse = new BeamPulseDef
				{
					BeamBrightness = 20,
					BeamFrequency = 512,
					TravelTime = 300,
					Duration = 360,

					Animations = new AttributeAnimationDef
					{
						ParticleColorAnimation = new VectorKeyframe[] {
							new VectorKeyframe(300, new Vector4D(0.5, 0.625, 1, 1)),
							new VectorKeyframe(360, Vector4D.Zero),
						},
						ParticleRadiusAnimation = new DoubleKeyframe[] {
							new DoubleKeyframe(0, 0d, ratio_type: RatioTypeEnum.RANDOM, lower: 0.25, upper: 1) * AnimationSourceEnum.JUMP_GATE_SIZE / 25,
							new DoubleKeyframe(360, 0d),
						},
					},

					FlashPointParticles = new ParticleDef[] {
						new ParticleDef {
							ParticleNames = new string[] { "IOTA.BasicFlashPoint" },
							Animations = new AttributeAnimationDef {
								ParticleScaleAnimation = new DoubleKeyframe[] {
									new DoubleKeyframe(300, AnimationSourceEnum.JUMP_GATE_SIZE) / 4,
									new DoubleKeyframe(360, 0d),
								},
								ParticleColorAnimation = new VectorKeyframe[] {
									new VectorKeyframe(0, Vector4D.One, ratio_type: RatioTypeEnum.ENDPOINT_DISTANCE, lower: new Vector4D(0.5, 0.625, 1, 1), upper: new Vector4D(1, 0.625, 0.5, 1)),
									new VectorKeyframe(180, Vector4D.One, ratio_type: RatioTypeEnum.ENDPOINT_DISTANCE, lower: new Vector4D(0.5, 0.625, 1, 1), upper: new Vector4D(1, 0.625, 0.5, 1)),
									new VectorKeyframe(240, Vector4D.Zero),
								},
							},
						},
					}
				},

				NodeSounds = new SoundDef[] {
					new SoundDef {
						SoundNames = new string[] { "IOTA.Tesseract.JumpGateJumpedOutbound" },
						Duration = 300,
					},
				},

				AntiNodeSounds = new SoundDef[] {
					new SoundDef {
						SoundNames = new string[] { "IOTA.Tesseract.JumpGateJumpedInbound" },
						StartTime = 120,
						Duration = 360,
					},
				},

				NodeParticles = new ParticleDef[] {
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.PulseJump.NodeParticleJumped" },
						CleanOnEffectEnd = false,
						Duration = 300,
						Animations = new AttributeAnimationDef {
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE),
							}
						}
					},
				},

				AntiNodeParticles = new ParticleDef[] {
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.PulseJump.AntiNodeParticleJumped" },
						CleanOnEffectEnd = false,
						Duration = 300,
						Animations = new AttributeAnimationDef {
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_ANTIGATE_SIZE),
							}
						}
					},
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.PulseJump.NodeParticleJumped" },
						CleanOnEffectEnd = false,
						StartTime = 300,
						Duration = 300,
						Animations = new AttributeAnimationDef {
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_ANTIGATE_SIZE),
							}
						}
					},
				},
			}
		};
	}
}
