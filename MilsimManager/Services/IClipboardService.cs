namespace MilsimManager.Services;

public interface IClipboardService {
    ValueTask<string> ReadTextAsync();
    ValueTask WriteTextAsync(string text);
    Task CopyToClipboard(string? text);
}
