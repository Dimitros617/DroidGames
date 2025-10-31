using BlazorApp1.Components;
using BlazorApp1.Data;
using BlazorApp1.Models;
using BlazorApp1.Services;
using BlazorApp1.Hubs;
using Blazored.Toast;
using Microsoft.AspNetCore.ResponseCompression;

Console.WriteLine("[DEBUG] Program.cs START");

var builder = WebApplication.CreateBuilder(args);

// REMOVED - detailed SignalR logging causing too much output
// builder.Logging.AddFilter("Microsoft.AspNetCore.SignalR", LogLevel.Debug);
// builder.Logging.AddFilter("Microsoft.AspNetCore.Http.Connections", LogLevel.Debug);

Console.WriteLine("[DEBUG] Builder created");

// Add services to the container
Console.WriteLine("[DEBUG] Adding Razor components...");
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
Console.WriteLine("[DEBUG] Razor components added");

// SignalR with security best practices
builder.Services.AddSignalR(options =>
{
    // Security: Only show detailed errors in Development
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    
    // Connection timeouts to prevent zombie connections
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30); // Client must ping every 30s
    options.HandshakeTimeout = TimeSpan.FromSeconds(15); // Max handshake time
    options.KeepAliveInterval = TimeSpan.FromSeconds(15); // Server pings client
    
    // Security: Prevent flooding and DoS
    options.MaximumParallelInvocationsPerClient = 1; // One call at a time per client
    options.MaximumReceiveMessageSize = 128 * 1024; // 128 KB limit (enough for Blazor, prevents DoS)
    options.StreamBufferCapacity = 10; // Limit stream buffer
});
builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});

// Toast notifications - DISABLED for debugging
// builder.Services.AddBlazoredToast();

