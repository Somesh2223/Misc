// Odd rules some customers ask couriers to follow (plan 4.8).
// Following a rule earns a bonus; breaking one can trigger an event.
public enum CustomerRule
{
    None,
    ComeAlone,        // nobody else within 10 m when you deliver
    NoLights,         // no flashlight on near the door
    BeforeSecondRise, // deliver before the water's second rise
    KnockOnce,        // knock on the door (E) exactly once before leaving the package
    NobodyAnswers     // just leave it. (No rule, only an unsettling note.)
}
