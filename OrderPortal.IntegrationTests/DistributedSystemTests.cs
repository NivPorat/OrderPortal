using System.Net;
using System.Net.Http.Json;

namespace OrderPortal.IntegrationTests;

[Collection(AppHostCollection.Name)]
public class DistributedSystemTests(AppHostFixture fixture)
{
    private record ProductDto(string Id, string Name, double Price);
    private record OrderDto(string Id, string ProductId, int Quantity, double TotalPrice);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static string NewId() => "it-" + Guid.NewGuid().ToString("N");

    [Fact]
    public async Task Product_IsPersistedInPostgres()
    {
        var product = new ProductDto(NewId(), "Persisted widget", 12.5);
        try
        {
            var create = await fixture.ApiClient.PostAsJsonAsync("/api/product", product, Ct);
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);

            // The list endpoint reads straight from PostgreSQL, bypassing the Redis cache.
            var all = await fixture.ApiClient.GetFromJsonAsync<List<ProductDto>>("/api/product", Ct);
            Assert.Contains(all!, p => p.Id == product.Id && p.Name == product.Name && p.Price == product.Price);

            var duplicate = await fixture.ApiClient.PostAsJsonAsync("/api/product", product, Ct);
            Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        }
        finally
        {
            await fixture.ApiClient.DeleteAsync($"/api/product/{product.Id}", Ct);
        }
    }

    [Fact]
    public async Task ValidOrder_IsAcceptedThenPersistedAsynchronously()
    {
        var product = new ProductDto(NewId(), "Ordered widget", 10);
        try
        {
            var create = await fixture.ApiClient.PostAsJsonAsync("/api/product", product, Ct);
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);

            var response = await fixture.OrderClient.PostAsJsonAsync("/order", new { productId = product.Id, quantity = 3 }, Ct);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var accepted = await response.Content.ReadFromJsonAsync<OrderDto>(Ct);
            Assert.NotNull(accepted);

            // The order row is written by the RabbitMQ consumer, so poll for the observable result.
            var stored = await PollOrderAsync(accepted.Id, TimeSpan.FromSeconds(30));

            Assert.Equal(product.Id, stored.ProductId);
            Assert.Equal(3, stored.Quantity);
            Assert.Equal(30, stored.TotalPrice);
        }
        finally
        {
            await fixture.ApiClient.DeleteAsync($"/api/product/{product.Id}", Ct);
        }
    }

    [Fact]
    public async Task OrderForUnknownProduct_IsRejectedWithNotFound()
    {
        var response = await fixture.OrderClient.PostAsJsonAsync("/order", new { productId = NewId(), quantity = 1 }, Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("does not exist", await response.Content.ReadAsStringAsync(Ct));
    }

    private async Task<OrderDto> PollOrderAsync(string orderId, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        HttpStatusCode last;
        do
        {
            var response = await fixture.OrderClient.GetAsync($"/order/{orderId}", Ct);
            last = response.StatusCode;
            if (last == HttpStatusCode.OK)
            {
                return (await response.Content.ReadFromJsonAsync<OrderDto>(Ct))!;
            }
            await Task.Delay(TimeSpan.FromMilliseconds(250), Ct);
        } while (DateTime.UtcNow < deadline);

        Assert.Fail($"Order {orderId} was not persisted within {timeout.TotalSeconds}s (last GET /order status: {last}). " +
                    "Check that RabbitMQ delivered the OrderCreated event and the OrderEventsWorker saved it to PostgreSQL.");
        return null!;
    }
}
