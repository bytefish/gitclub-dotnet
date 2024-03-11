using Serilog.Filters;
using Serilog.Sinks.SystemConsole.Themes;
using Serilog;
using OpenFga.Sdk.Client;
using GitClub.Services;
using GitClub.Database;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using GitClub.Infrastructure.Constants;
using System.Security.Claims;
using System.Threading.RateLimiting;
using GitClub.Infrastructure.Errors.Translators;
using GitClub.Infrastructure.Errors;

// We will log to %LocalAppData%/RebacExperiments to store the Logs, so it doesn't need to be configured 
// to a different path, when you run it on your machine.
string logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RebacExperiments");

// We are writing with RollingFileAppender using a daily rotation, and we want to have the filename as 
// as "LogRebacExperiments-{Date}.log", the "{Date}" placeholder will be replaced by Serilog itself.
string logFilePath = Path.Combine(logDirectory, "LogRebacExperiments-.log");

// Configure the Serilog Logger. This Serilog Logger will be passed 
// to the Microsoft.Extensions.Logging LoggingBuilder using the 
// LoggingBuilder#AddSerilog(...) extension.
Log.Logger = new LoggerConfiguration()
    .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware"))
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console(theme: AnsiConsoleTheme.Code)
    .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Configuration
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddEnvironmentVariables();

    // Logging
    builder.Services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));

    // Database
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        var connectionString = builder.Configuration.GetConnectionString("ApplicationDatabase");

        if (connectionString == null)
        {
            throw new InvalidOperationException("No ConnectionString named 'ApplicationDatabase' was found");
        }

        // Since version 7.0, NpgsqlDataSource is the recommended way to use Npgsql. When using NpsgqlDataSource,
        // NodaTime currently has to be configured twice - once at the EF level, and once at the underlying ADO.NET
        // level (there are plans to improve this):
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);

        // Call UseNodaTime() when building your data source:
        dataSourceBuilder.UseNodaTime();

        var dataSource = dataSourceBuilder.Build();

        // Then, when configuring EF Core with UseNpgsql(), call UseNodaTime() there as well:
        options
            .EnableSensitiveDataLogging()
            .UseNpgsql(dataSource, options => options.UseNodaTime());
    });

    // OpenFGA
    builder.Services.AddSingleton<OpenFgaClient>(sp =>
    {
        var configuration = new ClientConfiguration
        {
            ApiUrl = builder.Configuration.GetValue<string>("OpenFGA:ApiUrl")!,
            StoreId = builder.Configuration.GetValue<string>("OpenFGA:StoreId")!,
            AuthorizationModelId = builder.Configuration.GetValue<string>("OpenFGA:AuthorizationModelId")!,
        };

        return new OpenFgaClient(configuration);
    });

    builder.Services.AddScoped<AclService>();

    // CORS
    builder.Services.AddCors(options =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("AllowedOrigins")
            .Get<string[]>();

        if (allowedOrigins == null)
        {
            throw new InvalidOperationException("AllowedOrigins is missing in the appsettings.json");
        }

        options.AddPolicy("CorsPolicy", builder => builder
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
    });

    // Add Exception Handling
    builder.Services.AddSingleton<IExceptionTranslator, DefaultErrorExceptionTranslator>();
    builder.Services.AddSingleton<IExceptionTranslator, ApplicationErrorExceptionTranslator>();
    builder.Services.AddSingleton<IExceptionTranslator, InvalidModelStateExceptionTranslator>();

    builder.Services.Configure<ExceptionToApplicationErrorMapperOptions>(o =>
    {
        o.IncludeExceptionDetails = builder.Environment.IsDevelopment() || builder.Environment.IsStaging();
    });

    builder.Services.AddSingleton<ExceptionToApplicationErrorMapper>();

    // Application Services
    builder.Services.AddScoped<UserService>();
    builder.Services.AddScoped<OrganizationService>();

    // Controllers
    builder.Services.AddControllers();

    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Add Policies
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy(Policies.RequireUserRole, policy => policy.RequireRole(Roles.User));
        options.AddPolicy(Policies.RequireAdminRole, policy => policy.RequireRole(Roles.Administrator));
    });

    // Add the Rate Limiting
    builder.Services.AddRateLimiter(options =>
    {
        options.OnRejected = (context, cancellationToken) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

            return ValueTask.CompletedTask;
        };

        options.AddPolicy(Policies.PerUserRatelimit, context =>
        {
            var username = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            return RateLimitPartition.GetTokenBucketLimiter(username, key =>
            {
                return new()
                {
                    ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                    AutoReplenishment = true,
                    TokenLimit = 100,
                    TokensPerPeriod = 100,
                    QueueLimit = 100,
                };
            });
        });
    });

    var app = builder.Build();

    // Use a Controller for handling the ASP.NET Core lower-level errors.
    app.UseExceptionHandler("/error");
    app.UseStatusCodePagesWithReExecute("/error/{0}");

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // CORS
    app.UseCors("CorsPolicy");

    app.UseRateLimiter();

    app.UseHttpsRedirection();

    app.UseAuthorization();
    app.UseRateLimiter();
    app.MapControllers();

    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "An unhandeled exception occured.");
}
finally
{
    // Wait 0.5 seconds before closing and flushing, to gather the last few logs.
    await Task.Delay(TimeSpan.FromMilliseconds(500));
    await Log.CloseAndFlushAsync();
}