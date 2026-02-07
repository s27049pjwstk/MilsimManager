using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using MilsimManager.Models;
using MilsimManager.Services;

namespace MilsimManager.Extensions;

public static class AuthExtensions {
    public static async Task<User> GetAuthenticatedUserAsync(this IUserService userService, Task<AuthenticationState> authenticationStateTask) =>
        await userService.GetAuthenticatedUserAsync(await authenticationStateTask);

    public static async Task<User> GetAuthenticatedUserAsync(this IUserService userService, AuthenticationState authState) {
        if (!int.TryParse(authState.User.FindFirstValue("auth_UserId"), out var approverId))
            throw new AppException("Could not identify authenticated user");
        var approver = await userService.GetByIdAsync(approverId);
        return approver ?? throw new AppException("Could not identify authenticated user");
    }
}
