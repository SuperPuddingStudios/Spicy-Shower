using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UniRx;


namespace SpicyShower.Physics.CharacterController
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]         // Must have a Rigidbody2D in order for colliders to register collisions
    public partial class PlatformerRigidGroundChecker2D : MonoBehaviour
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

        /// <summary>
        /// Whether the object is on the ground right now.
        /// </summary>
        public ReadOnlyReactiveProperty<bool> isGrounded { get; private set; }

        private ReactiveProperty<bool> _isGrounded;


        private void Awake()
        {
            _isGrounded = new ReactiveProperty<bool>(false);
            isGrounded = _isGrounded.ToReadOnlyReactiveProperty();
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