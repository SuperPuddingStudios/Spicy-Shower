using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UniRx;


namespace SpicyShower.Physics.CharacterController
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlatformerRigidMover2D : MonoBehaviour
    {
        [SerializeField] private float _maxSpeed;
        [SerializeField] private float _timeToMaxSpeed;
        [SerializeField] private float _timeToStop;

        /// <summary>
        /// The maximum horizontal speed this object can reach in ideal conditions (no friction etc.)
        /// </summary>
        public float maxSpeed
        {
            get => _maxSpeed;
            set => _maxSpeed = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(maxSpeed), $"{nameof(maxSpeed)} must be non-negative.");
        }

        /// <summary>
        /// The time it takes to accelerate to max speed, in seconds.
        /// 0 means instant acceleration.
        /// </summary>
        public float timeToMaxSpeed
        {
            get => _timeToMaxSpeed;
            set => _timeToMaxSpeed = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(timeToMaxSpeed), $"{nameof(timeToMaxSpeed)} must be non-negative.");
        }

        /// <summary>
        /// The time it takes to deccelerate from max speed to full stop, in seconds.
        /// 0 means instant decceleration.
        /// </summary>
        public float timeToStop
        {
            get => _timeToStop;
            set => _timeToStop = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(timeToStop), $"{nameof(timeToStop)} must be non-negative.");
        }

        /// <summary>
        /// The current velocity of the object.
        /// </summary>
        public ReadOnlyReactiveProperty<float> velocity { get; private set; }

        private ReactiveProperty<float> _velocity;
        private Rigidbody2D _rigidbody;
        private int _targetDirection;


        private void OnValidate()
        {
            _maxSpeed = Mathf.Clamp(_maxSpeed, 0, float.MaxValue);
            _timeToMaxSpeed = Mathf.Clamp(_timeToMaxSpeed, 0, float.MaxValue);
            _timeToStop = Mathf.Clamp(_timeToStop, 0, float.MaxValue);
        }

        private void Awake()
        {
            _velocity = new ReactiveProperty<float>(0);
            velocity = _velocity.ToReadOnlyReactiveProperty();

            _rigidbody = GetComponent<Rigidbody2D>();

            _targetDirection = 0;
        }

        private void FixedUpdate()
        {
            float newVelocity = CalculateNewVelocity(currentVelocity: _rigidbody.velocity.x, _targetDirection, maxSpeed, timeToMaxSpeed, timeToStop, deltaTime: Time.deltaTime);

            _rigidbody.velocity = new Vector2(newVelocity, _rigidbody.velocity.y);
            _velocity.Value = newVelocity;
        }

        /// <summary>
        /// Requests the object to move in a given direction.
        /// </summary>
        /// <remarks>May not be able to move. May modify wanted direction.</remarks>
        /// <param name="wantedDirection">The direction in which to move.</param>
        /// <returns>The actual direction applied.</returns>
        public float Move(float wantedDirection)
        {
            _targetDirection = Math.Sign(wantedDirection);
            return _targetDirection;
        }

        /// <summary>
        /// Requests the object to move in a given direction.
        /// </summary>
        /// <remarks>May not be able to move. May modify wanted direction.</remarks>
        /// <param name="context">An input collback context with float value as the wanted movement direction.</param>
        public void Move(InputAction.CallbackContext context)
        {
            Move(context.ReadValue<float>());
        }

        // public and static mainly for testing
        // This could be done better if we had a delegate for CalculateNewVelocity, and the implementation was in some other class, we would then test that class. Not crucial enough to do this now.
        public static float CalculateNewVelocity(float currentVelocity, int targetDirection, float maxSpeed, float timeToMaxSpeed, float timeToStop, float deltaTime)
        {
            int velocityDirection = Math.Sign(currentVelocity);
            float accelerationDirection = targetDirection != 0 ? targetDirection : -velocityDirection;

            float speedUpAccelerationSize = maxSpeed != 0 ? maxSpeed / timeToMaxSpeed : 0;
            float slowDownAccelerationSize = maxSpeed != 0 ? maxSpeed / timeToStop : 0;
            float accelerationSize =
                accelerationDirection == 0 ? 0
                : accelerationDirection * velocityDirection >= 0 ? speedUpAccelerationSize
                : slowDownAccelerationSize;

            float newVelocity = currentVelocity + accelerationDirection * accelerationSize * deltaTime;
            newVelocity = Mathf.Clamp(newVelocity, -maxSpeed, maxSpeed);

            // If targetDirection is 0 we want to stop moving, if velocity swiched direction since last frame means we overshoot and should stop moving
            newVelocity = targetDirection == 0 && newVelocity * currentVelocity <= 0 ? 0 : newVelocity;

            return newVelocity;
        }
    }
}
