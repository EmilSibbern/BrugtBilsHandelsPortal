using BrugtBilsHandelsPortal.Components;
using BrugtBilsHandelsPortal.Data.Connections;
using BrugtBilsHandelsPortal.Data.Repositories.Implementations;
using BrugtBilsHandelsPortal.Data.Repositories.Interfaces;
using BrugtBilsHandelsPortal.Endpoints;
using BrugtBilsHandelsPortal.Models.Entities;
using BrugtBilsHandelsPortal.Services.Implementations;
using BrugtBilsHandelsPortal.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using BrugtBilsHandelsPortal.Security;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using BrugtBilsHandelsPortal.Configuration;
using BrugtBilsHandelsPortal.Storage;
var builder = WebApplication.CreateBuilder(args);

// Razor Components og Interactive Server
builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

// Databaseforbindelse
builder.Services.AddScoped<
    IDbConnectionFactory,
    SqlConnectionFactory>();

// Repositories
builder.Services.AddScoped<
    IUserRepository,
    UserRepository>();

// Password hashing
builder.Services.AddScoped<
    IPasswordHasher<User>,
    PasswordHasher<User>>();

// Services
builder.Services.AddScoped<
    IAuthService,
    AuthService>();

builder.Services.AddScoped<
    ICarBrandRepository,
    CarBrandRepository>();

builder.Services.AddScoped<
    IAnnouncementRepository,
    AnnouncementRepository>();

builder.Services.AddScoped<
    IAnnouncementService,
    AnnouncementService>();

builder.Services.Configure<ImageUploadOptions>(
    builder.Configuration.GetSection(
        ImageUploadOptions.SectionName));

builder.Services.AddSingleton<
    IImageStorage,
    LocalImageStorage>();

builder.Services.AddScoped<
    IImageRepository,
    ImageRepository>();

builder.Services.AddScoped<
    IImageService,
    ImageService>();

builder.Services.AddScoped<
    ICommentRepository,
    CommentRepository>();

builder.Services.AddScoped<
    ICommentService,
    CommentService>();

builder.Services.AddScoped<
    IMessageRepository,
    MessageRepository>();

builder.Services.AddScoped<
    IMessageService,
    MessageService>();

// Cookie authentication
builder.Services
    .AddAuthentication(
        CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/AccessDenied";

        options.Cookie.Name =
            "BrugtBilsHandelsPortal.Authentication";

        // JavaScript må ikke kunne læse cookien.
        options.Cookie.HttpOnly = true;

        // Begrænser cookien ved cross-site requests.
        options.Cookie.SameSite =
            SameSiteMode.Lax;

        // Cookien sendes kun over HTTPS.
        options.Cookie.SecurePolicy =
            CookieSecurePolicy.Always;

        // Standardlevetid for authentication-ticket.
        options.ExpireTimeSpan =
            TimeSpan.FromHours(8);

        // Forlænger levetiden, når brugeren er aktiv.
        options.SlidingExpiration = true;
    });

builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IUserService, UserService>();

// Authorization og authentication state til Blazor
builder.Services.AddAuthorization();

builder.Services.AddCascadingAuthenticationState();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    options.AddPolicy(
        "login",
        httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey:
                httpContext.Connection.RemoteIpAddress?
                    .ToString()
                ?? "unknown",

                factory: _ =>
                    new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));
});

var app = builder.Build();

// Fejlhåndtering i produktion
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(
        "/Error",
        createScopeForErrors: true);

    app.UseHsts();
}

// 404-side
app.UseStatusCodePagesWithReExecute(
    "/not-found",
    createScopeForStatusCodePages: true);

// HTTP sendes videre til HTTPS
app.UseHttpsRedirection();

app.UseRouting();

app.UseRateLimiter();

// Læs authentication-cookien og opret HttpContext.User
app.UseAuthentication();

// Kontrollér authorization-regler
app.UseAuthorization();

// Beskyt formularer mod CSRF
app.UseAntiforgery();

app.UseStaticFiles();

// Statiske filer fra wwwroot
app.MapStaticAssets();

// Login- og senere logout-endpoints
app.MapAccountEndpoints();

// Blazor-applikationen
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();