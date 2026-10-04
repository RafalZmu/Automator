namespace Automator.Application.Launcher;

/// <summary>Changes focus modes and cancels work whose result no longer belongs to the current view.</summary>
public sealed class LauncherModeCoordinator(LauncherSession session, AliasLaunchScheduler launchScheduler)
{
    public bool SetMode(LauncherMode mode)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(launchScheduler);
        var previous = session.Snapshot.Mode;
        if (previous == mode) return false;
        launchScheduler.CancelPending();
        session.EnterMode(mode);
        return true;
    }
}

public readonly record struct ActivationTicket(long ActivationId, long PanelRevision);

/// <summary>Invalidates late activation completions when the user opens or closes the launcher again.</summary>
public sealed class PanelActivationEpoch
{
    private long _activationId;
    private long _panelRevision;

    public ActivationTicket BeginActivation() => new(++_activationId, ++_panelRevision);

    public void PanelChanged() => _panelRevision++;

    public bool IsCurrent(ActivationTicket ticket) =>
        ticket.ActivationId == _activationId && ticket.PanelRevision == _panelRevision;
}
