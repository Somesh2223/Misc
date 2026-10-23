// Where the crew is in the night's loop (plan section 3).
public enum ShiftPhase
{
    Depot,   // in the hub: shop open, players may join, host starts the shift
    Shift,   // out delivering
    Results  // the shift is over: earnings and quota shown, host returns everyone to the depot
}
