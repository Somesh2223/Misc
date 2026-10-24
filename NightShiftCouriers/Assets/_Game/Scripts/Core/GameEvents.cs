using System;
using UnityEngine;

// A very small event bus (plan rule 8). Systems raise events here and
// other systems listen, so monsters, UI and audio never need direct references.
public static class GameEvents
{
    // A noise was made. loudness is roughly "how many metres away can it be heard at 1.0 monster hearing".
    // fromWater is true for splashing/swimming, which water monsters care about.
    public static event Action<Vector3, float, bool> NoiseMade;

    // Flood stage changed (new stage index, 0-based).
    public static event Action<int> FloodStageChanged;

    // A rise is about to happen (stage about to start, seconds until it does).
    public static event Action<int, float> FloodWarning;

    // A package was delivered: value paid.
    public static event Action<Package, int> PackageDelivered;

    // The player took damage / died.
    public static event Action<float> PlayerDamaged;
    public static event Action PlayerDied;

    // A monster changed AI state (raised on every machine). The AI Director and the music listen to this.
    public static event Action<MonsterBase, MonsterState> MonsterStateChanged;

    public static void RaiseNoise(Vector3 position, float loudness, bool fromWater)
    {
        NoiseMade?.Invoke(position, loudness, fromWater);
    }

    public static void RaiseFloodStageChanged(int stage)
    {
        FloodStageChanged?.Invoke(stage);
    }

    public static void RaiseFloodWarning(int nextStage, float secondsLeft)
    {
        FloodWarning?.Invoke(nextStage, secondsLeft);
    }

    public static void RaisePackageDelivered(Package package, int payout)
    {
        PackageDelivered?.Invoke(package, payout);
    }

    public static void RaisePlayerDamaged(float amount)
    {
        PlayerDamaged?.Invoke(amount);
    }

    public static void RaisePlayerDied()
    {
        PlayerDied?.Invoke();
    }

    public static void RaiseMonsterStateChanged(MonsterBase monster, MonsterState state)
    {
        MonsterStateChanged?.Invoke(monster, state);
    }
}
