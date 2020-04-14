using UniRx;


namespace SpicyShower.Physics.CharacterController
{
    public interface IKnowsWhenGrounded
    {
        /// <summary>
        /// Whether the object is on the ground right now.
        /// </summary>
        ReadOnlyReactiveProperty<bool> isGrounded { get; }
    }
}