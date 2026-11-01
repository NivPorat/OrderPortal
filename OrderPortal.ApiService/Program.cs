using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Hosting;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using OrderPortal.ApiService;
using OrderPortal.ApiService.Models;
using OrderPortal.ApiService.Services;
using System;
using System.Text;
using RabbitMQ.AMQP.Client;
using RabbitMQ.AMQP.Client.Impl;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.AddRedisDistributedCache("redis");
string dbConnectionString = builder.Configuration.GetConnectionString("ProductConnection")
                           ?? builder.Configuration.GetConnectionString("DefaultConnection")
                           ?? string.Empty;

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(dbConnectionString));
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
// Configure the HTTP request pipeline.
app.UseExceptionHandler();


app.MapDefaultEndpoints();

app.Run();

