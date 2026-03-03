using VRageMath;

namespace IOTA.ModularJumpGates.Animations
{
	public partial class AnimationDefinitions
	{
		public AnimationDef StandardWormhole = new AnimationDef("Standard", "Standard wormhole jump effect")
		{
			Enabled = true,
			ImmediateCancel = false,

			WormholeOpenAnimationDef = new JumpGateWormholeAnimationDef {
				Duration = 300,
				NodeSounds = new SoundDef[] {
					new SoundDef {
						SoundNames = new string[] { "IOTA.Wormhole.JumpGateWormholeOpening" },
					},
				},
				AntiNodeSounds = new SoundDef[] {
					new SoundDef {
						SoundNames = new string[] { "IOTA.Wormhole.JumpGateWormholeOpening" },
					},
				},
				DriveEmissiveColor = new DriveEmissiveColorDef
				{
					Duration = 60,
					EmissiveColor = new Vector4(0.5f, 0.625f, 1f, 1f),
					Brightness = 3,
				},
				NodeParticles = new ParticleDef[] {
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Wormhole.NodeParticleWormholeOpen" },
						Animations = new AttributeAnimationDef {
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE),
							},
						},
					},
				},
				AntiNodeParticles = new ParticleDef[] {
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Wormhole.NodeParticleWormholeOpen" },
						Animations = new AttributeAnimationDef {
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_ANTIGATE_SIZE),
							},
						},
					},
				},
				PerDriveParticles = new ParticleDef[] {
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Tesseract.DriveParticle" },
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
						ParticleOffset = new Vector3D(0, 0, 10),

						Animations = new AttributeAnimationDef {
							ParticleBirthAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, 0d, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new DoubleKeyframe(300, 1),
							},
						},
					},
				},
			},

			WormholeLoopAnimationDef = new JumpGateWormholeAnimationDef {
				Duration = 180,
				NodeParticles = new ParticleDef[] {
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Wormhole.NodeParticleWormholeLoop" },
						Animations = new AttributeAnimationDef {
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE),
							},
						},
					},
				},
				AntiNodeParticles = new ParticleDef[] {
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Wormhole.NodeParticleWormholeLoop" },
						Animations = new AttributeAnimationDef {
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_ANTIGATE_SIZE),
							},
						},
					},
				},
				NodeSounds = new SoundDef[] {
					new SoundDef {
						SoundNames = new string[] { "IOTA.Wormhole.JumpGateWormholeLoop" }
					},
				},
				AntiNodeSounds = new SoundDef[] {
					new SoundDef {
						SoundNames = new string[] { "IOTA.Wormhole.JumpGateWormholeLoop" }
					},
				},
				PerDriveParticles = new ParticleDef[] {
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Tesseract.DriveParticle" },
						ParticleOffset = new Vector3D(0, 0, 10),
					},
				},
				PerAntiDriveParticles = new ParticleDef[] {
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Tesseract.DriveParticle" },
						ParticleOffset = new Vector3D(0, 0, 10),
					},
				},
			},

			WormholeCloseAnimationDef = new JumpGateWormholeAnimationDef {
				Duration = 300,
				NodeSounds = new SoundDef[] {
					new SoundDef {
						SoundNames = new string[] { "IOTA.Wormhole.JumpGateWormholeClosing" },
					},
				},
				AntiNodeSounds = new SoundDef[] {
					new SoundDef {
						SoundNames = new string[] { "IOTA.Wormhole.JumpGateWormholeClosing" },
					},
				},
				NodeParticles = new ParticleDef[] {
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Wormhole.NodeParticleWormholeClose" },
						Animations = new AttributeAnimationDef {
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE),
							},
						},
					},
				},
				AntiNodeParticles = new ParticleDef[] {
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Wormhole.NodeParticleWormholeClose" },
						Animations = new AttributeAnimationDef {
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_ANTIGATE_SIZE),
							},
						},
					},
				},
				PerDriveParticles = new ParticleDef[] {
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Tesseract.DriveParticle" },
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
						ParticleOffset = new Vector3D(0, 0, 10),

						Animations = new AttributeAnimationDef {
							ParticleBirthAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, 1, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new DoubleKeyframe(300, 0d),
							},
						},
					},
				},
			},
		};
	}
}
