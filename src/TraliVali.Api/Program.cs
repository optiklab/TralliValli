using Serilog;
using StackExchange.Redis;
using TraliVali.Auth;
using TraliVali.Domain.Entities;
using TraliVali.Infrastructure.Data;
using TraliVali.Infrastructure.Repositories;
using TraliVali.Messaging;

// Configure Serilog bootstrap logger for startup errors
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting TraliVali API");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog from appsettings.json
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // Add controllers
    builder.Services.AddControllers();

    // Add services to the container.
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Configure MongoDB
    var mongoConnectionString = builder.Configuration.GetValue<string>("MongoDB:ConnectionString") 
        ?? Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING")
        ?? "mongodb://admin:password@localhost:27017/tralivali?authSource=admin";
    var mongoDatabaseName = builder.Configuration.GetValue<string>("MongoDB:DatabaseName") ?? "tralivali";
    builder.Services.AddSingleton(new MongoDbContext(mongoConnectionString, mongoDatabaseName));

    // Configure Redis
    var redisConnectionString = builder.Configuration.GetValue<string>("Redis:ConnectionString")
        ?? Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING")
        ?? "localhost:6379,password=password";
    builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
        ConnectionMultiplexer.Connect(redisConnectionString));

    // Configure JWT Settings
    var jwtSettings = new JwtSettings
    {
        PrivateKey = builder.Configuration.GetValue<string>("Jwt:PrivateKey") ?? "",
        PublicKey = builder.Configuration.GetValue<string>("Jwt:PublicKey") ?? "",
        Issuer = builder.Configuration.GetValue<string>("Jwt:Issuer") ?? "TraliVali",
        Audience = builder.Configuration.GetValue<string>("Jwt:Audience") ?? "TraliVali",
        ExpirationDays = builder.Configuration.GetValue<int>("Jwt:ExpirationDays", 7),
        RefreshTokenExpirationDays = builder.Configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays", 30)
    };
    builder.Services.AddSingleton(jwtSettings);

    // Configure Azure Communication Email Service
    var emailConfig = new AzureCommunicationEmailConfiguration
    {
        ConnectionString = builder.Configuration.GetValue<string>("AzureCommunicationEmail:ConnectionString") ?? "",
        SenderAddress = builder.Configuration.GetValue<string>("AzureCommunicationEmail:SenderAddress") ?? "",
        SenderName = builder.Configuration.GetValue<string>("AzureCommunicationEmail:SenderName") ?? "TraliVali"
    };
    builder.Services.AddSingleton(emailConfig);

    // Register services
    builder.Services.AddSingleton<ITokenBlacklistService, TokenBlacklistService>();
    builder.Services.AddSingleton<IJwtService, JwtService>();
    builder.Services.AddSingleton<IMagicLinkService, MagicLinkService>();
    builder.Services.AddSingleton<IEmailService, AzureCommunicationEmailService>();

    // Register repositories
    builder.Services.AddSingleton<IRepository<User>, UserRepository>();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Add Serilog request logging
    app.UseSerilogRequestLogging();

    app.UseHttpsRedirection();

    // Map controllers
    app.MapControllers();

    var summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    app.MapGet("/weatherforecast", () =>
    {
        var forecast =  Enumerable.Range(1, 5).Select(index =>
            new WeatherForecast
            (
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Random.Shared.Next(-20, 55),
                summaries[Random.Shared.Next(summaries.Length)]
            ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast")
    .WithOpenApi();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "TraliVali API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
