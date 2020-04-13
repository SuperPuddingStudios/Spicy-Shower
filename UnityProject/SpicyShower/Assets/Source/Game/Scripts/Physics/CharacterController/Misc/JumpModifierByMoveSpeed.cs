using System;
using UnityEngine;
using UniRx;


namespace SpicyShower.Physics.CharacterController
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(IHas1DReactiveVelocity))]
    [RequireComponent(typeof(IHasJumpData))]
    [RequireComponent(typeof(IKnowsWhenGrounded))]
    public class JumpModifierByMoveSpeed : MonoBehaviour
    {
        private const float MinAllowedTime = 0.001f;

        [SerializeField] private float _maxApexHeight;
        [SerializeField] private float _minApexHeight;
        [SerializeField] private float _timeToMaxApex;
        [SerializeField] private float _timeFromMaxApex;

        /// <summary>
        /// The height at the top of the maximum jump.
        /// </summary>
        public float maxApexHeight
        {
            get => _maxApexHeight;
            set => _maxApexHeight = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(maxApexHeight), $"{nameof(maxApexHeight)} must be non-negative.");
        }

        /// <summary>
        /// The height at the top of the minimum jump.
        /// </summary>
        public float minApexHeight
        {
            get => _minApexHeight;
            set => _minApexHeight = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(minApexHeight), $"{nameof(minApexHeight)} must be non-negative.");
        }

        /// <summary>
        /// The time in seconds it takes to reach the top of the maximum jump.
        /// </summary>
        public float timeToMaxApex
        {
            get => _timeToMaxApex;
            set => _timeToMaxApex = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(timeToMaxApex), $"{nameof(timeToMaxApex)} must be positive.");
        }

        /// <summary>
        /// The time in seconds it takes to fall from the top of the maximum jump back to ground.
        /// </summary>
        public float timeFromMaxApex
        {
            get => _timeFromMaxApex;
            set => _timeFromMaxApex = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(timeFromMaxApex), $"{nameof(timeFromMaxApex)} must be positive.");
        }

        private IHas1DReactiveVelocity mover;
        private IHasJumpData jumper;
        private IKnowsWhenGrounded groundChecker;


        private void OnValidate()
        {
            _maxApexHeight = Mathf.Clamp(_maxApexHeight, 0, float.MaxValue);
            _minApexHeight = Mathf.Clamp(_minApexHeight, 0, float.MaxValue);
            _timeToMaxApex = Mathf.Clamp(_timeToMaxApex, MinAllowedTime, float.MaxValue);
            _timeFromMaxApex = Mathf.Clamp(_timeFromMaxApex, MinAllowedTime, float.MaxValue);
        }

        private void Awake()
        {
            mover = GetComponent<IHas1DReactiveVelocity>();
            jumper = GetComponent<IHasJumpData>();
            groundChecker = GetComponent<IKnowsWhenGrounded>();
        }

        private void Start()
        {
            mover.velocity
                .Where(_ => groundChecker.isGrounded.Value == true)
                .Subscribe(_ => SetJumpData())
                .AddTo(this);

            groundChecker.isGrounded
                .Where(x => x == true)
                .Subscribe(_ => SetJumpData())
                .AddTo(this);
        }

        private void SetJumpData()
        {
            if (isActiveAndEnabled)
            {
                float speed = Mathf.Abs(mover.velocity.Value);
                jumper.apexHeight = Mathf.Lerp(minApexHeight, maxApexHeight, speed / mover.maxSpeed);

                float heightRatio = Mathf.Sqrt(jumper.apexHeight / maxApexHeight);
                jumper.timeToApex = timeToMaxApex * heightRatio;
                jumper.timeFromApex = timeFromMaxApex * heightRatio;
            }
        }
    }
}
