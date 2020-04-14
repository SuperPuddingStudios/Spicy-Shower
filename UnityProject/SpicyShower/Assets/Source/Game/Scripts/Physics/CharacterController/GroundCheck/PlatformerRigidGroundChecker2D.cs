using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UniRx;


namespace SpicyShower.Physics.CharacterController
{
    /// <inheritdoc/>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]         // Must have a Rigidbody2D in order for colliders to register collisions
    public partial class PlatformerRigidGroundChecker2D : MonoBehaviour, IKnowsWhenGrounded
    {
        /// <summary>
        /// Layer mask of the layers the object will consider to be ground.
        /// </summary>
        [SerializeField] public LayerMask groundLayerMask;

        /// <summary>
        /// Colliders to be used in the ground check.
        /// If any of them touches the ground, the object will be considerd grounded.
        /// </summary>
        [SerializeField] public Collider2D[] groundCheckColliders;

        public ReadOnlyReactiveProperty<bool> isGrounded { get; private set; }

        private ReactiveProperty<bool> _isGrounded;


        private void Awake()
        {
            _isGrounded = new ReactiveProperty<bool>(false).AddTo(this);
            isGrounded = _isGrounded.ToReadOnlyReactiveProperty().AddTo(this);
        }

        private void FixedUpdate()
        {
            _isGrounded.Value = CheckForGround(groundCheckColliders, groundLayerMask);
        }

        private static bool CheckForGround(IEnumerable<Collider2D> groundCheckColliders, LayerMask groundLayerMask)
        {
            return groundCheckColliders.Any(collider => collider.IsTouchingLayers(groundLayerMask));
        }
    }
}