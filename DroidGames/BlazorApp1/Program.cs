using BlazorApp1.Components;
using BlazorApp1.Data;
using BlazorApp1.Models;
using BlazorApp1.Services;
using BlazorApp1.Hubs;
using Blazored.Toast;
using Microsoft.AspNetCore.ResponseCompression;

Console.WriteLine("[DEBUG] Program.cs START");

var builder = WebApplication.CreateBuilder(args);
Console.WriteLine("[DEBUG] Builder created");

// Add services to the container
Console.WriteLine("[DEBUG] Adding Razor components...");
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
Console.WriteLine("[DEBUG] Razor components added");

// SignalR
builder.Services.AddSignalR();
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
builder.Services.AddSingleton<IRepository<QuizQuestion>>(sp => 
    new JsonRepository<QuizQuestion>(dataPath, "quiz.json"));
builder.Services.AddSingleton<IRepository<Reminder>>(sp => 
    new JsonRepository<Reminder>(dataPath, "reminders.json"));
builder.Services.AddSingleton<IRepository<FunFact>>(sp => 
    new JsonRepository<FunFact>(dataPath, "funfacts.json"));
builder.Services.AddSingleton<IRepository<User>>(sp => 
    new JsonRepository<User>(dataPath, "users.json"));

// Settings as singleton
Console.WriteLine("[DEBUG] Loading settings...");
builder.Services.AddSingleton<CompetitionSettings>(sp =>
{
    var settingsPath = Path.Combine(dataPath, "settings.json");
    Console.WriteLine($"[DEBUG] Settings path: {settingsPath}");
    if (File.Exists(settingsPath))
    {
        Console.WriteLine("[DEBUG] Settings file exists, loading...");
        var json = File.ReadAllText(settingsPath);
        var settings = System.Text.Json.JsonSerializer.Deserialize<CompetitionSettings>(json) 
            ?? new CompetitionSettings();
        Console.WriteLine("[DEBUG] Settings loaded successfully");
        return settings;
    }
    Console.WriteLine("[DEBUG] Settings file not found, using defaults");
    return new CompetitionSettings();
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
builder.Services.AddSingleton<ITimerService, TimerService>();

// Background services
Console.WriteLine("[DEBUG] Registering background services...");
builder.Services.AddHostedService<BackupService>();
builder.Services.AddHostedService<TimerBackgroundService>();
Console.WriteLine("[DEBUG] Background services registered");

// HTTP Context
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<UserSession>();

// CORS for ESP32 API
builder.Services.AddCors(options =>
{
    options.AddPolicy("ESP32Policy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Controllers for API
builder.Services.AddControllers();

Console.WriteLine("[DEBUG] Building app...");
var app = builder.Build();
Console.WriteLine("[DEBUG] App built successfully");

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
app.MapHub<ScoreboardHub>("/hubs/scoreboard");
app.MapHub<TimerHub>("/hubs/timer");
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHub<ProductionHub>("/hubs/production");

// API Controllers
app.MapControllers();

Console.WriteLine("[DEBUG] Starting app...");
app.Run();
