using Content.Server.Doors.Components;
using Content.Server.Wires;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Wires;

namespace Content.Server.Doors;

[DataDefinition]
public sealed class DoorSafetyWireAction : BaseWireAction
{
    [DataField("color")]
    private Color _statusColor = Color.Red;

    [DataField("name")]
    private string _text = "SAFE";
    protected override string Text
    {
        get => _text;
        set => _text = value;
    }

    [DataField("timeout")]
    private int _timeout = 30;

    public override StatusLightData? GetStatusLightData(Wire wire)
    {
        var lightState = StatusLightState.Off;
        if (IsPowered(wire.Owner)
            && EntityManager.TryGetComponent<AirlockComponent>(wire.Owner, out var door))
        {
            lightState = door.Safety
                ? StatusLightState.On
                : StatusLightState.Off;
        }

        return new StatusLightData(
            _statusColor,
            lightState,
            _text);
    }

    public override object StatusKey { get; } = AirlockWireStatus.SafetyIndicator;

    public override bool Cut(EntityUid user, Wire wire)
    {
        base.Cut(user, wire);
        if (EntityManager.TryGetComponent<AirlockComponent>(wire.Owner, out var door))
        {
            WiresSystem.TryCancelWireAction(wire.Owner, PulseTimeoutKey.Key);
            EntityManager.System<SharedAirlockSystem>().SetSafety(door, false);
        }

        return true;
    }

    public override bool Mend(EntityUid user, Wire wire)
    {
        base.Mend(user, wire);
        if (EntityManager.TryGetComponent<AirlockComponent>(wire.Owner, out var door))
        {
            EntityManager.System<SharedAirlockSystem>().SetSafety(door, true);
        }

        return true;
    }

    public override bool Pulse(EntityUid user, Wire wire)
    {
        base.Pulse(user, wire);
        if (EntityManager.TryGetComponent<AirlockComponent>(wire.Owner, out var door))
        {
            EntityManager.System<SharedAirlockSystem>().SetSafety(door, false);
            WiresSystem.StartWireAction(wire.Owner, _timeout, PulseTimeoutKey.Key, new TimedWireEvent(AwaitSafetyTimerFinish, wire));
        }

        return true;
    }

    public override void Update(Wire wire)
    {
        if (!IsPowered(wire.Owner))
        {
            WiresSystem.TryCancelWireAction(wire.Owner, PulseTimeoutKey.Key);
        }
    }

    private void AwaitSafetyTimerFinish(Wire wire)
    {
        if (!wire.IsCut)
        {
            if (EntityManager.TryGetComponent<AirlockComponent>(wire.Owner, out var door))
            {
                EntityManager.System<SharedAirlockSystem>().SetSafety(door, true);
            }
        }
    }

    private enum PulseTimeoutKey : byte
    {
        Key
    }
}
