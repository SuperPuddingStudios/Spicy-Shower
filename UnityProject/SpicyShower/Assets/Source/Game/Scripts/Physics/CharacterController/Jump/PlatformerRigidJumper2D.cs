using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UniRx;


namespace SpicyShower.Physics.CharacterController
{
    /// <inheritdoc/>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(IKnowsWhenGrounded))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlatformerRigidJumper2D : MonoBehaviour, IHasJumpData
    {
        private const float MinAllowedTime = 0.001f;

        [SerializeField] private float _apexHeight;
        [SerializeField] private float _timeToApex;
        [SerializeField] private float _timeFromApex;
        [SerializeField] private float _jumpAfterLeavingGroundDelay;
        [SerializeField] private float _jumpBeforeLandingDelay;

        public float apexHeight
        {
            get => _apexHeight;
            set => _apexHeight = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(apexHeight), $"{nameof(apexHeight)} must be non-negative.");
        }

        public float timeToApex
        {
            get => _timeToApex;
            set => _timeToApex = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(timeToApex), $"{nameof(timeToApex)} must be positive.");
        }

        public float timeFromApex
        {
            get => _timeFromApex;
            set => _timeFromApex = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(timeFromApex), $"{nameof(timeFromApex)} must be positive.");
        }

        /// <summary>
        /// A time period (in seconds) in which the object can jump even after leaving the ground.
        /// Typically this is to make the game feel more responsive.
        /// </summary>
        public float jumpAfterLeavingGroundDelay
        {
            get => _jumpAfterLeavingGroundDelay;
            set => _jumpAfterLeavingGroundDelay = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(jumpAfterLeavingGroundDelay), $"{nameof(jumpAfterLeavingGroundDelay)} must be non-negative.");
        }

        /// <summary>
        /// A time period (in seconds) in which the object can jump even before landing on the ground. The jump will occure once it lands.
        /// Typically this is to make the game feel more responsive.
        /// </summary>
        public float jumpBeforeLandingDelay
        {
            get => _jumpBeforeLandingDelay;
            set => _jumpBeforeLandingDelay = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(jumpBeforeLandingDelay), $"{nameof(jumpBeforeLandingDelay)} must be non-negative.");
        }

        /// <summary>
        /// Whether the object can jump right now.
        /// </summary>
        public ReadOnlyReactiveProperty<bool> canJump { get; private set; }

        /// <summary>
        /// An observable that emits when the object performs a jump.
        /// </summary>
        public Subject<Unit> onJump { get; private set; }

        private float _jumpSpeed => 2 * _apexHeight / _timeToApex;
        private float _riseGravityScale => -2 * _apexHeight / (_timeToApex * _timeToApex) / Physics2D.gravity.y;
        private float _fallGravityScale => -2 * _apexHeight / (_timeFromApex * _timeFromApex) / Physics2D.gravity.y;

        private IKnowsWhenGrounded _groundChecker;
        private Rigidbody2D _rigidbody;
        private ReactiveProperty<bool> _canJump;
        private float? _jumpRequestTime;


        private void OnValidate()
        {
            _apexHeight = Mathf.Clamp(_apexHeight, 0, float.MaxValue);
            _timeToApex = Mathf.Clamp(_timeToApex, MinAllowedTime, float.MaxValue);
            _timeFromApex = Mathf.Clamp(_timeFromApex, MinAllowedTime, float.MaxValue);
            _jumpAfterLeavingGroundDelay = Mathf.Clamp(_jumpAfterLeavingGroundDelay, 0, float.MaxValue);
            _jumpBeforeLandingDelay = Mathf.Clamp(_jumpBeforeLandingDelay, 0, float.MaxValue);
        }

        private void Awake()
        {
            _groundChecker = _groundChecker ?? GetComponent<IKnowsWhenGrounded>();
            _rigidbody = GetComponent<Rigidbody2D>();

            _canJump = new ReactiveProperty<bool>(false).AddTo(this);
            canJump = _canJump.ToReadOnlyReactiveProperty().AddTo(this);
            onJump = new Subject<Unit>().AddTo(this);

            _jumpRequestTime = null;
        }

        private void Start()
        {
            if (_groundChecker != null)
            {
                SubscribeToIsGrounded();
            }
        }

        private void FixedUpdate()
        {
            _rigidbody.gravityScale = _groundChecker.isGrounded.Value ? 0
                : _rigidbody.velocity.y <= 0 ? _fallGravityScale
                : _riseGravityScale;
        }

        public void Construct(IKnowsWhenGrounded groundChecker)
        {
            _groundChecker = groundChecker;
        }

        /// <summary>
        /// Requests the object to jump.
        /// </summary>
        /// <remarks>Might perform a delayed jump according to <see cref="jumpBeforeLandingDelay"/>, in this case this method returns false.</remarks>
        /// <returns>Whether a jump occured.</returns>
        public bool Jump()
        {
            if (!isActiveAndEnabled)
            {
                return false;
            }

            if (canJump.Value)
            {
                _Jump();
                return true;
            }
            else
            {
                _jumpRequestTime = Time.time;
                return false;
            }
        }

        public void Jump(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                Jump();
            }
        }

        private void _Jump()
        {
            _rigidbody.velocity = new Vector2(_rigidbody.velocity.x, _jumpSpeed);
            _canJump.Value = false;
            onJump.OnNext(Unit.Default);
        }

        private IEnumerator DelayAfterLeavingGround()
        {
            yield return new WaitForSeconds(jumpAfterLeavingGroundDelay);
            _canJump.Value = _groundChecker.isGrounded.Value;
        }

        private void SubscribeToIsGrounded()
        {
            _groundChecker.isGrounded
                .Where(x => x == true)
                .Subscribe(
                x =>
                {
                    _canJump.Value = x;

                    if (Time.time - _jumpRequestTime <= jumpBeforeLandingDelay)
                    {
                        _Jump();
                        _jumpRequestTime = null;
                    }
                })
                .AddTo(this);

            _groundChecker.isGrounded
                .Where(x => x == false)
                .Subscribe(
                _ => StartCoroutine(DelayAfterLeavingGround())
                )
                .AddTo(this);
        }
    }
}
