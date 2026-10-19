using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using OrderPortal.OrderService.Interfaces;
using OrderPortal.OrderService.Models;
using OrderPortal.OrderService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceDefaults();
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
string dbConnectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
builder.Services.AddDbContext<OrderDbContext>(options => options.UseNpgsql(dbConnectionString));
builder.Services.Configure<OrderSettingsConfig>(builder.Configuration.GetSection("OrderSettings"));
builder.Services.AddScoped<IOrderLogic, OrderLogicService>();
builder.Services.AddHttpClient<IProductClientService, ProductClientService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration.GetConnectionString("productservice") ?? "https://localhost:5001");
})
.AddServiceDiscovery()
.AddStandardResilienceHandler(options =>
{
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
    options.Retry.MaxRetryAttempts = 3;
});

var app = builder.Build();
app.MapControllers();
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    await dbContext.Database.MigrateAsync();
}
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
