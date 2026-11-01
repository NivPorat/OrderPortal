using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace OrderPortal.IntegrationTests;

// One AppHost (Postgres, Redis, RabbitMQ + services) shared by every test in the collection.
public sealed class AppHostFixture : IAsyncLifetime
{
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(3);

    private DistributedApplication? _app;

    public HttpClient ApiClient { get; private set; } = null!;
    public HttpClient OrderClient { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.OrderPortal_AppHost>(TestContext.Current.CancellationToken);

        _app = await builder.BuildAsync(TestContext.Current.CancellationToken);
        await _app.StartAsync(TestContext.Current.CancellationToken);

        // Fail fast with the name of the broken resource instead of hanging until a test times out.
        foreach (var resource in new[] { "postgres", "redis", "messaging", "apiservice" })
        {
            await WaitHealthyAsync(resource);
        }
        await WaitRunningAsync("orderservice");

        ApiClient = _app.CreateHttpClient("apiservice");

        // OrderService calls UseHttpsRedirection, so talk to its https endpoint (random port, dev cert).
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        OrderClient = new HttpClient(handler) { BaseAddress = _app.GetEndpoint("orderservice", "https") };
    }

    private async Task WaitHealthyAsync(string resource)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(StartupTimeout);
        try
        {
            await _app!.ResourceNotifications.WaitForResourceHealthyAsync(resource, cts.Token);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Resource '{resource}' did not become healthy within {StartupTimeout}: {ex.Message}", ex);
        }
    }

    private async Task WaitRunningAsync(string resource)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(StartupTimeout);
        try
        {
            await _app!.ResourceNotifications.WaitForResourceAsync(resource, KnownResourceStates.Running, cts.Token);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Resource '{resource}' did not reach Running within {StartupTimeout}: {ex.Message}", ex);
        }
    }

    public async ValueTask DisposeAsync()
    {
        ApiClient?.Dispose();
        OrderClient?.Dispose();
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }
}

[CollectionDefinition(Name)]
public sealed class AppHostCollection : ICollectionFixture<AppHostFixture>
{
    public const string Name = "AppHost";
}
