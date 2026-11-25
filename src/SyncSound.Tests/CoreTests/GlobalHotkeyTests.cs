using SyncSound.Windows.Hotkeys;

namespace SyncSound.Tests.CoreTests;

public sealed class GlobalHotkeyTests
{
    [Theory]
    [InlineData("Ctrl+Shift+S", true, "Ctrl+Shift+S")]
    [InlineData("ctrl + alt + a", true, "Ctrl+Alt+A")]
    [InlineData("Shift+F9", true, "Shift+F9")]
    [InlineData("S", false, "")]
    [InlineData("", false, "")]
    [InlineData("Ctrl+NotARealKey", false, "")]
    public void TryParseShortcut_ValidatesModifiersAndKeys(string input, bool expectedValid, string expectedNormalized)
    {
        var ok = GlobalHotkeyService.TryParseShortcut(input, out _, out _, out var normalized);

        Assert.Equal(expectedValid, ok);
        if (expectedValid)
        {
            Assert.Equal(expectedNormalized, normalized);
        }
    }
}
