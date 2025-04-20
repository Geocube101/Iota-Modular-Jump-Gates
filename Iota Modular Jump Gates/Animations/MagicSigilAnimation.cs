using VRageMath;

namespace IOTA.ModularJumpGates.Animations
{
	public partial class AnimationDefinitions
	{
		private static AttributeAnimationDef MagicSigil_Animation;

		public AnimationDef MagicSigilAnimation = new AnimationDef("Magic Sigil")
		{
			Enabled = true,

			JumpingAnimationDef = new JumpGateJumpingAnimationDef
			{
				Duration = 720,

				NodeParticles = new ParticleDef[] {
					/* Sigils */
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.MagicSigil.Entropy13" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 1 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.5, 0.625, 1, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(60, new Vector4D(0.5, 0.625, 1, 0.01)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE) * 0.625,
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(0.1, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(0.7, 0, 0, 0)),
							},
						},
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Runes.Entropy13" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 2 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(0.05, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(0.35, 0, 0, 0)),
							},
						}.Overlay(MagicSigil_Animation),
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Circle" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 3 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(-0.1, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(-0.7, 0, 0, 0)),
							},
						}.Overlay(MagicSigil_Animation),
					},
					
					// Forward Particles
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Space7" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 4 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.5, 0.625, 1, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(60, new Vector4D(0.5, 0.625, 1, 0.01)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE),
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(-0.1, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(-0.7, 0, 0, 0)),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(60, new Vector4D(0, 0, -0.25, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN) * AnimationSourceEnum.JUMP_GATE_SIZE,
								new VectorKeyframe(720, Vector4D.Zero),
							}
						},
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Runes.Space7" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 5 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(-0.05, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(-0.35, 0, 0, 0)),
							},
						}.Overlay(MagicSigil_Animation),
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Circle" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 6 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(0.1, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(0.7, 0, 0, 0)),
							},
						}.Overlay(MagicSigil_Animation),
					},

					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Trinity3" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 7 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.5, 0.625, 1, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(60, new Vector4D(0.5, 0.625, 1, 0.01)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE) * 0.625,
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(-0.25, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(-1.75, 0, 0, 0)),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(60, new Vector4D(0, 0, -0.5, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN) * AnimationSourceEnum.JUMP_GATE_SIZE,
								new VectorKeyframe(720, Vector4D.Zero),
							}
						},
					},

					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Hexad6" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 8 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.5, 0.625, 1, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(60, new Vector4D(0.5, 0.625, 1, 0.01)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE) * 0.5,
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(-0.5, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(-3.5, 0, 0, 0)),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(60, new Vector4D(0, 0, -0.625, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN) * AnimationSourceEnum.JUMP_GATE_SIZE,
								new VectorKeyframe(720, Vector4D.Zero),
							}
						},
					},

					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Space7" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 9 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.5, 0.625, 1, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(60, new Vector4D(0.5, 0.625, 1, 0.01)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE) * 0.875,
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(0.15, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(1.05, 0, 0, 0)),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(60, new Vector4D(0, 0, -1, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN) * AnimationSourceEnum.JUMP_GATE_SIZE,
								new VectorKeyframe(720, Vector4D.Zero),
							}
						},
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Runes.Space7" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 10 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(-0.025, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(-0.175, 0, 0, 0)),
							},
						}.Overlay(MagicSigil_Animation),
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Circle" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 11 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(0.05, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(0.35, 0, 0, 0)),
							},
						}.Overlay(MagicSigil_Animation),
					},

					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Time5" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 12 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.5, 0.625, 1, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(60, new Vector4D(0.5, 0.625, 1, 0.01)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE) * 0.5,
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(-0.1, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(-0.7, 0, 0, 0)),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(60, new Vector4D(0, 0, -1.5, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN) * AnimationSourceEnum.JUMP_GATE_SIZE,
								new VectorKeyframe(720, Vector4D.Zero),
							}
						},
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Runes.Time5" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 13 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(-0.25, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(-1.75, 0, 0, 0)),
							},
						}.Overlay(MagicSigil_Animation),
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Circle" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 14 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(0.15, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(1.05, 0, 0, 0)),
							},
						}.Overlay(MagicSigil_Animation),
					},

					// Rear Particles
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Time5" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 15 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.5, 0.625, 1, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(60, new Vector4D(0.5, 0.625, 1, 0.01)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE),
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(-0.25, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(-1.75, 0, 0, 0)),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(60, new Vector4D(0, 0, 0.25, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN) * AnimationSourceEnum.JUMP_GATE_SIZE,
								new VectorKeyframe(720, Vector4D.Zero),
							}
						},
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Runes.Time5" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 16 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(-0.5, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(-3.5, 0, 0, 0)),
							},
						}.Overlay(MagicSigil_Animation),
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Circle" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 17 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(0.25, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(1.75, 0, 0, 0)),
							},
						}.Overlay(MagicSigil_Animation),
					},

					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Hexad6" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 18 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.5, 0.625, 1, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(60, new Vector4D(0.5, 0.625, 1, 0.01)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE) * 0.375,
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(0.1, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(0.7, 0, 0, 0)),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(60, new Vector4D(0, 0, 0.375, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN) * AnimationSourceEnum.JUMP_GATE_SIZE,
								new VectorKeyframe(720, Vector4D.Zero),
							}
						},
					},

					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Entropy13" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 19 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.5, 0.625, 1, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(60, new Vector4D(0.5, 0.625, 1, 0.01)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE) * 0.5,
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(-0.25, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(-1.75, 0, 0, 0)),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(60, new Vector4D(0, 0, 0.5, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN) * AnimationSourceEnum.JUMP_GATE_SIZE,
								new VectorKeyframe(720, Vector4D.Zero),
							}
						},
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Runes.Entropy13" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 20 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(-0.5, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(-3.5, 0, 0, 0)),
							},
						}.Overlay(MagicSigil_Animation),
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Circle" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 21 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(0.25, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(1.75, 0, 0, 0)),
							},
						}.Overlay(MagicSigil_Animation),
					},

					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Trinity3" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 22 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.5, 0.625, 1, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(60, new Vector4D(0.5, 0.625, 1, 0.01)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE),
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(-0.5, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(-3.5, 0, 0, 0)),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(60, new Vector4D(0, 0, 1, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN) * AnimationSourceEnum.JUMP_GATE_SIZE,
								new VectorKeyframe(720, Vector4D.Zero),
							}
						},
					},

					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Space7" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 23 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.5, 0.625, 1, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(60, new Vector4D(0.5, 0.625, 1, 0.01)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE) * 0.75,
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(-0.05, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(-0.35, 0, 0, 0)),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(60, new Vector4D(0, 0, 1.25, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN) * AnimationSourceEnum.JUMP_GATE_SIZE,
								new VectorKeyframe(720, Vector4D.Zero),
							}
						},
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Runes.Space7" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 24 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(0.025, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(0.175, 0, 0, 0)),
							},
						}.Overlay(MagicSigil_Animation),
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Circle" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 25 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(-0.1, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(-0.7, 0, 0, 0)),
							},
						}.Overlay(MagicSigil_Animation),
					},
				},

				AntiNodeParticles = new ParticleDef[] {
					/* Sigils */
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.MagicSigil.Entropy13" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 31 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.5, 0.625, 1, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(60, new Vector4D(0.5, 0.625, 1, 0.01)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE) * 0.625,
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(0.1, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(0.7, 0, 0, 0)),
							},
						},
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Runes.Entropy13" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 32 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(0.05, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(0.35, 0, 0, 0)),
							},
						}.Overlay(MagicSigil_Animation),
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Circle" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 33 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(-0.1, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(-0.7, 0, 0, 0)),
							},
						}.Overlay(MagicSigil_Animation),
					},
					
					// Forward Particles
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Space7" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 34 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.5, 0.625, 1, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(60, new Vector4D(0.5, 0.625, 1, 0.01)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE),
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(-0.1, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(-0.7, 0, 0, 0)),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(60, new Vector4D(0, 0, -0.25, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN) * AnimationSourceEnum.JUMP_GATE_SIZE,
								new VectorKeyframe(720, Vector4D.Zero),
							}
						},
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Runes.Space7" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 35 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(-0.05, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(-0.35, 0, 0, 0)),
							},
						}.Overlay(MagicSigil_Animation),
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Circle" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 36 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(0.1, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(0.7, 0, 0, 0)),
							},
						}.Overlay(MagicSigil_Animation),
					},

					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Trinity3" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 37 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.5, 0.625, 1, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(60, new Vector4D(0.5, 0.625, 1, 0.01)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE) * 0.625,
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(-0.25, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(-1.75, 0, 0, 0)),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(60, new Vector4D(0, 0, -0.5, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN) * AnimationSourceEnum.JUMP_GATE_SIZE,
								new VectorKeyframe(720, Vector4D.Zero),
							}
						},
					},

					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Hexad6" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 38 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.5, 0.625, 1, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(60, new Vector4D(0.5, 0.625, 1, 0.01)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE) * 0.5,
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(-0.5, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(-3.5, 0, 0, 0)),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(60, new Vector4D(0, 0, -0.625, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN) * AnimationSourceEnum.JUMP_GATE_SIZE,
								new VectorKeyframe(720, Vector4D.Zero),
							}
						},
					},

					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Space7" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 39 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.5, 0.625, 1, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(60, new Vector4D(0.5, 0.625, 1, 0.01)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE) * 0.875,
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(0.15, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(1.05, 0, 0, 0)),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(60, new Vector4D(0, 0, -1, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN) * AnimationSourceEnum.JUMP_GATE_SIZE,
								new VectorKeyframe(720, Vector4D.Zero),
							}
						},
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Runes.Space7" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 40 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(-0.025, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(-0.175, 0, 0, 0)),
							},
						}.Overlay(MagicSigil_Animation),
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Circle" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 41 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(0.05, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(0.35, 0, 0, 0)),
							},
						}.Overlay(MagicSigil_Animation),
					},

					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Time5" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 42 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.5, 0.625, 1, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(60, new Vector4D(0.5, 0.625, 1, 0.01)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE) * 0.5,
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(-0.1, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(-0.7, 0, 0, 0)),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(60, new Vector4D(0, 0, -1.5, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN) * AnimationSourceEnum.JUMP_GATE_SIZE,
								new VectorKeyframe(720, Vector4D.Zero),
							}
						},
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Runes.Time5" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 43 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(-0.25, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(-1.75, 0, 0, 0)),
							},
						}.Overlay(MagicSigil_Animation),
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Circle" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 44 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(0.15, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(1.05, 0, 0, 0)),
							},
						}.Overlay(MagicSigil_Animation),
					},

					// Rear Particles
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Time5" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 45 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.5, 0.625, 1, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(60, new Vector4D(0.5, 0.625, 1, 0.01)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE),
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(-0.25, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(-1.75, 0, 0, 0)),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(60, new Vector4D(0, 0, 0.25, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN) * AnimationSourceEnum.JUMP_GATE_SIZE,
								new VectorKeyframe(720, Vector4D.Zero),
							}
						},
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Runes.Time5" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 46 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(-0.5, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(-3.5, 0, 0, 0)),
							},
						}.Overlay(MagicSigil_Animation),
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Circle" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 47 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(0.25, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(1.75, 0, 0, 0)),
							},
						}.Overlay(MagicSigil_Animation),
					},

					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Hexad6" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 48 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.5, 0.625, 1, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(60, new Vector4D(0.5, 0.625, 1, 0.01)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE) * 0.375,
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(0.1, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(0.7, 0, 0, 0)),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(60, new Vector4D(0, 0, 0.375, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN) * AnimationSourceEnum.JUMP_GATE_SIZE,
								new VectorKeyframe(720, Vector4D.Zero),
							}
						},
					},

					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Entropy13" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 49 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.5, 0.625, 1, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(60, new Vector4D(0.5, 0.625, 1, 0.01)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE) * 0.5,
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(-0.25, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(-1.75, 0, 0, 0)),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(60, new Vector4D(0, 0, 0.5, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN) * AnimationSourceEnum.JUMP_GATE_SIZE,
								new VectorKeyframe(720, Vector4D.Zero),
							}
						},
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Runes.Entropy13" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 50 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(-0.5, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(-3.5, 0, 0, 0)),
							},
						}.Overlay(MagicSigil_Animation),
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Circle" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 51 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(0.25, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(1.75, 0, 0, 0)),
							},
						}.Overlay(MagicSigil_Animation),
					},

					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Trinity3" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 52 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.5, 0.625, 1, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(60, new Vector4D(0.5, 0.625, 1, 0.01)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE),
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(-0.5, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(-3.5, 0, 0, 0)),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(60, new Vector4D(0, 0, 1, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN) * AnimationSourceEnum.JUMP_GATE_SIZE,
								new VectorKeyframe(720, Vector4D.Zero),
							}
						},
					},

					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Space7" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 53 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.5, 0.625, 1, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(60, new Vector4D(0.5, 0.625, 1, 0.01)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE) * 0.75,
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(-0.05, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(-0.35, 0, 0, 0)),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(60, new Vector4D(0, 0, 1.25, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN) * AnimationSourceEnum.JUMP_GATE_SIZE,
								new VectorKeyframe(720, Vector4D.Zero),
							}
						},
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Runes.Space7" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 54 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(0.025, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(0.175, 0, 0, 0)),
							},
						}.Overlay(MagicSigil_Animation),
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Circle" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 55 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(420, new Vector4D(-0.1, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(720, new Vector4D(-0.7, 0, 0, 0)),
							},
						}.Overlay(MagicSigil_Animation),
					},
				},

				NodeSounds = new SoundDef[] {
					new SoundDef {
						SoundNames = new string[] { "IOTA.MagicSigil.JumpGateJumping" },
					},
					new SoundDef {
						SoundNames = new string[] { "IOTA.MagicSigil.JumpGateJumpedInbound" },
						Duration = 210,
						StartTime = 510,
					},
				},

				AntiNodeSounds = new SoundDef[] {
					new SoundDef {
						SoundNames = new string[] { "IOTA.MagicSigil.JumpGateJumping" },
					},
					new SoundDef {
						SoundNames = new string[] { "IOTA.MagicSigil.JumpGateJumpedInbound" },
						Duration = 210,
						StartTime = 510,
					},
				},

				DriveEmissiveColor = new DriveEmissiveColorDef
				{
					Duration = 60,
					EmissiveColor = new Color(62, 133, 247),
					Brightness = 10,
				},
			},

			JumpedAnimationDef = new JumpGateJumpedAnimationDef
			{
				Duration = 300,

				NodeParticles = new ParticleDef[] {
					/* Sigils */
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.MagicSigil.Entropy13" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 1 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(240, new Vector4D(0.5, 0.625, 1, 0.01), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0.5, 0.625, 1, 0)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE) * 0.625,
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.7, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
						},
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Runes.Entropy13" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 2 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.35, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
						}.Overlay(MagicSigil_Animation),
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Circle" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 3 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(-0.7, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
						}.Overlay(MagicSigil_Animation),
					},
					
					// Forward Particles
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Space7" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 4 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(240, new Vector4D(0.5, 0.625, 1, 0.01), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0.5, 0.625, 1, 0)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE),
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(-0.7, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0, 0, 0.25, 0)) * AnimationSourceEnum.JUMP_GATE_SIZE,
							}
						},
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Runes.Space7" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 5 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(-0.35, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
						}.Overlay(MagicSigil_Animation),
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Circle" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 6 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.7, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
						}.Overlay(MagicSigil_Animation),
					},

					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Trinity3" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 7 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(240, new Vector4D(0.5, 0.625, 1, 0.01), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0.5, 0.625, 1, 0)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE) * 0.625,
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(-1.75, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0, 0, 0.5, 0)) * AnimationSourceEnum.JUMP_GATE_SIZE,
							}
						},
					},

					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Hexad6" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 8 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(240, new Vector4D(0.5, 0.625, 1, 0.01), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0.5, 0.625, 1, 0)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE) * 0.5,
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(-3.5, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0, 0, 0.625, 0)) * AnimationSourceEnum.JUMP_GATE_SIZE,
							}
						},
					},

					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Space7" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 9 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(240, new Vector4D(0.5, 0.625, 1, 0.01), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0.5, 0.625, 1, 0)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE) * 0.875,
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(1.05, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0, 0, 1, 0)) * AnimationSourceEnum.JUMP_GATE_SIZE,
							}
						},
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Runes.Space7" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 10 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(-0.175, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
						}.Overlay(MagicSigil_Animation),
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Circle" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 11 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.35, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
						}.Overlay(MagicSigil_Animation),
					},

					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Time5" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 12 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(240, new Vector4D(0.5, 0.625, 1, 0.01), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0.5, 0.625, 1, 0)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE) * 0.5,
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(-0.7, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0, 0, 1.5, 0)) * AnimationSourceEnum.JUMP_GATE_SIZE,
							}
						},
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Runes.Time5" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 13 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(-1.75, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
						}.Overlay(MagicSigil_Animation),
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Circle" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 14 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(1.05, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
						}.Overlay(MagicSigil_Animation),
					},

					// Rear Particles
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Time5" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 15 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(240, new Vector4D(0.5, 0.625, 1, 0.01), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0.5, 0.625, 1, 0)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE),
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(-1.75, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0, 0, -0.25, 0)) * AnimationSourceEnum.JUMP_GATE_SIZE,
							}
						},
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Runes.Time5" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 16 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(-3.5, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
						}.Overlay(MagicSigil_Animation),
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Circle" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 17 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(1.75, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
						}.Overlay(MagicSigil_Animation),
					},

					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Hexad6" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 18 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(240, new Vector4D(0.5, 0.625, 1, 0.01), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0.5, 0.625, 1, 0)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE) * 0.375,
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.7, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0, 0, -0.375, 0)) * AnimationSourceEnum.JUMP_GATE_SIZE,
							}
						},
					},

					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Entropy13" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 19 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(240, new Vector4D(0.5, 0.625, 1, 0.01), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0.5, 0.625, 1, 0)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE) * 0.5,
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(-1.75, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0, 0, -0.5, 0)) * AnimationSourceEnum.JUMP_GATE_SIZE,
							}
						},
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Runes.Entropy13" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 20 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(-3.5, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
						}.Overlay(MagicSigil_Animation),
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Circle" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 21 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(1.75, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
						}.Overlay(MagicSigil_Animation),
					},

					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Trinity3" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 22 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(240, new Vector4D(0.5, 0.625, 1, 0.01), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0.5, 0.625, 1, 0)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE),
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(-3.5, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0, 0, -1, 0)) * AnimationSourceEnum.JUMP_GATE_SIZE,
							}
						},
					},

					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Space7" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 23 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(240, new Vector4D(0.5, 0.625, 1, 0.01), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0.5, 0.625, 1, 0)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE) * 0.75,
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(-0.35, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0, 0, -1.25, 0)) * AnimationSourceEnum.JUMP_GATE_SIZE,
							}
						},
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Runes.Space7" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 24 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.175, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
						}.Overlay(MagicSigil_Animation),
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Circle" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 25 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(-0.7, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
						}.Overlay(MagicSigil_Animation),
					},
				},

				AntiNodeParticles = new ParticleDef[] {
					/* Sigils */
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.MagicSigil.Entropy13" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 31 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(240, new Vector4D(0.5, 0.625, 1, 0.01), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0.5, 0.625, 1, 0)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE) * 0.625,
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.7, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
						},
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Runes.Entropy13" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 32 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.35, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
						}.Overlay(MagicSigil_Animation),
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Circle" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 33 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(-0.7, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
						}.Overlay(MagicSigil_Animation),
					},
					
					// Forward Particles
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Space7" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 34 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(240, new Vector4D(0.5, 0.625, 1, 0.01), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0.5, 0.625, 1, 0)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE),
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(-0.7, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0, 0, 0.25, 0)) * AnimationSourceEnum.JUMP_GATE_SIZE,
							}
						},
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Runes.Space7" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 35 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(-0.35, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
						}.Overlay(MagicSigil_Animation),
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Circle" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 36 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.7, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
						}.Overlay(MagicSigil_Animation),
					},

					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Trinity3" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 37 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(240, new Vector4D(0.5, 0.625, 1, 0.01), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0.5, 0.625, 1, 0)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE) * 0.625,
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(-1.75, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0, 0, 0.5, 0)) * AnimationSourceEnum.JUMP_GATE_SIZE,
							}
						},
					},

					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Hexad6" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 38 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(240, new Vector4D(0.5, 0.625, 1, 0.01), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0.5, 0.625, 1, 0)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE) * 0.5,
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(-3.5, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0, 0, 0.625, 0)) * AnimationSourceEnum.JUMP_GATE_SIZE,
							}
						},
					},

					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Space7" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 39 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(240, new Vector4D(0.5, 0.625, 1, 0.01), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0.5, 0.625, 1, 0)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE) * 0.875,
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(1.05, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0, 0, 1, 0)) * AnimationSourceEnum.JUMP_GATE_SIZE,
							}
						},
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Runes.Space7" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 40 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(-0.175, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
						}.Overlay(MagicSigil_Animation),
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Circle" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 41 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.35, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
						}.Overlay(MagicSigil_Animation),
					},

					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Time5" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 42 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(240, new Vector4D(0.5, 0.625, 1, 0.01), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0.5, 0.625, 1, 0)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE) * 0.5,
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(-0.7, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0, 0, 1.5, 0)) * AnimationSourceEnum.JUMP_GATE_SIZE,
							}
						},
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Runes.Time5" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 43 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(-1.75, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
						}.Overlay(MagicSigil_Animation),
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Circle" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 44 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(1.05, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
						}.Overlay(MagicSigil_Animation),
					},

					// Rear Particles
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Time5" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 45 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(240, new Vector4D(0.5, 0.625, 1, 0.01), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0.5, 0.625, 1, 0)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE),
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(-1.75, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0, 0, -0.25, 0)) * AnimationSourceEnum.JUMP_GATE_SIZE,
							}
						},
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Runes.Time5" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 46 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(-3.5, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
						}.Overlay(MagicSigil_Animation),
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Circle" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 47 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(1.75, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
						}.Overlay(MagicSigil_Animation),
					},

					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Hexad6" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 48 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(240, new Vector4D(0.5, 0.625, 1, 0.01), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0.5, 0.625, 1, 0)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE) * 0.375,
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.7, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0, 0, -0.375, 0)) * AnimationSourceEnum.JUMP_GATE_SIZE,
							}
						},
					},

					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Entropy13" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 49 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(240, new Vector4D(0.5, 0.625, 1, 0.01), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0.5, 0.625, 1, 0)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE) * 0.5,
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(-1.75, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0, 0, -0.5, 0)) * AnimationSourceEnum.JUMP_GATE_SIZE,
							}
						},
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Runes.Entropy13" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 50 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(-3.5, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
						}.Overlay(MagicSigil_Animation),
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Circle" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 51 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(1.75, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
						}.Overlay(MagicSigil_Animation),
					},

					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Trinity3" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 52 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(240, new Vector4D(0.5, 0.625, 1, 0.01), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0.5, 0.625, 1, 0)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE),
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(-3.5, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0, 0, -1, 0)) * AnimationSourceEnum.JUMP_GATE_SIZE,
							}
						},
					},

					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Space7" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 53 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(240, new Vector4D(0.5, 0.625, 1, 0.01), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0.5, 0.625, 1, 0)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE) * 0.75,
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(-0.35, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0, 0, -1.25, 0)) * AnimationSourceEnum.JUMP_GATE_SIZE,
							}
						},
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Runes.Space7" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 54 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.175, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
						}.Overlay(MagicSigil_Animation),
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Circle" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 55 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(-0.7, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
						}.Overlay(MagicSigil_Animation),
					},
				},

				TravelEffects = new ParticleDef[] {
					new ParticleDef {
						Duration = 240,
						ParticleNames = new string[] { "IOTA.TravelEffect.WarpField" },
						ParticleOrientation = new ParticleOrientationDef(ParticleOrientationEnum.GATE_TRUE_ENDPOINT_NORMAL),
						ParticleOffset = new Vector3D(0, 0, -250),

						Animations = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.5, 0.625, 1, 1), EasingCurveEnum.EXPONENTIAL, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(210, new Vector4D(0.5, 0.625, 1, 0)),
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

				BeamPulse = new BeamPulseDef
				{
					BeamBrightness = 20,
					BeamFrequency = 512,
					TravelTime = 240,

					Animations = new AttributeAnimationDef
					{
						ParticleColorAnimation = new VectorKeyframe[] {
							new VectorKeyframe(0, Vector4D.One, ratio_type: RatioTypeEnum.ENDPOINT_DISTANCE, lower: new Vector4D(0.5, 0.625, 1, 1), upper: new Vector4D(1, 0.625, 0.5, 1)),
							new VectorKeyframe(240, Vector4D.Zero),
						},
						ParticleRadiusAnimation = new DoubleKeyframe[] {
							new DoubleKeyframe(0, 0d, ratio_type: RatioTypeEnum.RANDOM, lower: 0.25, upper: 1) * AnimationSourceEnum.JUMP_GATE_SIZE / 25,
						},
						BeamOffsetAnimation = new DoubleKeyframe[] {
							new DoubleKeyframe(0, 0d, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
							new DoubleKeyframe(300, -1.25) * AnimationSourceEnum.JUMP_GATE_SIZE,
						},
					},
				},

				NodeSounds = new SoundDef[] {
					new SoundDef {
						SoundNames = new string[] { "IOTA.MagicSigil.JumpGateJumpedOutbound" },
						Duration = 300,
					},
				},

				AntiNodeSounds = new SoundDef[] {
					new SoundDef {
						SoundNames = new string[] { "IOTA.MagicSigil.JumpGateJumpedOutbound" },
						Duration = 300,
					},
				},
			},

			FailedAnimationDef = new JumpGateFailedAnimationDef
			{
				Duration = 300,

				NodeParticles = new ParticleDef[] {
					/* Sigils */
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.MagicSigil.Entropy13" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 1 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(240, new Vector4D(0.5, 0.625, 1, 0.01), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0.5, 0.625, 1, 0)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE) * 0.625,
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.7, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
						},
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Runes.Entropy13" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 2 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.35, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
						}.Overlay(MagicSigil_Animation),
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Circle" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 3 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(-0.7, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
						}.Overlay(MagicSigil_Animation),
					},
					
					// Forward Particles
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Space7" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 4 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(240, new Vector4D(0.5, 0.625, 1, 0.01), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0.5, 0.625, 1, 0)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE),
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(-0.7, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0, 0, 0.25, 0)) * AnimationSourceEnum.JUMP_GATE_SIZE,
							}
						},
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Runes.Space7" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 5 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(-0.35, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
						}.Overlay(MagicSigil_Animation),
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Circle" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 6 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.7, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
						}.Overlay(MagicSigil_Animation),
					},

					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Trinity3" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 7 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(240, new Vector4D(0.5, 0.625, 1, 0.01), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0.5, 0.625, 1, 0)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE) * 0.625,
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(-1.75, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0, 0, 0.5, 0)) * AnimationSourceEnum.JUMP_GATE_SIZE,
							}
						},
					},

					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Hexad6" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 8 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(240, new Vector4D(0.5, 0.625, 1, 0.01), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0.5, 0.625, 1, 0)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE) * 0.5,
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(-3.5, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0, 0, 0.625, 0)) * AnimationSourceEnum.JUMP_GATE_SIZE,
							}
						},
					},

					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Space7" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 9 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(240, new Vector4D(0.5, 0.625, 1, 0.01), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0.5, 0.625, 1, 0)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE) * 0.875,
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(1.05, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0, 0, 1, 0)) * AnimationSourceEnum.JUMP_GATE_SIZE,
							}
						},
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Runes.Space7" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 10 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(-0.175, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
						}.Overlay(MagicSigil_Animation),
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Circle" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 11 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.35, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
						}.Overlay(MagicSigil_Animation),
					},

					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Time5" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 12 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(240, new Vector4D(0.5, 0.625, 1, 0.01), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0.5, 0.625, 1, 0)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE) * 0.5,
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(-0.7, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0, 0, 1.5, 0)) * AnimationSourceEnum.JUMP_GATE_SIZE,
							}
						},
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Runes.Time5" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 13 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(-1.75, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
						}.Overlay(MagicSigil_Animation),
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Circle" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 14 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(1.05, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
						}.Overlay(MagicSigil_Animation),
					},

					// Rear Particles
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Time5" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 15 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(240, new Vector4D(0.5, 0.625, 1, 0.01), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0.5, 0.625, 1, 0)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE),
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(-1.75, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0, 0, -0.25, 0)) * AnimationSourceEnum.JUMP_GATE_SIZE,
							}
						},
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Runes.Time5" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 16 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(-3.5, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
						}.Overlay(MagicSigil_Animation),
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Circle" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 17 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(1.75, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
						}.Overlay(MagicSigil_Animation),
					},

					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Hexad6" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 18 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(240, new Vector4D(0.5, 0.625, 1, 0.01), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0.5, 0.625, 1, 0)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE) * 0.375,
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.7, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0, 0, -0.375, 0)) * AnimationSourceEnum.JUMP_GATE_SIZE,
							}
						},
					},

					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Entropy13" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 19 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(240, new Vector4D(0.5, 0.625, 1, 0.01), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0.5, 0.625, 1, 0)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE) * 0.5,
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(-1.75, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0, 0, -0.5, 0)) * AnimationSourceEnum.JUMP_GATE_SIZE,
							}
						},
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Runes.Entropy13" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 20 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(-3.5, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
						}.Overlay(MagicSigil_Animation),
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Circle" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 21 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(1.75, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
						}.Overlay(MagicSigil_Animation),
					},

					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Trinity3" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 22 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(240, new Vector4D(0.5, 0.625, 1, 0.01), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0.5, 0.625, 1, 0)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE),
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(-3.5, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0, 0, -1, 0)) * AnimationSourceEnum.JUMP_GATE_SIZE,
							}
						},
					},

					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Space7" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 23 },

						Animations = MagicSigil_Animation = new AttributeAnimationDef {
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(240, new Vector4D(0.5, 0.625, 1, 0.01), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0.5, 0.625, 1, 0)),
							},
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE) * 0.75,
							},
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(-0.35, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
							ParticleOffsetAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.Zero, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(300, new Vector4D(0, 0, -1.25, 0)) * AnimationSourceEnum.JUMP_GATE_SIZE,
							}
						},
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Runes.Space7" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 24 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(0.175, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
						}.Overlay(MagicSigil_Animation),
					},
					new ParticleDef {
						ParticleNames = new string[]{ "IOTA.MagicSigil.Circle" },
						DirtifyEffect = true,
						TransientIDs = new byte[] { 25 },

						Animations = new AttributeAnimationDef {
							ParticleRotationSpeedAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, new Vector4D(-0.7, 0, 0, 0), EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_OUT),
								new VectorKeyframe(240, Vector4D.Zero),
							},
						}.Overlay(MagicSigil_Animation),
					},
				},

				NodeSounds = new SoundDef[] {
					new SoundDef {
						SoundNames = new string[] { "IOTA.MagicSigil.JumpGateFailed" },
						Duration = 210,
					},
				},

				AntiNodeSounds = new SoundDef[] {
					new SoundDef {
						SoundNames = new string[] { "IOTA.MagicSigil.JumpGateFailed" },
						Duration = 210,
					},
				},
			},
		};
	}
}
