using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using NSubstitute;
using FsCheck;
using UnityEngine;
using UnityEngine.TestTools;
using UniRx;

using Object = UnityEngine.Object;


namespace SpicyShower.Physics.CharacterController.Tests
{
    [TestOf(typeof(PlatformerRigidJumper2D))]
    public class TestPlatformerRigidJumper2D
    {
        private PlatformerRigidJumper2D jumper;
        private IKnowsWhenGrounded groundChecker;
        private ReactiveProperty<bool> isGrounded;
        private float originalTimeScale;

        [OneTimeSetUp]
        public void ScaleTime()
        {
            originalTimeScale = Time.timeScale;
            Time.timeScale = 2.5f;
        }

        [OneTimeTearDown]
        public void ResetTimeScale()
        {
            Time.timeScale = originalTimeScale;
        }

        [SetUp]
        public void CreateJumper()
        {
            isGrounded = new ReactiveProperty<bool>(false);
            groundChecker = Substitute.For<IKnowsWhenGrounded>();
            groundChecker.isGrounded.Returns(isGrounded.ToReadOnlyReactiveProperty());

            jumper = new GameObject().AddComponent<PlatformerRigidJumper2D>();
            jumper.Construct(groundChecker);

            isGrounded.AddTo(jumper);
        }

        [TearDown]
        public void DestroyJumper()
        {
            Object.Destroy(jumper);
        }

        [UnityTest]
        public IEnumerator CanJumpValue_should_initially_be_true_when_grounded_and_false_when_not_grounded(
            [Values(false, true)] bool grounded
            )
        {
            isGrounded.Value = grounded;
            Assert.That(jumper.canJump.Value, Is.EqualTo(grounded), $"{nameof(jumper.canJump)} should initially be equal to {nameof(groundChecker.isGrounded)}");

            yield break;
        }

        [UnityTest]
        public IEnumerator CanJumpValue_should_still_be_true_jumpAfterLeavingGroundDelay_seconds_after_leaving_ground(
            [Random(0.01f, 2f, 3)] [Values(0f)] float jumpAfterLeavingGroundDelay
            )
        {
            isGrounded.Value = true;
            Assert.That(jumper.canJump.Value, Is.True);

            jumper.jumpAfterLeavingGroundDelay = jumpAfterLeavingGroundDelay;

            isGrounded.Value = false;
            yield return null;

            if (jumpAfterLeavingGroundDelay == 0)
            {
                Assert.That(jumper.canJump.Value, Is.False, $"{nameof(jumper.canJump)} should be false immediately if {nameof(jumper.jumpAfterLeavingGroundDelay)} is 0.");
            }
            else
            {
                Assert.That(jumper.canJump.Value, Is.True, $"{nameof(jumper.canJump)} should stay true for {nameof(jumper.jumpAfterLeavingGroundDelay)} seconds.");

                yield return new WaitForSeconds(jumpAfterLeavingGroundDelay * 0.9f);
                Assert.That(jumper.canJump.Value, Is.True, $"{nameof(jumper.canJump)} should stay true for {nameof(jumper.jumpAfterLeavingGroundDelay)} seconds.");

                yield return new WaitForSeconds(jumpAfterLeavingGroundDelay * 0.1f);
                Assert.That(jumper.canJump.Value, Is.False, $"{nameof(jumper.canJump)} should be false after {nameof(jumper.jumpAfterLeavingGroundDelay)} seconds.");
            }
        }

        [UnityTest]
        public IEnumerator Jump_should_be_executed_when_landing_if_jump_request_occured_within_jumpBeforeLandingDelay_seconds_of_landing(
            [Random(0.01f, 2f, 3)] float jumpBeforeLandingDelay)
        {
            Action<Unit> jumpCallback = Substitute.For<Action<Unit>>();
            jumper.onJump.Subscribe(jumpCallback).AddTo(jumper);

            isGrounded.Value = false;
            Assert.That(jumper.canJump.Value, Is.False);

            jumper.jumpBeforeLandingDelay = jumpBeforeLandingDelay;

            jumper.Jump();
            jumpCallback.DidNotReceiveWithAnyArgs().Invoke(default);

            yield return new WaitForSeconds(jumpBeforeLandingDelay * 0.9f);
            isGrounded.Value = true;
            jumpCallback.Received(1).Invoke(default);
        }

        [UnityTest]
        public IEnumerator Jump_should_be_executed_only_once_when_allowed(
            [Random(0.01f, 2f, 2)] [Values(0f)] float jumpAfterLeavingGroundDelay,
            [Random(0.01f, 2f, 2)] [Values(0f)] float jumpBeforeLandingDelay
            )
        {
            Action<Unit> jumpCallback = Substitute.For<Action<Unit>>();
            jumper.onJump.Subscribe(jumpCallback).AddTo(jumper);

            isGrounded.Value = false;
            Assert.That(jumper.canJump.Value, Is.False);

            jumper.jumpAfterLeavingGroundDelay = jumpAfterLeavingGroundDelay;
            jumper.jumpBeforeLandingDelay = jumpBeforeLandingDelay;

            jumper.Jump();
            jumpCallback.DidNotReceiveWithAnyArgs().Invoke(default);

            yield return null;

            isGrounded.Value = true;
            jumper.Jump();
            jumpCallback.Received(1).Invoke(default);

            yield return null;

            isGrounded.Value = false;
            jumper.Jump();
            jumpCallback.Received(1).Invoke(default);
        }
    }
}
