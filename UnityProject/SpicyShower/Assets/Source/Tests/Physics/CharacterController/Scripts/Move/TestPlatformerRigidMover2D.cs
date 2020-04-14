using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using FsCheck;
using UnityEngine;

using Object = UnityEngine.Object;


namespace SpicyShower.Physics.CharacterController.Tests
{
    [TestOf(typeof(PlatformerRigidMover2D))]
    public class TestPlatformerRigidMover2D
    {
        private struct ValidCalculateVelocityInput
        {
            public float currentVelocity, maxSpeed, timeToMaxSpeed, timeToStop, deltaTime;
            public int targetDirection;

            public override string ToString()
            {
                return $"{nameof(currentVelocity)}: {currentVelocity}"
                    + "\n" + $"{nameof(targetDirection)}: {targetDirection}"
                    + "\n" + $"{nameof(maxSpeed)}: {maxSpeed}"
                    + "\n" + $"{nameof(timeToMaxSpeed)}: {timeToMaxSpeed}"
                    + "\n" + $"{nameof(timeToStop)}: {timeToStop}"
                    + "\n" + $"{nameof(deltaTime)}: {deltaTime}";
            }

            private static readonly Gen<float> normalFloatGen = Arb.Generate<NormalFloat>().Select(x => (float)x.Get);
            private static readonly Gen<float> nonNegativeNormalFloatGen = normalFloatGen.Select(x => Mathf.Abs(x));
            private static readonly Gen<float> positiveNormalFloatGen = nonNegativeNormalFloatGen.Where(x => x > 0);

            public static readonly Gen<ValidCalculateVelocityInput> generator =
                from currentVelocity in normalFloatGen
                from targetDirection in Gen.Choose(-1, 1)
                from maxSpeed in nonNegativeNormalFloatGen
                from timeToMaxSpeed in nonNegativeNormalFloatGen
                from timeToStop in nonNegativeNormalFloatGen
                from deltaTime in positiveNormalFloatGen
                select new ValidCalculateVelocityInput
                {
                    currentVelocity = currentVelocity,
                    targetDirection = targetDirection,
                    maxSpeed = maxSpeed,
                    timeToMaxSpeed = timeToMaxSpeed,
                    timeToStop = timeToStop,
                    deltaTime = deltaTime
                };

            public static IEnumerable<ValidCalculateVelocityInput> Shrinker(ValidCalculateVelocityInput input)
            {
                return
                    from currentVelocity in Arb.Shrink(input.currentVelocity)
                    from targetDirection in Arb.Shrink(input.targetDirection) where -1 <= targetDirection && targetDirection <= 1
                    from maxSpeed in Arb.Shrink(input.maxSpeed) where maxSpeed >= 0
                    from timeToMaxSpeed in Arb.Shrink(input.timeToMaxSpeed) where timeToMaxSpeed >= 0
                    from timeToStop in Arb.Shrink(input.timeToStop) where timeToStop >= 0
                    from deltaTime in Arb.Shrink(input.deltaTime) where deltaTime > 0
                    select new ValidCalculateVelocityInput
                    {
                        currentVelocity = currentVelocity,
                        targetDirection = targetDirection,
                        maxSpeed = maxSpeed,
                        timeToMaxSpeed = timeToMaxSpeed,
                        timeToStop = timeToStop,
                        deltaTime = deltaTime
                    };
            }

            public static Arbitrary<ValidCalculateVelocityInput> Arbitrary()
            {
                return Arb.From(generator, Shrinker);
            }
        }


        private PlatformerRigidMover2D mover;

        [OneTimeSetUp]
        public void RegisterValidCalculateVelocityInput()
        {
            Arb.Register<ValidCalculateVelocityInput>();
        }

        [SetUp]
        public void CreateMover()
        {
            mover = new GameObject().AddComponent<PlatformerRigidMover2D>();
        }

        [TearDown]
        public void DestroyMover()
        {
            Object.Destroy(mover.gameObject);
        }

