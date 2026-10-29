using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using OrderPortal.OrderService.Interfaces;
using OrderPortal.OrderService.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OrderPortal.OrderService.Services;
using RabbitMQ.Client;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. ALL SERVICE REGISTRATIONS 
// ==========================================
builder.AddServiceDefaults();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

string dbConnectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
builder.Services.AddDbContext<OrderDbContext>(options => options.UseNpgsql(dbConnectionString));

builder.Services.Configure<OrderSettingsConfig>(builder.Configuration.GetSection("OrderSettings"));
builder.Services.Configure<JwtSettingsConfig>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<MessageQueueConfig>(builder.Configuration.GetSection("MessageQueueSettings"));

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

builder.Services.AddSingleton<IConnection>(sp =>
{
    var factory = new ConnectionFactory
    {
        HostName = builder.Configuration["RabbitMQ:HostName"] ?? "localhost"
    };

    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
});

builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
builder.Services.AddHostedService<OrderEventsWorker>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],

            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],

            ValidateLifetime = true,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!))
        };
    });

builder.Services.AddAuthorization();

// ==========================================
// 2. BUILD THE APPLICATION
// ==========================================
var app = builder.Build();

// Database migration scope
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    await dbContext.Database.MigrateAsync();
}

// ==========================================
// 3. HTTP REQUEST PIPELINE (Middleware)
// ==========================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Authentication MUST come before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();