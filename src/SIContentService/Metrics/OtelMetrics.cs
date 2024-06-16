using System.Diagnostics.Metrics;

namespace SIContentService.Metrics;

/// <summary>
/// Holds service metrics.
/// </summary>
public sealed class OtelMetrics
{
    public const string MeterName = "SIContent";
    
    private Counter<int> UploadedPackagesCounter { get; }

    private Counter<int> UploadedAvatarsCounter { get; }

    private Counter<int> DeletedPackagesCounter { get; }

    private Counter<int> DeletedAvatarsCounter { get; }

    public OtelMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        UploadedPackagesCounter = meter.CreateCounter<int>("packages-uploaded");
        UploadedAvatarsCounter = meter.CreateCounter<int>("avatars-uploaded");
        DeletedPackagesCounter = meter.CreateCounter<int>("packages-deleted");
        DeletedAvatarsCounter = meter.CreateCounter<int>("avatars-deleted");
    }

    public void AddPackage() => UploadedPackagesCounter.Add(1);

    public void AddAvatar() => UploadedAvatarsCounter.Add(1);

    public void DeletePackage() => DeletedPackagesCounter.Add(1);

    public void DeleteAvatar() => DeletedAvatarsCounter.Add(1);
}
