//using Platform.API.Extensions;
//using Platform.API.Services;
//using Platform.Application.Interfaces;
//using Platform.Infrastructure.Persistence;
//var builder = WebApplication.CreateBuilder(args);

//// Add services to the container.

//builder.Services.AddApplicationServices();
//builder.Services.AddInfrastructureServices(builder.Configuration);
//builder.Services.AddSwaggerExtension();
//builder.Services.AddControllers();
//builder.Services.AddHealthChecks();
//builder.Services.AddScoped<IAuthenticatedUserService, AuthenticatedUserService>();
//builder.Services.AddDatabaseDeveloperPageExceptionFilter();
//builder.Services.AddEndpointsApiExplorer();

//var app = builder.Build();

//// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{

//    app.UseDeveloperExceptionPage();
//    app.UseMigrationsEndPoint();

//    // Initialise and seed database
//    using (var scope = app.Services.CreateScope())
//    {
//        var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();
//        await initialiser.InitialiseAsync();
//        await initialiser.SeedAsync();
//    }
//}
//else
//{
//    app.UseExceptionHandler("/Error");
//    app.UseHsts();
//}

//app.UseHttpsRedirection();
//app.UseRouting();
//app.UseAuthentication();
//app.UseAuthorization();
//app.UseSwaggerExtension();
//app.UseErrorHandlingMiddleware();
//app.UseHealthChecks("/health");

//app.UseEndpoints(endpoints =>
//{
//    endpoints.MapControllers();
//});

using Platform.Infrastructure;

public class Program
{
    public static async Task Main(string[] args)
    {
        var logger = GetLogger();

        try
        {
            logger.LogDebug("Init main");
            var host = CreateHostBuilder(args).Build();
            await host.RunAsync();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Stopped program because of exception");
            throw;
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
    }

    private static ILogger GetLogger()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Path.GetDirectoryName(typeof(Program).Assembly.Location))
            .AddJsonFile("appsettings.json", true, true)
            .Build();

        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConfiguration(config)
                .AddConsole();
        });

        return loggerFactory.CreateLogger<Program>();
    }
}