        [Test]
        public void Should_throw_when_assigning_negative_value_to_speed_and_time_properties()
        {
            Arbitrary<float> negativeFloats = Arb.From<float>()
                .MapFilter(
                map: x => -Mathf.Abs(x),
                filter: x => x < 0 || float.IsNaN(x)
                );

            Prop.ForAll(negativeFloats,
                x =>
                {
                    Assert.Throws<ArgumentOutOfRangeException>(
                        () => mover.maxSpeed = x,
                        $"Didn't throw an exception when setting {nameof(mover.maxSpeed)} to a negative value.");

                    Assert.Throws<ArgumentOutOfRangeException>(
                        () => mover.timeToMaxSpeed = x,
                        $"Didn't throw an exception when setting {nameof(mover.timeToMaxSpeed)} to a negative value.");

                    Assert.Throws<ArgumentOutOfRangeException>(
                        () => mover.timeToStop = x,
                        $"Didn't throw an exception when setting {nameof(mover.timeToStop)} to a negative value.");
                }
                ).QuickCheckThrowOnFailure();
        }

        [Test]
        public void Move_should_use_only_sign_of_wanted_direction()
        {
            Arbitrary<float> definedFloats = Arb.From<float>()
                .Filter(x => !float.IsNaN(x));

            Prop.ForAll(definedFloats,
                wantedDirection =>
                {
                    float appliedDirection = mover.Move(wantedDirection);
                    return (appliedDirection == Math.Sign(wantedDirection))
                    .Label("Uses only sign.")
                    .When(!float.IsNaN(wantedDirection));

                }
                ).QuickCheckThrowOnFailure();
        }

        [Test]
        public void CalculateVelocity_should_move_velocity_towards_target_direction()
        {
            Prop.ForAll<ValidCalculateVelocityInput>(
                input =>
                {
                    float newVelocity = PlatformerRigidMover2D.CalculateNewVelocity(input.currentVelocity, input.targetDirection, input.maxSpeed, input.timeToMaxSpeed, input.timeToStop, input.deltaTime);

                    return input.targetDirection != 0
                        ? (Math.Sign(newVelocity - input.currentVelocity) == Math.Sign(input.targetDirection))
                        .When(Mathf.Abs(input.currentVelocity) < input.maxSpeed)
                        .Label("Moves velocity towards non-zero target direction")

                        : (Mathf.Abs(newVelocity) < Mathf.Abs(input.currentVelocity))
                        .When(input.currentVelocity != 0)
                        .Label("Moves velocity towards 0 when target directtion is 0");
                }
                ).QuickCheckThrowOnFailure();
        }

        [Test]
        public void CalculateVelocity_should_not_exceed_max_speed()
        {
            Arbitrary<ValidCalculateVelocityInput> maxSpeedInputs = Arb.From<ValidCalculateVelocityInput>()
                .MapFilter(
                map: x => new ValidCalculateVelocityInput
                {
                    currentVelocity = x.maxSpeed * Mathf.Sign(x.targetDirection),       // Mathf.Sign(0) == 1
                    targetDirection = x.targetDirection != 0 ? x.targetDirection : 1,
                    maxSpeed = x.maxSpeed,
                    timeToMaxSpeed = x.timeToMaxSpeed,
                    timeToStop = x.timeToStop,
                    deltaTime = x.deltaTime
                },
                filter: x => true
                );

            Prop.ForAll(maxSpeedInputs,
                input =>
                {
                    Assert.That(Mathf.Abs(input.currentVelocity), Is.EqualTo(input.maxSpeed));

                    float newVelocity = PlatformerRigidMover2D.CalculateNewVelocity(input.currentVelocity, input.targetDirection, input.maxSpeed, input.timeToMaxSpeed, input.timeToStop, input.deltaTime);

                    return (Mathf.Abs(newVelocity) == input.maxSpeed)
                    .Label("Does not exceed max speed");
                }
                ).QuickCheckThrowOnFailure();
        }

        [Test]
        public void CalculateVelocity_should_not_overshoot_zero_when_stopping()
        {
            Arbitrary<ValidCalculateVelocityInput> almostStoppedInputs = Arb.From<ValidCalculateVelocityInput>()
                .MapFilter(
                map: x => new ValidCalculateVelocityInput
                {
                    currentVelocity = float.Epsilon * -Mathf.Sign(x.targetDirection),       // Mathf.Sign(0) == 1
                    targetDirection = 0,
                    maxSpeed = x.maxSpeed,
                    timeToMaxSpeed = x.timeToMaxSpeed,
                    timeToStop = x.timeToStop,
                    deltaTime = x.deltaTime
                },
                filter: x => true
                );

            Prop.ForAll(almostStoppedInputs,
                input =>
                {
                    Assert.That(input.currentVelocity, Is.Not.EqualTo(0));

                    float newVelocity = PlatformerRigidMover2D.CalculateNewVelocity(input.currentVelocity, input.targetDirection, input.maxSpeed, input.timeToMaxSpeed, input.timeToStop, input.deltaTime);

                    return (newVelocity == 0)
                    .Label("Does not overshoot 0 when stopping");
                }
                ).QuickCheckThrowOnFailure();
        }

