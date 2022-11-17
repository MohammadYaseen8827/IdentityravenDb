using Platform.API.Extensions;
using Platform.API.Services;
using Platform.Application;
using Platform.Application.Interfaces;
using Platform.Infrastructure;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddApplicationServices();
        services.AddInfrastructureServices(Configuration);
        services.AddSwaggerExtension();
        services.AddControllers();
        services.AddHealthChecks();
        services.AddScoped<IAuthenticatedUserService, AuthenticatedUserService>();
        services.AddDatabaseDeveloperPageExceptionFilter();
        services.AddEndpointsApiExplorer();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseMigrationsEndPoint();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }
        
        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseSwaggerExtension();
        app.UseErrorHandlingMiddleware();
        app.UseHealthChecks("/health");

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}