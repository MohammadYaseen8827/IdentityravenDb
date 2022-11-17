using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Platform.Application.Interfaces;
using Platform.Application.Wrappers;
using Platform.Domain.Settings;
using Platform.Infrastructure.Identity.Models;
using Platform.Infrastructure.Identity.Services;
using Platform.Infrastructure.Repositories;
using Platform.Infrastructure.Services;
using System.Text;
using Mcrio.AspNetCore.Identity.On.RavenDb;
using Mcrio.AspNetCore.Identity.On.RavenDb.Model.Role;
using Mcrio.AspNetCore.Identity.On.RavenDb.Model.User;
using Mcrio.AspNetCore.Identity.On.RavenDb.RavenDb;
using Mcrio.AspNetCore.Identity.On.RavenDb.Stores;
using Raven.Client;
using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Raven.Client.Documents.Session;

namespace Platform.Infrastructure;

public static class ConfigureServices
{ 
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        
        // Register document store
        string databaseName = configuration.GetSection("RavenDbDatabase").Get<string>();
        IDocumentStore store = new DocumentStore
        {
            Urls = configuration.GetSection("RavenDbUrls").Get<string[]>(),
            Database = databaseName,
        };
        store.Conventions.FindCollectionName = type =>
        {
            if (IdentityRavenDbConventions.TryGetCollectionName(
                    type,
                    out string? collectionName))
            {
                return collectionName;
            }

            return DocumentConventions.DefaultGetCollectionName(type);
        };
        store.Initialize();
        store.EnsureDatabaseExists(databaseName, true);

        services.AddSingleton(store);

        // Register scoped document session
        services.AddScoped(
            provider => provider.GetRequiredService<IDocumentStore>().OpenAsyncSession()
        );

        // Add identity
        services
            .AddIdentity<RavenIdentityUser, RavenIdentityRole>(
                options =>
                {
                    options.User.RequireUniqueEmail = true;
                    options.SignIn.RequireConfirmedEmail = false;
                }
            )
            .AddRavenDbStores<RavenUserStore, RavenRoleStore, RavenIdentityUser, RavenIdentityRole>(
                provider => provider.GetRequiredService<IAsyncDocumentSession>()
            )
            .AddDefaultUI()
            .AddDefaultTokenProviders();
        #region Services
        services.AddTransient<IAccountService, AccountService>();
        #endregion
        services.Configure<JWTSettings>(configuration.GetSection("JWTSettings"));
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
            .AddJwtBearer(o =>
            {
                o.RequireHttpsMetadata = false;
                o.SaveToken = false;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidIssuer = configuration["JWTSettings:Issuer"],
                    ValidAudience = configuration["JWTSettings:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWTSettings:Key"]))
                };
                o.Events = new JwtBearerEvents()
                {
                    OnAuthenticationFailed = c =>
                    {
                        c.NoResult();
                        c.Response.StatusCode = 500;
                        c.Response.ContentType = "text/plain";
                        return c.Response.WriteAsync(c.Exception.ToString());
                    },
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";
                        var result = JsonConvert.SerializeObject(new Response<string>("You are not Authorized"));
                        return context.Response.WriteAsync(result);
                    },
                    OnForbidden = context =>
                    {
                        context.Response.StatusCode = 403;
                        context.Response.ContentType = "application/json";
                        var result = JsonConvert.SerializeObject(new Response<string>("You are not authorized to access this resource"));
                        return context.Response.WriteAsync(result);
                    },
                };
            });


        #region Repositories
        services.AddScoped(typeof(IGenericRepositoryAsync<>), typeof(GenericRepositoryAsync<>));
        #endregion

        services.AddTransient<IDateTimeService, DateTimeService>();
        return services;
    }
}
