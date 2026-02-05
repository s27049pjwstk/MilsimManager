using MudBlazor;

namespace MilsimManager.Services;

public sealed class ErrorHandler(ISnackbar snackbar, ILogger<ErrorHandler> logger) : IErrorHandler {

    public void HandleException(Exception ex, string? userMessage = null, bool swallowUnexpected = false) {
        if (ex is AppException aex) {
            logger.LogWarning(aex, "AppException handled: {Message}", aex.Message);
            snackbar.Add(aex.Message, Severity.Error);
            return;
        }

        logger.LogError(ex, "Unhandled exception");
        snackbar.Add(userMessage ?? "Unexpected error occurred.", Severity.Error);
        if (!swallowUnexpected) throw ex;
    }
    
    public Task ExecuteAsync(Func<Task> action, string? userMessage = null, bool swallowUnexpected = false) => ExecuteAsync<object>(
    async () => {
        await action();
        return null!;
    },
    userMessage,
    null!,
    swallowUnexpected);

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, string? userMessage = null, T fallback = default!, bool swallowUnexpected = false) {
        try {
            return await action();
        } catch (AppException ex) {
            logger.LogWarning(ex, "AppException handled: {Message}", ex.Message);
            snackbar.Add(ex.Message, Severity.Error);
            return fallback;
        } catch (Exception ex) {
            logger.LogError(ex, "Unhandled exception");
            if (!swallowUnexpected) throw;
            snackbar.Add(userMessage ?? "Unexpected error occurred.", Severity.Error);
            return fallback;
        }
    }
}