// Data path configuration
Console.WriteLine("[DEBUG] Configuring data path...");
var dataPath = builder.Configuration["DataPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "data");
Console.WriteLine($"[DEBUG] Data path: {dataPath}");
Directory.CreateDirectory(dataPath);
Console.WriteLine("[DEBUG] Data directory created");

// Repository registrations
Console.WriteLine("[DEBUG] Registering repositories...");
builder.Services.AddSingleton<IRepository<Team>>(sp => 
    new JsonRepository<Team>(dataPath, "teams.json"));
builder.Services.AddSingleton<IRepository<MapConfiguration>>(sp => 
    new JsonRepository<MapConfiguration>(dataPath, "maps.json"));
builder.Services.AddSingleton<IRepository<Achievement>>(sp => 
    new JsonRepository<Achievement>(dataPath, "achievements.json"));
builder.Services.AddSingleton<IRepository<TeamAchievement>>(sp => 
    new JsonRepository<TeamAchievement>(dataPath, "team-achievements.json"));
builder.Services.AddSingleton<IRepository<QuizQuestion>>(sp => 
    new JsonRepository<QuizQuestion>(dataPath, "quiz.json"));
builder.Services.AddSingleton<IRepository<QuizProgress>>(sp => 
    new JsonRepository<QuizProgress>(dataPath, "quiz-progress.json"));
builder.Services.AddSingleton<IRepository<Reminder>>(sp => 
    new JsonRepository<Reminder>(dataPath, "reminders.json"));
builder.Services.AddSingleton<IRepository<FunFact>>(sp => 
    new JsonRepository<FunFact>(dataPath, "funfacts.json"));
builder.Services.AddSingleton<IRepository<User>>(sp => 
    new JsonRepository<User>(dataPath, "users.json"));

// FinalRoundScore repository
builder.Services.AddSingleton<IRepository<FinalRoundScore>>(sp => 
    new JsonRepository<FinalRoundScore>(dataPath, "final-round-scores.json"));

// RoundOrder repository
builder.Services.AddSingleton<IRepository<RoundOrder>>(sp => 
    new JsonRepository<RoundOrder>(dataPath, "round-orders.json"));

// Settings repository - needed for Home.razor
builder.Services.AddSingleton<IRepository<CompetitionSettings>>(sp => 
    new JsonRepository<CompetitionSettings>(dataPath, "settings.json"));

// Settings as singleton (for backward compatibility)
Console.WriteLine("[DEBUG] Loading settings...");
builder.Services.AddSingleton<CompetitionSettings>(sp =>
{
    var settingsRepo = sp.GetRequiredService<IRepository<CompetitionSettings>>();
    var allSettings = settingsRepo.GetAllAsync().Result;
    Console.WriteLine($"[DEBUG] Settings loaded: {allSettings.Count} items");
    return allSettings.FirstOrDefault() ?? new CompetitionSettings();
});
Console.WriteLine("[DEBUG] Settings registration complete");

// Services - Changed to Singleton for Blazor Server compatibility
Console.WriteLine("[DEBUG] Registering services...");
builder.Services.AddSingleton<ITeamService, TeamService>();
builder.Services.AddSingleton<IScoreService, ScoreService>();
builder.Services.AddSingleton<IMapService, MapService>();
builder.Services.AddSingleton<IAchievementService, AchievementService>();
builder.Services.AddSingleton<IQuizService, QuizService>();
builder.Services.AddSingleton<IReminderService, ReminderService>();
builder.Services.AddSingleton<IFunFactService, FunFactService>();
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddSingleton<IGameStatusService, GameStatusService>(); // MUST be before TimerService
builder.Services.AddSingleton<ITimerService, TimerService>();
builder.Services.AddSingleton<IDiplomaService, DiplomaService>();
builder.Services.AddSingleton<IFinalScoreService, FinalScoreService>();
builder.Services.AddSingleton<IScoreNotificationService, ScoreNotificationService>();
builder.Services.AddSingleton<IAchievementEvaluationService, AchievementEvaluationService>();
builder.Services.AddSingleton<IScoreFinalizationService, ScoreFinalizationService>();

// Background services
Console.WriteLine("[DEBUG] Registering background services...");
builder.Services.AddHostedService<BackupService>();
builder.Services.AddHostedService<TimerBackgroundService>();
Console.WriteLine("[DEBUG] Background services registered");

// HTTP Context
builder.Services.AddHttpContextAccessor();

// User session management with proper cleanup
builder.Services.AddScoped<UserSession>();

// Circuit handler for WebSocket lifecycle monitoring
builder.Services.AddScoped<Microsoft.AspNetCore.Components.Server.Circuits.CircuitHandler, CircuitHandlerService>();

// CORS for ESP32 API
builder.Services.AddCors(options =>
{
    options.AddPolicy("ESP32Policy", policy =>
    {
        // TODO: Replace with actual ESP32 IP address before production!
        // SECURITY: AllowAnyOrigin is dangerous - only for development
        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            // Production: Specify exact ESP32 IP
            policy.WithOrigins(
                "http://192.168.1.100", // Replace with your ESP32 IP
                "http://10.0.0.100"     // Add more IPs if needed
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
        }
    });
});

// Controllers for API
builder.Services.AddControllers();

Console.WriteLine("[DEBUG] Building app...");
var app = builder.Build();
Console.WriteLine("[DEBUG] App built successfully");

// Configure global exception handler
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionHandlerPathFeature = 
            context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
        var exception = exceptionHandlerPathFeature?.Error;
        
        Console.WriteLine($"[ERROR] Unhandled exception: {exception?.GetType().Name}");
        Console.WriteLine($"[ERROR] Message: {exception?.Message}");
        Console.WriteLine($"[ERROR] StackTrace: {exception?.StackTrace}");
        
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("Internal Server Error. Check console for details.");
    });
});

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.UseResponseCompression();

app.UseCors("ESP32Policy");

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// SignalR Hubs
app.MapHub<ScoreboardHub>("/scoreboardHub");
app.MapHub<TimerHub>("/hubs/timer");
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHub<ProductionHub>("/hubs/production");

// API Controllers
app.MapControllers();

Console.WriteLine("[DEBUG] Starting app...");
app.Run();
