namespace MilsimManager.Services;

public interface IDevService {
    Task ResetAsync();
    Task SeedAsync();
}
