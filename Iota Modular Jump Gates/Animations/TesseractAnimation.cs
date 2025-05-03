using VRageMath;

namespace IOTA.ModularJumpGates.Animations
{
    public partial class AnimationDefinitions
    {
		public AnimationDef TesseractAnimation = new AnimationDef("Tesseract", "A nested tesseract jump gate animation")
		{
			Enabled = true,

			JumpingAnimationDef = new JumpGateJumpingAnimationDef
			{
				Duration = 1440,

				NodeParticles = new ParticleDef[] {
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Tesseract.NodeParticleJumping.TesseractCube" },
						StartTime = 0,
						Duration = 1440,
						CleanOnEffectEnd = true,

						Animations = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.5, 0.625, 1, 1)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, 0d, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new DoubleKeyframe(60, AnimationSourceEnum.JUMP_GATE_SIZE, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN) * 0.625,
								new DoubleKeyframe(1440, 0d),
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, -0.1d, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(1440, -0.125d),
							},
						},
					},
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Tesseract.NodeParticleJumping.TesseractCube" },
						StartTime = 0,
						Duration = 1440,
						CleanOnEffectEnd = true,

						Animations = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.5, 0.625, 1, 1)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, 0d, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new DoubleKeyframe(60, AnimationSourceEnum.JUMP_GATE_SIZE, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN) * 0.375,
								new DoubleKeyframe(1440, 0d),
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, 0.1d, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(1440, -2.5d),
							},
						},
					},
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Tesseract.NodeParticleJumping.TesseractCube" },
						StartTime = 0,
						Duration = 1440,
						CleanOnEffectEnd = true,

						Animations = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.5, 0.625, 1, 1)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, 0d, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new DoubleKeyframe(60, AnimationSourceEnum.JUMP_GATE_SIZE, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN) * 0.125,
								new DoubleKeyframe(1440, 0d),
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, -0.1d, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(1440, 5d),
							},
						},
					},
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Tesseract.NodeParticleJumping.TesseractCore" },
						StartTime = 0,
						Duration = 1440,

						Animations = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.5, 0.625, 1, 1)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[]{
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE),
							},
							ParticleVelocityAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE) / 100d,
							},
						},
					},
				},

				AntiNodeParticles = new ParticleDef[] {
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Tesseract.NodeParticleJumping.TesseractCube" },
						StartTime = 0,
						Duration = 1260,
						CleanOnEffectEnd = true,

						Animations = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.5, 0.625, 1, 1)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, 0d, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new DoubleKeyframe(60, AnimationSourceEnum.JUMP_ANTIGATE_SIZE, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN) * 0.625,
								new DoubleKeyframe(1260, 0d),
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, -0.1d, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(1260, -0.125d),
							},
						},
					},
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Tesseract.NodeParticleJumping.TesseractCube" },
						StartTime = 0,
						Duration = 1260,
						CleanOnEffectEnd = true,

						Animations = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.5, 0.625, 1, 1)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, 0d, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new DoubleKeyframe(60, AnimationSourceEnum.JUMP_ANTIGATE_SIZE, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN) * 0.375,
								new DoubleKeyframe(1260, 0d),
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, 0.1d, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(1260, -2.5d),
							},
						},
					},
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Tesseract.NodeParticleJumping.TesseractCube" },
						StartTime = 0,
						Duration = 1260,
						CleanOnEffectEnd = true,

						Animations = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.5, 0.625, 1, 1)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, 0d, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new DoubleKeyframe(60, AnimationSourceEnum.JUMP_ANTIGATE_SIZE, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN) * 0.125,
								new DoubleKeyframe(1260, 0d),
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, -0.1d, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(1260, 5d),
							},
						},
					},
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Tesseract.NodeParticleJumping.TesseractCore" },
						StartTime = 0,
						Duration = 1260,

						Animations = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.5, 0.625, 1, 1)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[]{
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_ANTIGATE_SIZE),
							},
						},
					},
				},

				PerDriveParticles = new ParticleDef[] {
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Tesseract.DriveParticle" },
						StartTime = 0,
						Duration = 1440,
						ParticleOffset = new Vector3D(0, 0, 10),

						Animations = new AttributeAnimationDef {
							ParticleBirthAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, 0d, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new DoubleKeyframe(300, 1),
							},
						},
					},
				},

				PerAntiDriveParticles = new ParticleDef[] {
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Tesseract.DriveParticle" },
						StartTime = 0,
						Duration = 1440,
						ParticleOffset = new Vector3D(0, 0, 10),

						Animations = new AttributeAnimationDef {
							ParticleBirthAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, 0d, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new DoubleKeyframe(300, 1),
							},
						},
					},
				},

				NodePhysics = new NodePhysicsDef
				{
					AttractorForce = 12.5,
					AttractorForceFalloff = 2,
				},

				AntiNodePhysics = new NodePhysicsDef
				{
					AttractorForce = 12.5,
					AttractorForceFalloff = 2,
				},

				NodeSounds = new SoundDef[] {
					new SoundDef {
						SoundNames = new string[] { "IOTA.Tesseract.JumpGateJumping" },
						StartTime = 0,
						Duration = 1440,
					},
				},

				AntiNodeSounds = new SoundDef[] {
					new SoundDef {
						SoundNames = new string[] { "IOTA.Tesseract.JumpGateJumping" },
						StartTime = 0,
						Duration = 1440,
					},
				},

				DriveEmissiveColor = new DriveEmissiveColorDef
				{
					Duration = 300,
					EmissiveColor = new Color(62, 133, 247),
					Brightness = 10,
				},
			},

			JumpedAnimationDef = new JumpGateJumpedAnimationDef
			{
				Duration = 360,

				NodeParticles = new ParticleDef[] {
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Tesseract.NodeParticleJumped.PulseFire" },
						StartTime = 0,
						Duration = 360,

						Animations = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.5, 0.625, 1, 1)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE),
							},
						},
					},
				},

				AntiNodeParticles = new ParticleDef[] {
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Tesseract.NodeParticleJumped.PulseFire" },
						StartTime = 180,
						Duration = 180,
						ParticleOrientation = new ParticleOrientationDef(ParticleOrientationEnum.ANTIGATE_DRIVE_NORMAL),

						Animations = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.5, 0.625, 1, 1)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_ANTIGATE_SIZE),
							},
						},
					},
				},

				PerDriveParticles = new ParticleDef[] {
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Tesseract.DriveParticle" },
						StartTime = 0,
						Duration = 360,
						ParticleOffset = new Vector3D(0, 0, 10),

						Animations = new AttributeAnimationDef {
							ParticleBirthAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, 1, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new DoubleKeyframe(300, 0d),
							},
						},
					},
				},

				PerAntiDriveParticles = new ParticleDef[] {
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Tesseract.DriveParticle" },
						StartTime = 0,
						Duration = 360,
						ParticleOffset = new Vector3D(0, 0, 10),

						Animations = new AttributeAnimationDef {
							ParticleBirthAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, 1, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new DoubleKeyframe(300, 0d),
							},
						},
					},
				},

				TravelEffects = new ParticleDef[] {
					new ParticleDef {
						Duration = 180,
						ParticleNames = new string[] { "IOTA.TravelEffect.WarpField" },
						ParticleOrientation = new ParticleOrientationDef(ParticleOrientationEnum.GATE_TRUE_ENDPOINT_NORMAL),
						ParticleOffset = new Vector3D(0, 0, -312.5),

						Animations = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.5, 0.625, 1, 1), EasingCurveEnum.EXPONENTIAL, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(150, new Vector4D(0.5, 0.625, 1, 0)),
							},
						},
					},
				},

				TravelSounds = new SoundDef[] {
					new SoundDef {
						Duration = 240,
						SoundNames = new string[] { "IOTA.TravelEffects.Standard_0" },
						Distance = 1000,
					},
				},

				BeamPulse = new BeamPulseDef {
					BeamBrightness = 20,
					BeamFrequency = 0,
					TravelTime = 180,

					Animations = new AttributeAnimationDef {
						ParticleColorAnimation = new VectorKeyframe[] {
							new VectorKeyframe(0, new Vector4D(0.5, 0.625, 1, 1)),
							new VectorKeyframe(240, Vector4D.Zero),
						},
						ParticleRadiusAnimation = new DoubleKeyframe[] {
							new DoubleKeyframe(0, 0d, ratio_type: RatioTypeEnum.RANDOM, lower: 0.25, upper: 1) * AnimationSourceEnum.JUMP_GATE_SIZE / 25,
						},
					},

					FlashPointParticles = new ParticleDef[] {
						new ParticleDef {
							ParticleNames = new string[] { "IOTA.BasicFlashPoint" },
							Animations = new AttributeAnimationDef {
								ParticleScaleAnimation = new DoubleKeyframe[] {
									new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE) / 4,
								},
							},
						},
					}
				},

				NodeSounds = new SoundDef[] {
					new SoundDef {
						SoundNames = new string[] { "IOTA.Tesseract.JumpGateJumpedOutbound" },
						StartTime = 0,
						Duration = 360,
					},
				},

				AntiNodeSounds = new SoundDef[] {
					new SoundDef {
						SoundNames = new string[] { "IOTA.Tesseract.JumpGateJumpedInbound" },
						StartTime = 0,
						Duration = 360,
					},
				},
			},

			FailedAnimationDef = new JumpGateFailedAnimationDef
			{
				Duration = 330,

				NodeParticles = new ParticleDef[] {
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Tesseract.NodeParticleJumping.TesseractCube" },
						StartTime = 0,
						Duration = 300,

						Animations = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.5, 0.625, 1, 1), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(300, new Vector4D(1, 0.5, 0.25, 1)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, 0d, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new DoubleKeyframe(300, AnimationSourceEnum.JUMP_GATE_SIZE) * 0.625,
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, -1.25, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, 0d),
							},
						},
					},
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Tesseract.NodeParticleJumping.TesseractCube" },
						StartTime = 0,
						Duration = 300,

						Animations = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.5, 0.625, 1, 1), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(300, new Vector4D(1, 0.5, 0.25, 1)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, 0d, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new DoubleKeyframe(300, AnimationSourceEnum.JUMP_GATE_SIZE) * 0.375,
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, 2.5, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, 0d),
							},
						},
					},
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Tesseract.NodeParticleJumping.TesseractCube" },
						StartTime = 0,
						Duration = 300,

						Animations = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.5, 0.625, 1, 1), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(300, new Vector4D(1, 0.5, 0.25, 1)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, 0d, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new DoubleKeyframe(300, AnimationSourceEnum.JUMP_GATE_SIZE) * 0.125,
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, -5, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, 0d),
							},
						},
					},
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Tesseract.NodeParticleJumping.TesseractCore" },
						StartTime = 0,
						Duration = 300,

						Animations = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.5, 0.625, 1, 1), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(300, new Vector4D(1, 0.5, 0.375, 1)),
							},
							ParticleVelocityAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE) / 100d,
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE),
							},
						},
					},
				},

				AntiNodeParticles = new ParticleDef[] {
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Tesseract.NodeParticleFailed.PulseWave" },
						StartTime = 0,
						Duration = 300,
						ParticleOrientation = new ParticleOrientationDef(ParticleOrientationEnum.ANTIGATE_DRIVE_NORMAL),

						Animations = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, 1, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(300, new Vector4D(1, 0.5, 0.375, 1)),
							},
							ParticleVelocityAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_ANTIGATE_SIZE) / 100d,
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_ANTIGATE_SIZE),
							},
						},
					},
				},

				PerDriveParticles = new ParticleDef[] {
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Tesseract.DriveParticle" },
						StartTime = 0,
						Duration = 360,
						ParticleOffset = new Vector3D(0, 0, 10),

						Animations = new AttributeAnimationDef {
							ParticleBirthAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, 1, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new DoubleKeyframe(300, 0d),
							},
						},
					},
				},

				PerAntiDriveParticles = new ParticleDef[] {
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Tesseract.DriveParticle" },
						StartTime = 0,
						Duration = 360,
						ParticleOffset = new Vector3D(0, 0, 10),

						Animations = new AttributeAnimationDef {
							ParticleBirthAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, 1, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new DoubleKeyframe(300, 0d),
							},
						},
					},
				},

				NodePhysics = new NodePhysicsDef
				{
					AttractorForce = -12.5,
					AttractorForceFalloff = 2,
				},

				AntiNodePhysics = new NodePhysicsDef
				{
					AttractorForce = -12.5,
					AttractorForceFalloff = 2,
				},

				NodeSounds = new SoundDef[] {
					new SoundDef {
						SoundNames = new string[] { "IOTA.Tesseract.JumpGateFailed" },
						StartTime = 0,
						Duration = 330,
					},
				},

				AntiNodeSounds = new SoundDef[] {
					new SoundDef {
						SoundNames =  new string[] { "IOTA.Tesseract.JumpGateFailed" },
						StartTime = 0,
						Duration = 330,
					},
				},

				DriveEmissiveColor = new DriveEmissiveColorDef
				{
					Duration = 330,
					EmissiveColor = Color.Black,
					Brightness = 10,
				},
			},
		};
    }
}
