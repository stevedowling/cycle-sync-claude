namespace CycleSync.Acceptance.Support;

/// <summary>
/// A <see cref="TimeProvider"/> whose "now" can be set from tests. Lets scenarios generate cached
/// data "1 day ago" and then assert it is reused rather than regenerated. Defaults to real UTC now.
/// </summary>
public sealed class ControllableTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow = DateTimeOffset.UtcNow;

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public void SetUtcNow(DateTimeOffset value) => _utcNow = value;

    public void Advance(TimeSpan by) => _utcNow = _utcNow.Add(by);
}
