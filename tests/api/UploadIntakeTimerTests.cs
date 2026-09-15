namespace Documate.Api.Tests;

using Documate.Api.Infrastructure.Work;

public class UploadIntakeTimerTests
{
    [Fact]
    public async Task Accumulates_db_and_blob_separately()
    {
        var timer = new UploadIntakeTimer();
        await timer.MeasureDbAsync(async ct => await Task.Delay(20, ct), CancellationToken.None);
        await timer.MeasureBlobAsync(async ct => await Task.Delay(30, ct), CancellationToken.None);
        var (acceptMs, blobMs, dbMs) = timer.Elapsed();

        Assert.True(acceptMs >= 40, $"accept_ms={acceptMs}");
        Assert.True(dbMs >= 15, $"db_ms={dbMs}");
        Assert.True(blobMs >= 25, $"blob_ms={blobMs}");
    }
}
