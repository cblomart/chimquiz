using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.Sqlite;
using ChimQuiz.Api;
using ChimQuiz.Data;
using ChimQuiz.Middleware;
using ChimQuiz.Services;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

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
    options.Cookie.Name = "chimquiz_session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromHours(2);
});

// ── EF Core / SQLite ─────────────────────────────────────────────────────────
// Azure Files SMB doesn't support POSIX fcntl advisory locks that SQLite uses
// for every write commit. Workaround: operate on a local /tmp copy of the DB
// (where fcntl works) and sync back to Azure Files on shutdown + periodically.
string dbPath = builder.Configuration["DatabasePath"] ?? "chimquiz.db";
bool useLocalCopy = OperatingSystem.IsLinux() && Path.IsPathRooted(dbPath);
string workingDbPath = useLocalCopy ? "/tmp/chimquiz_work.db" : dbPath;

if (useLocalCopy && File.Exists(dbPath))
{
    File.Copy(dbPath, workingDbPath, overwrite: true);
}

SqliteConnection sqliteConnection = new($"Data Source={workingDbPath}");
sqliteConnection.Open();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(sqliteConnection));

// ── Application Services ─────────────────────────────────────────────────────
builder.Services.AddSingleton<ElementService>();
builder.Services.AddScoped<PlayerService>();
builder.Services.AddScoped<QuizService>();

// ── Rate Limiting ─────────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    _ = options.AddFixedWindowLimiter("api", limiterOptions =>
    {
        limiterOptions.PermitLimit = 120;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });
});

// ── Antiforgery ───────────────────────────────────────────────────────────────
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

WebApplication app = builder.Build();

// ── Error handling ────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    _ = app.UseDeveloperExceptionPage();
}
else
{
    _ = app.UseExceptionHandler("/Error");
    _ = app.UseHsts();
    _ = app.UseHttpsRedirection();
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
RouteGroupBuilder api = app.MapGroup("/api")
    .RequireRateLimiting("api")
    .DisableAntiforgery();

api.MapPlayerApi();
api.MapQuizApi();
api.MapLeaderboardApi();

// ── Database initialisation ───────────────────────────────────────────────────
using (IServiceScope scope = app.Services.CreateScope())
{
    AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    _ = db.Database.EnsureCreated();
}

// ── Azure Files sync ─────────────────────────────────────────────────────────
// Sync the working DB back to Azure Files on shutdown and every 5 minutes.
if (useLocalCopy)
{
    app.Lifetime.ApplicationStopping.Register(SyncToAzureFiles);

    _ = Task.Run(async () =>
    {
        using PeriodicTimer timer = new(TimeSpan.FromMinutes(5));
        while (await timer.WaitForNextTickAsync())
        {
            SyncToAzureFiles();
        }
    });
}

app.Run();

void SyncToAzureFiles()
{
    try
    {
        using SqliteCommand cmd = sqliteConnection.CreateCommand();
        cmd.CommandText = $"VACUUM INTO '{dbPath}'"; // nosemgrep: csharp-sqli
        _ = cmd.ExecuteNonQuery();
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[chimquiz] Azure Files sync failed: {ex.Message}");
    }
}
