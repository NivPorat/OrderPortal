using Microsoft.EntityFrameworkCore;
using OrderPortal.ApiService;
using OrderPortal.ApiService.Models;
using OrderPortal.ApiService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddControllers();

builder.AddRedisDistributedCache("redis");

builder.AddNpgsqlDbContext<OrderDbContext>("ProductConnection");

builder.Services.Configure<ProductSettingsConfig>(builder.Configuration.GetSection("ProductSettings"));
builder.Services.AddScoped<IProductLogic, ProductLogicService>();
builder.Services.AddScoped<CachingService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.MapControllers();
app.UseExceptionHandler();
app.MapDefaultEndpoints();

app.Run();