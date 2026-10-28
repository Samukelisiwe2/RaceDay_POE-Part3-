
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
    var apiUrl = builder.Configuration["ApiSettings:BaseUrl"];

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
        // storing other users' authentication cookies.
        UseCookies = false,

        // Let our service handle authentication
        // responses without automatic redirects.
        AllowAutoRedirect = false
    });

// ==========================================
// 3. REGISTER CUSTOM API SERVICE
// ==========================================

builder.Services.AddScoped<RaceDayApiService>();

// ==========================================
// 4. COOKIE AUTHENTICATION
// ==========================================

builder.Services
    .AddAuthentication(
        CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";

        options.Cookie.Name = "RaceDay.Part3.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy =
            CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;

        options.ExpireTimeSpan =
            TimeSpan.FromHours(2);

        options.SlidingExpiration = true;
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
// 10. START APPLICATION
// ==========================================

app.Run();

