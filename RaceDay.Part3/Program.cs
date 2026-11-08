
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using RaceDay.Part3.Services;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. MVC SERVICES
// ==========================================

builder.Services.AddControllersWithViews();

// ==========================================
// 2. PART 2 REST API CONNECTION
// ==========================================

builder.Services.AddHttpClient("RaceDayApi", client =>
{
    var apiUrl =
        builder.Configuration["ApiSettings:BaseUrl"];

    if (string.IsNullOrWhiteSpace(apiUrl))
    {
        throw new InvalidOperationException(
            "RaceDay API address is missing.");
    }

    client.BaseAddress = new Uri(apiUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() =>
    new HttpClientHandler
    {
        // Prevent shared HttpClient handlers from
        // storing authentication cookies.
        UseCookies = false,

        // Handle authentication redirects explicitly.
        AllowAutoRedirect = false
    });

// ==========================================
// 3. REGISTER CUSTOM SERVICES
// ==========================================

builder.Services.AddScoped<RaceDayApiService>();

// Server-side temporary API session storage.
builder.Services.AddMemoryCache();

builder.Services.AddSingleton<ApiSessionStore>();

// ==========================================
// 4. COOKIE AUTHENTICATION
// ==========================================

builder.Services
    .AddAuthentication(
        CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";

        options.AccessDeniedPath =
            "/Account/AccessDenied";

        options.Cookie.Name = "RaceDay.Part3.Auth";

        options.Cookie.HttpOnly = true;

        options.Cookie.SecurePolicy =
            CookieSecurePolicy.Always;

        options.Cookie.SameSite = SameSiteMode.Lax;

        options.ExpireTimeSpan =
            TimeSpan.FromHours(2);

        // Do not keep extending a frontend session
        // beyond the API session's fixed lifetime.
        options.SlidingExpiration = false;

        // ======================================
        // VALIDATE SERVER-SIDE SESSION
        // ======================================

        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                var sessionId = context.Principal?
                    .FindFirst("ApiSessionId")?.Value;

                var sessionStore = context.HttpContext
                    .RequestServices
                    .GetRequiredService<ApiSessionStore>();

                var apiCookie =
                    sessionStore.GetApiCookie(sessionId);

                // Reject frontend authentication if
                // its server-side API session is missing.
                if (string.IsNullOrWhiteSpace(apiCookie))
                {
                    context.RejectPrincipal();

                    await context.HttpContext.SignOutAsync(
                        CookieAuthenticationDefaults
                            .AuthenticationScheme);
                }
            }
        };
    });

// ==========================================
// 5. AUTHORISATION
// ==========================================

builder.Services.AddAuthorization();

// ==========================================
// 6. BUILD APPLICATION
// ==========================================

var app = builder.Build();

// ==========================================
// 7. ERROR HANDLING
// ==========================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");

    app.UseHsts();
}

// ==========================================
// 8. MIDDLEWARE
// ==========================================

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

// Authentication must run before authorisation.
app.UseAuthentication();

app.UseAuthorization();

// ==========================================
// 9. MVC ROUTING
// ==========================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ==========================================
// 10. START RACEDAY PART 3
// ==========================================

app.Run();
