using IOTA.ModularJumpGates.Extensions;
using System;
using VRageMath;

namespace IOTA.ModularJumpGates.Animations
{
	public partial class AnimationDefinitions
	{
		static ParticleDef[] IMJG_Catapult_DriveParticlesJumping;
		static ParticleDef[] IMJG_Catapult_DriveParticlesFailed;

		public AnimationDef Catapult = new AnimationDef("Catapult", "An animation loosely inspired by the catapult from Star Trek Voyager")
		{
			Enabled = true,
			
			JumpingAnimationDef = new JumpGateJumpingAnimationDef {
				Duration = 840,

				NodeSounds = new SoundDef[] {
					new SoundDef {
						SoundNames = new string[] { "IOTA.Catapult.JumpGateJumping" },
					}
				},

				AntiNodeSounds = new SoundDef[] {
					new SoundDef {
						SoundNames = new string[] { "IOTA.Catapult.AntiJumpGateJumping" },
					},
					new SoundDef {
						StartTime = 690,
						Duration = 240,
						SoundNames = new string[] { "IOTA.TravelEffects.Standard_0" },
					},
				},

				DriveEmissiveColor = new DriveEmissiveColorDef
				{
					Duration = 300,
					EmissiveColor = Color.CornflowerBlue,
					Brightness = 5,
				},

				NodeParticles = new ParticleDef[] {
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Catapult.NodeParticleJumping.CatapultFuzz" },
						Animations = new AttributeAnimationDef {
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE),
							},
						},
					},
				},

