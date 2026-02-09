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
}
