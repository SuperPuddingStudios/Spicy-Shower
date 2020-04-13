namespace SpicyShower.Physics.CharacterController
{
    public interface IHasJumpData
    {
        /// <summary>
        /// The height at the top of the jump.
        /// </summary>
        float apexHeight { get; set; }

        /// <summary>
        /// The time in seconds it takes to reach the top of the jump.
        /// </summary>
        float timeFromApex { get; set; }

        /// <summary>
        /// The time in seconds it takes to fall from the top of the jump back to ground.
        /// </summary>
        float timeToApex { get; set; }
    }
}