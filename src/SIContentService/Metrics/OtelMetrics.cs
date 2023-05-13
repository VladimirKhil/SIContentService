using System.Diagnostics.Metrics;

namespace SIContentService.Metrics;

/// <summary>
/// Holds service metrics.
/// </summary>
public sealed class OtelMetrics
{
    private Counter<int> UploadedPackagesCounter { get; }

    private Counter<int> UploadedAvatarsCounter { get; }

    public string MeterName { get; }

    public OtelMetrics(string meterName = "SIContent")
    {
        var meter = new Meter(meterName);
        MeterName = meterName;

        UploadedPackagesCounter = meter.CreateCounter<int>("packages-uploaded");
        UploadedAvatarsCounter = meter.CreateCounter<int>("avatars-uploaded");
    }

    public void AddPackage() => UploadedPackagesCounter.Add(1);

    public void AddAvatar() => UploadedAvatarsCounter.Add(1);
}