        [Test]
        public void CalculateVelocity_should_reach_max_speed_in_time()
        {
            Arbitrary<ValidCalculateVelocityInput> stoppedInputs = Arb.From<ValidCalculateVelocityInput>()
                .MapFilter(
                map: x => new ValidCalculateVelocityInput
                {
                    currentVelocity = 0,
                    targetDirection = x.targetDirection != 0 ? x.targetDirection : 1,
                    maxSpeed = x.maxSpeed,
                    timeToMaxSpeed = x.timeToMaxSpeed,
                    timeToStop = x.timeToStop,
                    deltaTime = x.deltaTime
                },
                filter: x => true
                );

            Prop.ForAll(stoppedInputs,
                input =>
                {
                    float currentVelocity = input.currentVelocity;
                    int numOfTimeFrames = Mathf.FloorToInt(input.timeToMaxSpeed / input.deltaTime);

                    for (int i = 0; i < numOfTimeFrames; i++)
                    {
                        currentVelocity = PlatformerRigidMover2D.CalculateNewVelocity(currentVelocity, input.targetDirection, input.maxSpeed, input.timeToMaxSpeed, input.timeToStop, input.deltaTime);
                    }

                    float finalVelocity = PlatformerRigidMover2D.CalculateNewVelocity(currentVelocity, input.targetDirection, input.maxSpeed, input.timeToMaxSpeed, input.timeToStop, input.deltaTime);

                    return (Mathf.Abs(finalVelocity) == input.maxSpeed)
                    .Label($"Reaches max speed in time.\n{nameof(finalVelocity)} = {finalVelocity}")

                    .And((Mathf.Abs(currentVelocity) < input.maxSpeed)
                    .Label($"Doesn't reach max speed before time\n{nameof(currentVelocity)} = {currentVelocity}"));
                }
                ).QuickCheckThrowOnFailure();
        }

        [Test]
        public void CalculateVelocity_should_reach_zero_speed_in_time()
        {
            Arbitrary<ValidCalculateVelocityInput> maxSpeedInputs = Arb.From<ValidCalculateVelocityInput>()
                .MapFilter(
                map: x => new ValidCalculateVelocityInput
                {
                    currentVelocity = x.maxSpeed * Mathf.Sign(x.targetDirection),       // Mathf.Sign(0) == 1
                    targetDirection = 0,
                    maxSpeed = x.maxSpeed,
                    timeToMaxSpeed = x.timeToMaxSpeed,
                    timeToStop = x.timeToStop,
                    deltaTime = x.deltaTime
                },
                filter: x => true
                );

            Prop.ForAll(maxSpeedInputs,
                input =>
                {
                    float currentVelocity = input.currentVelocity;
                    int numOfTimeFrames = Mathf.FloorToInt(input.timeToStop / input.deltaTime);

                    for (int i = 0; i < numOfTimeFrames; i++)
                    {
                        currentVelocity = PlatformerRigidMover2D.CalculateNewVelocity(currentVelocity, input.targetDirection, input.maxSpeed, input.timeToMaxSpeed, input.timeToStop, input.deltaTime);
                    }

                    float finalVelocity = PlatformerRigidMover2D.CalculateNewVelocity(currentVelocity, input.targetDirection, input.maxSpeed, input.timeToMaxSpeed, input.timeToStop, input.deltaTime);

                    return (finalVelocity == 0)
                    .Label($"Reaches zero speed in time.\n{nameof(finalVelocity)} = {finalVelocity}")

                    .And((Mathf.Abs(currentVelocity) > 0)
                    .Label($"Doesn't reach zero speed before time\n{nameof(currentVelocity)} = {currentVelocity}"));
                }
                ).QuickCheckThrowOnFailure();
        }
    }
}
