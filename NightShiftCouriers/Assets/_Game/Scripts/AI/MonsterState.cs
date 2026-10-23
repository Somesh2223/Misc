// The shared monster states (section 4.6):
// Idle -> Wander -> Investigate (sound/light) -> Hunt -> Attack -> Cooldown/Retreat
public enum MonsterState
{
    Idle,
    Wander,
    Investigate,
    Hunt,
    Attack,
    Cooldown
}
