using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using StackExchange.Redis;
using TraliVali.Api.Hubs;
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

    // Add SignalR
    builder.Services.AddSignalR();

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
    var jwtPrivateKey = builder.Configuration.GetValue<string>("Jwt:PrivateKey") 
        ?? Environment.GetEnvironmentVariable("JWT_PRIVATE_KEY") ?? "";
    var jwtPublicKey = builder.Configuration.GetValue<string>("Jwt:PublicKey") 
        ?? Environment.GetEnvironmentVariable("JWT_PUBLIC_KEY") ?? "";
    
    if (string.IsNullOrWhiteSpace(jwtPrivateKey) || string.IsNullOrWhiteSpace(jwtPublicKey))
    {
        throw new InvalidOperationException("JWT keys are required. Please configure Jwt:PrivateKey and Jwt:PublicKey in appsettings.json or set JWT_PRIVATE_KEY and JWT_PUBLIC_KEY environment variables.");
    }
    
    var jwtSettings = new JwtSettings
    {
        PrivateKey = jwtPrivateKey,
        PublicKey = jwtPublicKey,
        Issuer = builder.Configuration.GetValue<string>("Jwt:Issuer") ?? "TraliVali",
        Audience = builder.Configuration.GetValue<string>("Jwt:Audience") ?? "TraliVali",
        ExpirationDays = builder.Configuration.GetValue<int>("Jwt:ExpirationDays", 7),
        RefreshTokenExpirationDays = builder.Configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays", 30)
    };
    builder.Services.AddSingleton(jwtSettings);

    // Configure JWT Authentication with RSA256 signing
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        // Create RSA key for validation
        // Note: RSA instance is not explicitly disposed as it needs to live for the application lifetime
        // It will be cleaned up when the application shuts down
        var rsa = RSA.Create();
        rsa.ImportFromPem(jwtPublicKey);
        var validationKey = new RsaSecurityKey(rsa);
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = validationKey,
            ClockSkew = TimeSpan.Zero
        };

        // Configure SignalR to use JWT from query string
        // Note: This is necessary for WebSocket connections which can't use headers
        // Security tradeoff: tokens may appear in logs, but this is standard for SignalR
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];

                // If the request is for the hub endpoint
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/chat"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

    // Configure Azure Communication Email Service
    var emailConnectionString = builder.Configuration.GetValue<string>("AzureCommunicationEmail:ConnectionString") 
        ?? Environment.GetEnvironmentVariable("AZURE_COMMUNICATION_EMAIL_CONNECTION_STRING") ?? "";
    var emailSenderAddress = builder.Configuration.GetValue<string>("AzureCommunicationEmail:SenderAddress") 
        ?? Environment.GetEnvironmentVariable("EMAIL_SENDER_ADDRESS") ?? "";
    
    if (string.IsNullOrWhiteSpace(emailConnectionString) || string.IsNullOrWhiteSpace(emailSenderAddress))
    {
        Log.Warning("Azure Communication Email is not configured. Email functionality will not work. Please configure AzureCommunicationEmail:ConnectionString and AzureCommunicationEmail:SenderAddress.");
    }
    
    var emailConfig = new AzureCommunicationEmailConfiguration
    {
        ConnectionString = emailConnectionString,
        SenderAddress = emailSenderAddress,
        SenderName = builder.Configuration.GetValue<string>("AzureCommunicationEmail:SenderName") ?? "TraliVali"
    };
    builder.Services.AddSingleton(emailConfig);

    // Register services
    builder.Services.AddSingleton<ITokenBlacklistService, TokenBlacklistService>();
    builder.Services.AddSingleton<IPresenceService, PresenceService>();
    builder.Services.AddSingleton<IJwtService, JwtService>();
    builder.Services.AddSingleton<IMagicLinkService, MagicLinkService>();
    builder.Services.AddSingleton<IEmailService, AzureCommunicationEmailService>();
    builder.Services.AddNotificationService(builder.Configuration);
    
    // Register InviteService
    var inviteSigningKey = builder.Configuration.GetValue<string>("Invite:SigningKey")
        ?? Environment.GetEnvironmentVariable("INVITE_SIGNING_KEY")
        ?? "default-signing-key-32-chars-min";
    builder.Services.AddSingleton<IInviteService>(sp =>
    {
        var dbContext = sp.GetRequiredService<MongoDbContext>();
        return new InviteService(dbContext.Invites, inviteSigningKey);
    });

    // Register repositories as scoped for better thread safety
    builder.Services.AddScoped<IRepository<User>, UserRepository>();
    builder.Services.AddScoped<IRepository<Conversation>, ConversationRepository>();
    builder.Services.AddScoped<IRepository<Invite>, InviteRepository>();
    builder.Services.AddScoped<IMessageRepository, MessageRepository>();
    
    // Register ArchiveService as scoped for thread-safe MongoDB access
    builder.Services.AddScoped<IArchiveService>(sp =>
    {
        var dbContext = sp.GetRequiredService<MongoDbContext>();
        return new ArchiveService(dbContext.Conversations, dbContext.Messages, dbContext.Users);
    });

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

    // Add authentication and authorization
    // JWT validation configured above with RSA256 signing and supports SignalR query string tokens
    app.UseAuthentication();
    app.UseAuthorization();

    // Map controllers
    app.MapControllers();

    // Map SignalR hub
    app.MapHub<ChatHub>("/hubs/chat");

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
