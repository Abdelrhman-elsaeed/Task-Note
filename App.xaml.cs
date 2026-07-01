using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.IO;
using System.Windows;
using TaskNote.Data;
using TaskNote.Models;
using TaskNote.Services;
using TaskNote.ViewModels;

namespace TaskNote
{
    public partial class App : Application
    {
        private static IHost? _host;

        public static IHost Host => _host ?? throw new InvalidOperationException("Host not initialized.");

        public App()
        {
            var appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TaskNote");
            if (!Directory.Exists(appDataFolder))
            {
                Directory.CreateDirectory(appDataFolder);
            }

            var settingsPath = Path.Combine(appDataFolder, "appsettings.json");
            if (!File.Exists(settingsPath))
            {
                var defaultSettings = new AppSettings
                {
                    DatabasePath = Path.Combine(appDataFolder, "tasknote.db"),
                    TimerStartSoundPath = string.Empty,
                    TimerFinishSoundPath = string.Empty,
                    TimerDurationMinutes = 25
                };
                var json = System.Text.Json.JsonSerializer.Serialize(new { AppSettings = defaultSettings }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(settingsPath, json);
            }

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(Path.Combine(appDataFolder, "logs", "log-.txt"), rollingInterval: RollingInterval.Day)
                .CreateLogger();

            _host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                .UseSerilog()
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile(settingsPath, optional: false, reloadOnChange: true);
                })
                .ConfigureServices((context, services) =>
                {
                    services.Configure<AppSettings>(context.Configuration.GetSection("AppSettings"));

                    services.AddSingleton<ISettingsService, SettingsService>();
                    services.AddSingleton<IAudioService, AudioService>();
                    services.AddSingleton<ITimerService, TimerService>();
                    services.AddSingleton<IDialogService, DialogService>();
                    services.AddSingleton<CarryOverService>();
                    services.AddSingleton<ProjectService>();

                    services.AddDbContextFactory<AppDbContext>();

                    services.AddSingleton<IProjectRepository, ProjectRepository>();
                    services.AddTransient(typeof(IRepository<>), typeof(Repository<>));

                    services.AddSingleton<MainViewModel>();
                    services.AddSingleton<BoardViewModel>();
                    services.AddSingleton<TimerViewModel>();
                    services.AddSingleton<SettingsViewModel>();
                    services.AddSingleton<CalendarViewModel>();

                    services.AddSingleton<MainWindow>();
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            Log.Information("Application Starting up...");

            try
            {
                await Host.StartAsync();

                var dbContextFactory = Host.Services.GetRequiredService<Microsoft.EntityFrameworkCore.IDbContextFactory<AppDbContext>>();
                using (var context = await dbContextFactory.CreateDbContextAsync())
                {
                    try
                    {
                        await context.Database.EnsureCreatedAsync();
                        
                        try
                        {
                            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Projects ADD COLUMN TargetDate TEXT;");
                        }
                        catch
                        {
                            // Ignored if the column already exists
                        }

                        try
                        {
                            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Projects ADD COLUMN IsCarryOverProcessed INTEGER NOT NULL DEFAULT 0;");
                        }
                        catch
                        {
                            // Ignored if the column already exists
                        }

                        try
                        {
                            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Tasks ADD COLUMN TaskDate TEXT;");
                        }
                        catch
                        {
                            // Ignored if the column already exists
                        }

                        // Perform a test query to verify schema matches the new models
                        await context.Projects.AsNoTracking().FirstOrDefaultAsync();
                        await context.Folders.AsNoTracking().FirstOrDefaultAsync();
                        await context.Tasks.AsNoTracking().FirstOrDefaultAsync();
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "SQLite schema mismatch or obsolete database. Recreating database...");
                        await context.Database.EnsureDeletedAsync();
                        await context.Database.EnsureCreatedAsync();
                    }
                }

                var mainViewModel = Host.Services.GetRequiredService<MainViewModel>();
                await mainViewModel.InitializeAsync();

                var mainWindow = Host.Services.GetRequiredService<MainWindow>();
                mainWindow.DataContext = mainViewModel;
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application start failed");
                MessageBox.Show($"Application failed to start: {ex.Message}", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            Log.Information("Application exiting...");
            if (_host != null)
            {
                await _host.StopAsync();
                _host.Dispose();
            }
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }
}
