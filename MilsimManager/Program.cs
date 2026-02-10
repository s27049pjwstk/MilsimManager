using System.Security.Claims;
using AspNet.Security.OAuth.Discord;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.EntityFrameworkCore;
using MilsimManager;
using MilsimManager.Models;
using MilsimManager.Services;
using MudBlazor;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment()) {
    builder.Configuration.AddJsonFile("appsettings.Secret.json", optional: true, reloadOnChange: true);
}

var envConnectionString = builder.Configuration.GetConnectionString("Default") ?? throw new InvalidOperationException("Missing Connection String");
var envAuthDiscordClientId = builder.Configuration["Authentication:Discord:ClientId"];
var envAuthDiscordSecret = builder.Configuration["Authentication:Discord:ClientSecret"];
var envAuthDiscordEnabled = true;
if (builder.Environment.IsDevelopment()) envAuthDiscordEnabled = builder.Configuration.GetValue<bool?>("Authentication:Discord:Enabled") ?? true;
if (envAuthDiscordEnabled) {
    if (string.IsNullOrWhiteSpace(envAuthDiscordClientId)) throw new InvalidOperationException("Missing Authentication:Discord:ClientId");
    if (string.IsNullOrWhiteSpace(envAuthDiscordSecret)) throw new InvalidOperationException("Missing Authentication:Discord:ClientSecret");
}

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddDbContextFactory<Context>(options => options.UseNpgsql(envConnectionString));
builder.Services.AddMudServices(config => {
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = false;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 10000;
    config.SnackbarConfiguration.HideTransitionDuration = 0;
    config.SnackbarConfiguration.ShowTransitionDuration = 500;
    config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
});


builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUnitService, UnitService>();
builder.Services.AddScoped<ICertificationService, CertificationService>();
builder.Services.AddScoped<IAwardService, AwardService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IRankService, RankService>();
builder.Services.AddScoped<IDevService, DevService>();
builder.Services.AddScoped<IClipboardService, ClipboardService>();
builder.Services.AddScoped<IErrorHandler, ErrorHandler>();


var authenticationBuilder = builder.Services.AddAuthentication(options => {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = builder.Environment.IsDevelopment() ? CookieAuthenticationDefaults.AuthenticationScheme : DiscordAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options => {
        options.Cookie.HttpOnly = true;
        options.Cookie.Name = "auth_token";
        options.LoginPath = builder.Environment.IsDevelopment() ? "/dev" : "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/403";

        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(4);

        /* todo fixme issue
            If I open two separate tabs that Im logged in on and I logout on one of them, I dont get logged out on the other ones
            see here for what to possibly do:
            https://learn.microsoft.com/en-us/aspnet/core/blazor/security/?view=aspnetcore-8.0&tabs=net-cli
        */

        options.Events = new CookieAuthenticationEvents {
            OnValidatePrincipal = async context => {
                var userIdRaw = context.Principal?.FindFirstValue("auth_UserId");
                if (!int.TryParse(userIdRaw, out var userId)) {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return;
                }

                await using var db = await context.HttpContext.RequestServices.GetRequiredService<IDbContextFactory<Context>>().CreateDbContextAsync();

                User user;
                try {
                    user = await db.Users.AsNoTracking().SingleAsync(u => u.Id == userId);
                } catch (InvalidOperationException) {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return;
                }

                var identity = (ClaimsIdentity)context.Principal!.Identity!;

                if (context.Principal.IsInRole("Active") == user.Active && context.Principal.IsInRole("Admin") == user.Admin) return;

                var newIdentity = new ClaimsIdentity(
                identity.Claims.Where(c => c.Type != identity.RoleClaimType).ToList(),
                identity.AuthenticationType,
                identity.NameClaimType,
                identity.RoleClaimType
                );

                if (user.Active) newIdentity.AddClaim(new Claim(newIdentity.RoleClaimType, "Active"));
                if (user.Admin) newIdentity.AddClaim(new Claim(newIdentity.RoleClaimType, "Admin"));

                context.ReplacePrincipal(new ClaimsPrincipal(newIdentity));
                context.ShouldRenew = true;
            }
        };
    });
if (envAuthDiscordEnabled)
    authenticationBuilder.AddDiscord(options => {
        options.ClientId = envAuthDiscordClientId!;
        options.ClientSecret = envAuthDiscordSecret!;
        options.CallbackPath = "/signin-discord";

        options.Scope.Clear();
        options.Scope.Add("identify");
        // options.SaveTokens = true; //todo this might be needed later for steam connection

        options.Events = new OAuthEvents {
            OnCreatingTicket = async context => {
                await using var db = await context.HttpContext.RequestServices.GetRequiredService<IDbContextFactory<Context>>().CreateDbContextAsync();

                var discordId = context.User.GetProperty("id").GetString();
                if (string.IsNullOrWhiteSpace(discordId)) throw new InvalidOperationException("Discord profile didn't include an id");

                var user = await db.Users.SingleOrDefaultAsync(u => u.DiscordId == discordId);

                if (user is null) {
                    context.User.TryGetProperty("username", out var username);
                    context.User.TryGetProperty("global_name", out var globalName);
                    var displayName = username.GetString();
                    if (string.IsNullOrWhiteSpace(displayName)) displayName = globalName.GetString();
                    if (string.IsNullOrWhiteSpace(displayName)) displayName = discordId;
                    displayName = displayName.Trim();
                    if (displayName.Length > 32) displayName = displayName[..32];

                    var i = 1;
                    while (await db.Users.AnyAsync(u => u.Name == displayName)) {
                        displayName = displayName[..(32 - i.ToString().Length)] + i;
                        i++;
                        if (i >= 100) throw new Exception("Couldn't find a non-conflicting name for new user");
                    }
                    user = new User {
                        Name = displayName,
                        DiscordId = discordId,
                        DateJoined = DateTime.UtcNow,
                        Active = false,
                        Admin = false
                    };
                    db.Users.Add(user);
                    await db.SaveChangesAsync();
                }

                if (!user.Admin) {
                    var adminIds = context.HttpContext.RequestServices
                        .GetRequiredService<IConfiguration>()
                        .GetSection("Auth:AdminDiscordIds")
                        .Get<string[]>() ?? [];
                    if (adminIds.Contains(discordId, StringComparer.Ordinal)) {
                        user.Admin = true;
                        await db.SaveChangesAsync();
                    }
                }

                var identity = (ClaimsIdentity)context.Principal!.Identity!;
                identity.AddClaim(new Claim("auth_UserId", user.Id.ToString()));
                identity.AddClaim(new Claim("discord_id", discordId));

                if (user.Active) identity.AddClaim(new Claim(identity.RoleClaimType, "Active"));
                if (user.Admin) identity.AddClaim(new Claim(identity.RoleClaimType, "Admin"));
            }
        };
    });

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Active", policy => policy
        .RequireAuthenticatedUser()
        .RequireRole("Active"))
    .AddPolicy("Admin", policy => policy
        .RequireAuthenticatedUser()
        .RequireRole("Active")
        .RequireRole("Admin"));

