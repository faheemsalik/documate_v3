namespace Documate.Api.Infrastructure.Settings;

using Documate.Api.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

/// <summary>Signals <see cref="IOptionsMonitor{T}"/> to rebuild after system-settings invalidate.</summary>
public sealed class SystemSettingsOptionsChangeSource :
    IOptionsChangeTokenSource<StorageOptions>,
    IOptionsChangeTokenSource<OcrOptions>,
    IOptionsChangeTokenSource<PipelineOptions>,
    IOptionsChangeTokenSource<NotificationOptions>,
    IOptionsChangeTokenSource<AdminOptions>
{
    private CancellationTokenSource _cts = new();

    public string Name => Options.DefaultName;

    public IChangeToken GetChangeToken() => new CancellationChangeToken(_cts.Token);

    public void Signal()
    {
        var previous = Interlocked.Exchange(ref _cts, new CancellationTokenSource());
        previous.Cancel();
        previous.Dispose();
    }
}
