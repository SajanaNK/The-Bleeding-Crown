namespace HeroKnight.Input
{
    /// <summary>
    /// Abstracts the Hero Knight's control source. Movement/combat states read only
    /// this interface, never a concrete input implementation, so the input source
    /// (touch today, potentially keyboard/gamepad later) can change independently.
    /// </summary>
    public interface IHeroKnightInput
    {
        float MoveX { get; }
        bool JumpPressed { get; }
        bool AttackPressed { get; }
        bool BlockHeld { get; }
        bool RollPressed { get; }
    }
}
