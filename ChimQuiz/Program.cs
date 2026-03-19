using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using ChimQuiz.Api;
using ChimQuiz.Data;
using ChimQuiz.Middleware;
using ChimQuiz.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Razor Pages ──────────────────────────────────────────────────────────────
builder.Services.AddRazorPages();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase));
});

// ── Session ───────────────────────────────────────────────────────────────────
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name       = "chimquiz_session";
    options.Cookie.HttpOnly   = true;
    options.Cookie.SameSite   = SameSiteMode.Strict;
    options.Cookie.IsEssential = true;
    options.IdleTimeout       = TimeSpan.FromHours(2);
});

// ── EF Core / SQLite ─────────────────────────────────────────────────────────
var dbPath = builder.Configuration["DatabasePath"] ?? "chimquiz.db";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// ── Application Services ─────────────────────────────────────────────────────
builder.Services.AddSingleton<ElementService>();
builder.Services.AddScoped<PlayerService>();
builder.Services.AddScoped<QuizService>();

// ── Rate Limiting ─────────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("api", limiterOptions =>
    {
        limiterOptions.PermitLimit        = 120;
        limiterOptions.Window             = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit         = 0;
    });
});

// ── Antiforgery ───────────────────────────────────────────────────────────────
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

var app = builder.Build();

// ── Error handling ────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

// ── Middleware pipeline ───────────────────────────────────────────────────────
app.UseSecurityHeaders();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseSession();
app.UseAntiforgery();

// ── Razor Pages ───────────────────────────────────────────────────────────────
app.MapRazorPages();

// ── API routes ────────────────────────────────────────────────────────────────
var api = app.MapGroup("/api")
    .RequireRateLimiting("api")
    .DisableAntiforgery();

api.MapPlayerApi();
api.MapQuizApi();
api.MapLeaderboardApi();

// ── Database initialisation ───────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.Run();