				AntiNodeParticles = new ParticleDef[] {
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Catapult.NodeParticleJumping.CatapultFuzz" },
						ParticleOrientation = new ParticleOrientationDef(ParticleOrientationEnum.GATE_ENDPOINT_NORMAL, MatrixD.CreateFromYawPitchRoll(0, Math.PI, 0)),
						Animations = new AttributeAnimationDef {
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_ANTIGATE_SIZE),
							},
						},
					},
				},

				PerDriveParticles = IMJG_Catapult_DriveParticlesJumping = new ParticleDef[] {
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Tesseract.DriveParticle" },
						StartTime = 0,
						ParticleOffset = new Vector3D(0, 0, 10),

						Animations = new AttributeAnimationDef {
							ParticleBirthAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, 0d, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new DoubleKeyframe(300, 1),
							},
						},
					},
				},

				PerAntiDriveParticles = IMJG_Catapult_DriveParticlesJumping,

				DriveEntityLock = new DriveEntityLockDef {
					LockDelayShift = LockDelayShiftEnum.FIXED,
					LockDelayEasingType = EasingTypeEnum.EASE_OUT,
					LockDelayEasingCurve = EasingCurveEnum.CIRCULAR,
					MinLockTime = 300,
					MaxLockTime = 300,
					InitialRotation = new Vector3D(60, 0, 0),
					StartTime = 180,

					EntityLockParticles = new ParticleDef[] {
						new ParticleDef {
							ParticleNames = new string[] { "IOTA.Catapult.DriveParticleJumping.EntityLockLine" },
							CleanOnEffectEnd = false,
							Animations = new AttributeAnimationDef {
								ParticleScaleAnimation = new DoubleKeyframe[] {
									new DoubleKeyframe(0, AnimationSourceEnum.DISTANCE_THIS_TO_ENTITY),
								},
								ParticleBirthAnimation = new DoubleKeyframe[] {
									new DoubleKeyframe(0, 0d, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
									new DoubleKeyframe(60, AnimationSourceEnum.DISTANCE_THIS_TO_ENTITY) / 25,
								},
								ParticleRadiusAnimation = new DoubleKeyframe[] {
									new DoubleKeyframe(0, 25) / AnimationSourceEnum.DISTANCE_THIS_TO_ENTITY,
								},
								ParticleVelocityAnimation = new DoubleKeyframe[] {
									new DoubleKeyframe(0, 25) / AnimationSourceEnum.DISTANCE_THIS_TO_ENTITY,
								},
							}
						}
					}
				},
			},

			JumpedAnimationDef = new JumpGateJumpedAnimationDef {
				Duration = 300,
				TravelTime = 300,

				NodeSounds = new SoundDef[] {
					new SoundDef {
						SoundNames = new string[] { "IOTA.Catapult.JumpGateJumpedOutbound" },
					}
				},

				AntiNodeSounds = new SoundDef[] {
					new SoundDef {
						StartTime = 120,
						SoundNames = new string[] { "IOTA.Catapult.JumpGateJumpedInbound" },
					},
					new SoundDef {
						Duration = 240,
						SoundNames = new string[] { "IOTA.TravelEffects.Standard_0" },
					},
				},

				NodeParticles = new ParticleDef[] {
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Catapult.NodeParticleFailed.CatapultFuzz" },
						Animations = new AttributeAnimationDef {
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE),
							},
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.One, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(300, Vector4D.Zero),
							},
						},
					},
				},

				AntiNodeParticles = new ParticleDef[] {
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Catapult.NodeParticleFailed.CatapultFuzz" },
						ParticleOrientation = new ParticleOrientationDef(ParticleOrientationEnum.GATE_ENDPOINT_NORMAL, MatrixD.CreateFromYawPitchRoll(0, Math.PI, 0)),
						Animations = new AttributeAnimationDef {
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE),
							},
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.One, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(300, Vector4D.Zero),
							},
						},
					},
				},

				TravelEffects = new ParticleDef[] {
					new ParticleDef {
						Duration = 240,
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
						Duration = 240,
						SoundNames = new string[] { "IOTA.TravelEffects.Standard_0" },
						Distance = 1000,
					},
				},

				BeamPulse = new BeamPulseDef {
					TravelTime = 240,
					BeamBrightness = 20,
					BeamFrequency = 0,

					Animations = new AttributeAnimationDef
					{
						ParticleColorAnimation = new VectorKeyframe[] {
							new VectorKeyframe(0, Color.CornflowerBlue.ToVector4D()),
							new VectorKeyframe(300, Vector4D.Zero),
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
								ParticleColorAnimation = new VectorKeyframe[] {
									new VectorKeyframe(0, Vector4D.One, ratio_type: RatioTypeEnum.ENDPOINT_DISTANCE, lower: Color.CornflowerBlue.ToVector4D(), upper: new Color(130, 83, 207).ToVector4D()),
									new VectorKeyframe(120, Vector4D.One, ratio_type: RatioTypeEnum.ENDPOINT_DISTANCE, lower: Color.CornflowerBlue.ToVector4D(), upper: new Color(130, 83, 207).ToVector4D()),
									new VectorKeyframe(300, Vector4D.Zero),
								},
							},
						},
					}
				},

				PerDriveParticles = new ParticleDef[] {
					new ParticleDef {
						Duration = 300,
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
						Duration = 300,
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

			FailedAnimationDef = new JumpGateFailedAnimationDef {
				Duration = 300,

				NodeSounds = new SoundDef[] {
					new SoundDef {
						SoundNames = new string[] { "IOTA.Catapult.JumpGateFailed" },
					}
				},

				AntiNodeSounds = new SoundDef[] {
					new SoundDef {
						SoundNames = new string[] { "IOTA.Catapult.JumpGateFailed" },
					}
				},

				DriveEmissiveColor = new DriveEmissiveColorDef
				{
					Duration = 300,
					EmissiveColor = Color.Black,
					Brightness = 5,
				},

				NodeParticles = new ParticleDef[] {
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Catapult.NodeParticleFailed.CatapultFuzz" },
						Animations = new AttributeAnimationDef {
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_GATE_SIZE),
							},
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.One, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(300, Vector4D.Zero),
							},
						},
					},
				},

				AntiNodeParticles = new ParticleDef[] {
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Catapult.NodeParticleFailed.CatapultFuzz" },
						Animations = new AttributeAnimationDef {
							ParticleScaleAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, AnimationSourceEnum.JUMP_ANTIGATE_SIZE),
							},
							ParticleColorAnimation = new VectorKeyframe[] {
								new VectorKeyframe(0, Vector4D.One, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new VectorKeyframe(300, Vector4D.Zero),
							},
						},
					},
				},

				PerDriveParticles = IMJG_Catapult_DriveParticlesFailed = new ParticleDef[] {
					new ParticleDef {
						ParticleNames = new string[] { "IOTA.Tesseract.DriveParticle" },
						StartTime = 0,
						ParticleOffset = new Vector3D(0, 0, 10),

						Animations = new AttributeAnimationDef {
							ParticleBirthAnimation = new DoubleKeyframe[] {
								new DoubleKeyframe(0, 1, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN),
								new DoubleKeyframe(300, 0d),
							},
						},
					},
				},

				PerAntiDriveParticles = IMJG_Catapult_DriveParticlesFailed,

				DriveEntityLock = new DriveEntityLockDef
				{
					LockDelayShift = LockDelayShiftEnum.FIXED,
					LockDelayEasingType = EasingTypeEnum.EASE_OUT,
					LockDelayEasingCurve = EasingCurveEnum.CIRCULAR,
					MinLockTime = 300,
					MaxLockTime = 300,
					InitialRotation = new Vector3D(60, 0, 0),
					RatioModifier = -1,

					EntityLockParticles = new ParticleDef[] {
						new ParticleDef {
							ParticleNames = new string[] { "IOTA.Catapult.DriveParticleJumping.EntityLockLine" },
							CleanOnEffectEnd = false,
							Animations = new AttributeAnimationDef {
								ParticleScaleAnimation = new DoubleKeyframe[] {
									new DoubleKeyframe(0, AnimationSourceEnum.DISTANCE_THIS_TO_ENTITY),
								},
								ParticleBirthAnimation = new DoubleKeyframe[] {
									new DoubleKeyframe(0, AnimationSourceEnum.DISTANCE_THIS_TO_ENTITY, EasingCurveEnum.CIRCULAR, EasingTypeEnum.EASE_IN) / 25,
									new DoubleKeyframe(300, 0d),
								},
								ParticleRadiusAnimation = new DoubleKeyframe[] {
									new DoubleKeyframe(0, 25) / AnimationSourceEnum.DISTANCE_THIS_TO_ENTITY,
								},
								ParticleVelocityAnimation = new DoubleKeyframe[] {
									new DoubleKeyframe(0, 25) / AnimationSourceEnum.DISTANCE_THIS_TO_ENTITY,
								},
							}
						}
					}
				},
			},
		};
	}
}
