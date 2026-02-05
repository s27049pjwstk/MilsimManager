namespace MilsimManager.Services;

public interface IErrorHandler {
    void HandleException(Exception ex, string? userMessage = null, bool swallowUnexpected = false);
    
    Task ExecuteAsync(
        Func<Task> action,
        string? userMessage = null,
        bool swallowUnexpected = false
    );

    Task<T> ExecuteAsync<T>(
        Func<Task<T>> action,
        string? userMessage = null,
        T fallback = default!,
        bool swallowUnexpected = false
    );
}