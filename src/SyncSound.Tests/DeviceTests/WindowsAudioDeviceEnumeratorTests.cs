using SyncSound.Windows.Audio;

namespace SyncSound.Tests.DeviceTests;

public sealed class WindowsAudioDeviceEnumeratorTests
{
    [Fact]
    public void GetRenderEndpoints_ReturnsUniqueEndpointsWithoutThrowing()
    {
        using var enumerator = new WindowsAudioDeviceEnumerator();
        var endpoints = enumerator.GetRenderEndpoints(includeInactive: true);

        Assert.NotNull(endpoints);
        var uniqueIds = endpoints.Select(e => e.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        Assert.Equal(endpoints.Count, uniqueIds);
    }
}
