using LAZConverter.Contracts;
using LAZConverter.Models;
using LAZConverter.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LAZConverter;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("LAZ to Point Cloud Converter");
        Console.WriteLine("============================");

        try
        {
            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Setup dependency injection
            var services = new ServiceCollection();
            ConfigureServices(services, configuration);

            using var serviceProvider = services.BuildServiceProvider();

            // Run the application
            var app = serviceProvider.GetRequiredService<Application>();
            await app.RunAsync(args);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            Environment.Exit(1);
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Configure logging
        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddConsole();
        });

        // Register configuration
        var appSettings = configuration.GetSection("AppSettings").Get<AppSettings>() ?? new AppSettings();
        services.AddSingleton(appSettings);

        // Register services
        services.AddScoped<IDirectoryService, DirectoryService>();
        services.AddScoped<IFileProcessorService, FileProcessorService>();
        services.AddScoped<Application>();
    }
}

