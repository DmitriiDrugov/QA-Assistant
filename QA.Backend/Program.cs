using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using QA.Backend.Data;
using QA.Backend.Data.Entities;
using QA.Backend.Models;
using QA.Backend.Options;
using QA.Backend.Services;

var builder = WebApplication.CreateBuilder(args);

// STEP 8: Bind typed options for AI, knowledge base, CORS, and future database integration.
builder.Services.Configure<AiOptions>(builder.Configuration.GetSection(AiOptions.SectionName));
builder.Services.Configure<KnowledgeBaseOptions>(builder.Configuration.GetSection(KnowledgeBaseOptions.SectionName));
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection(DatabaseOptions.SectionName));
builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection(CorsOptions.SectionName));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

var databaseOptions = builder.Configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();
var connectionString = databaseOptions.ConnectionString;
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Database:ConnectionString is required. Set it via appsettings.json or environment variable Database__ConnectionString.");
}

var mySqlServerVersion = ServerVersion.Create(new Version(8, 0, 0), ServerType.MySql);
builder.Services.AddDbContext<AppDbContext>(options => options.UseMySql(connectionString, mySqlServerVersion));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<KnowledgeBaseService>();
builder.Services.AddSingleton<SearchService>();
builder.Services.AddScoped<QaService>();
builder.Services.AddScoped<AuraChatService>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddScoped<IPasswordHasher<UserEntity>, PasswordHasher<UserEntity>>();

builder.Services.AddHttpClient<IAiService, OpenAiService>((serviceProvider, client) =>
{
    var aiOptions = serviceProvider.GetRequiredService<IOptions<AiOptions>>().Value;

    if (Uri.TryCreate(aiOptions.BaseUrl, UriKind.Absolute, out var baseUri))
    {
        client.BaseAddress = baseUri;
    }

    client.Timeout = TimeSpan.FromSeconds(Math.Clamp(aiOptions.TimeoutSeconds, 5, 300));
});
builder.Services.AddHttpClient<AuraModelService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

const string frontendCorsPolicy = "FrontendCors";
builder.Services.AddCors(options =>
{
    var corsOptions = builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new CorsOptions();

    options.AddPolicy(frontendCorsPolicy, policy =>
    {
        if (corsOptions.AllowedOrigins.Count == 0 || corsOptions.AllowedOrigins.Contains("*"))
        {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            return;
        }

        policy.WithOrigins(corsOptions.AllowedOrigins.ToArray())
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
var jwtSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = jwtSigningKey,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    try
    {
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Database migration failed during startup. The application will not be able to serve requests that touch the database.");
        throw;
    }

    var knowledgeBaseService = scope.ServiceProvider.GetRequiredService<KnowledgeBaseService>();

    try
    {
        var knowledgeStatus = await knowledgeBaseService.ReloadAsync();
        logger.LogInformation(
            "Knowledge base warm-up completed. Loaded: {IsLoaded}, Chunks: {ChunkCount}, Path: {ResolvedFilePath}",
            knowledgeStatus.IsLoaded,
            knowledgeStatus.ChunkCount,
            knowledgeStatus.ResolvedFilePath);
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Knowledge base warm-up failed during startup. The application will continue and retry on demand.");
    }
}

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(new ApiErrorResponse
        {
            Message = "An unexpected internal server error occurred."
        });
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors(frontendCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