var app = builder.Build();

if (app.Environment.IsDevelopment()) {
    using var scope = app.Services.CreateScope();
    var dev = scope.ServiceProvider.GetRequiredService<IDevService>();
    await dev.ResetAsync();
}

if (!app.Environment.IsDevelopment()) {
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}


app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/login", async (HttpContext http, string? returnUrl) => {
    var redirectUri = !string.IsNullOrWhiteSpace(returnUrl) && returnUrl.StartsWith('/') ? returnUrl : "/";
    if (!envAuthDiscordEnabled) return Results.Redirect($"/dev?ReturnUrl={Uri.EscapeDataString(redirectUri)}");
    await http.ChallengeAsync(DiscordAuthenticationDefaults.AuthenticationScheme, new AuthenticationProperties { RedirectUri = redirectUri });
    return Results.Empty;
}).AllowAnonymous();


app.MapGet("/logout", async http => {
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    http.Response.Redirect("/");
}).AllowAnonymous();

if (app.Environment.IsDevelopment() || true)
    app.MapGet("/dev/login", async (
        HttpContext http,
        IDbContextFactory<Context> dbFactory,
        int? userId,
        int? authLevel,
        string? returnUrl
    ) => {
        await using var db = await dbFactory.CreateDbContextAsync();

        var user = userId is null
            ? await db.Users.OrderBy(u => u.Id).FirstOrDefaultAsync()
            : await db.Users.SingleOrDefaultAsync(u => u.Id == userId);
        if (user is null) return Results.BadRequest($"Could not login as user Id={userId}");

        var changed = false;

        if (authLevel is not null) {
            switch (authLevel) {
                case 0:
                    if (user.Active) {
                        user.Active = false;
                        changed = true;
                    }
                    if (user.Admin) {
                        user.Admin = false;
                        changed = true;
                    }
                    break;
                case 1:
                    if (!user.Active) {
                        user.Active = true;
                        changed = true;
                    }
                    if (user.Admin) {
                        user.Admin = false;
                        changed = true;
                    }
                    break;
                case 2:
                    if (!user.Active) {
                        user.Active = true;
                        changed = true;
                    }
                    if (!user.Admin) {
                        user.Admin = true;
                        changed = true;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(authLevel));
            }
            if (changed) await db.SaveChangesAsync();
        }

        var claims = new List<Claim> {
            new("auth_UserId", user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
        };

        if (!string.IsNullOrWhiteSpace(user.DiscordId)) claims.Add(new Claim("discord_id", user.DiscordId));

        if (user.Active) claims.Add(new Claim(ClaimTypes.Role, "Active"));
        if (user.Admin) claims.Add(new Claim(ClaimTypes.Role, "Admin"));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return Results.Redirect(!string.IsNullOrWhiteSpace(returnUrl) && returnUrl.StartsWith('/') ? returnUrl : "/");
    }).AllowAnonymous();

app.UseStatusCodePagesWithRedirects("/{0}");

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
