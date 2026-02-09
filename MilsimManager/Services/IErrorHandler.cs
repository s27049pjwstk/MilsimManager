namespace MilsimManager.Services;

public interface IErrorHandler {
    void HandleException(Exception ex, string? userMessage = null, bool swallowUnexpected = false);
}
