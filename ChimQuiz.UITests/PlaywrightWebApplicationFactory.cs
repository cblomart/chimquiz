using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace ChimQuiz.UITests;

/// <summary>
/// Starts a real Kestrel server (needed by Playwright) using WebApplicationFactory.
/// Uses the "UITest" environment so Program.cs switches to InMemory EF Core.
/// </summary>
public sealed class PlaywrightWebApplicationFactory : WebApplicationFactory<Program>
{
    private IHost? _kestrelHost;

    public string ServerAddress { get; private set; } = null!;

    public IBrowser Browser { get; private set; } = null!;
    private IPlaywright? _playwright;

    // ── WebApplicationFactory configuration ───────────────────────────────────

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseEnvironment("UITest");

    // ── Override CreateHost to also spin up a real Kestrel server ─────────────
    //
    // WebApplicationFactory uses an in-process TestServer by default.
    // Playwright launches a real browser process that needs a real TCP port.
    // We find a free port up front, then bind Kestrel to it explicitly.

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Build the TestServer host required by WebApplicationFactory internals
        var testHost = base.CreateHost(builder);

        // Find a free port to avoid conflicts
        int port = FindFreePort();
        ServerAddress = $"http://127.0.0.1:{port}";

        // Configure and start a real Kestrel host on that port
        builder.ConfigureWebHost(wb =>
            wb.UseKestrel(o => o.Listen(IPAddress.Loopback, port)));

        _kestrelHost = builder.Build();
        _kestrelHost.Start();

        return testHost;
    }

    private static int FindFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    // ── Playwright browser lifecycle ──────────────────────────────────────────

    public async Task InitPlaywrightAsync()
    {
        _ = CreateDefaultClient(); // Triggers host startup
        _playwright = await Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync(
            new BrowserTypeLaunchOptions { Headless = true });
    }

    // ── Cleanup ───────────────────────────────────────────────────────────────

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Browser?.DisposeAsync().AsTask().GetAwaiter().GetResult();
            _playwright?.Dispose();
            _kestrelHost?.Dispose();
        }
        base.Dispose(disposing);
    }
}
