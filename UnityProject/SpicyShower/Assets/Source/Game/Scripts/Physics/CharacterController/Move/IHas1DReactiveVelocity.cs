using UniRx;


namespace SpicyShower.Physics.CharacterController
{
    public interface IHas1DReactiveVelocity
    {
        /// <summary>
        /// The maximum horizontal speed this object can reach in ideal conditions (no friction etc.)
        /// </summary>
        float maxSpeed { get; set; }

        /// <summary>
        /// The current velocity of the object.
        /// </summary>
        ReadOnlyReactiveProperty<float> velocity { get; }
    }
}