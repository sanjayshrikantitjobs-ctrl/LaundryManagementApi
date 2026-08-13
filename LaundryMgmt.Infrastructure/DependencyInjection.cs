using System.Text;
using LaundryMgmt.Application.Common.Interfaces;
using LaundryMgmt.Infrastructure.Identity;
using LaundryMgmt.Infrastructure.Interceptors;
using LaundryMgmt.Infrastructure.Persistence;
using LaundryMgmt.Infrastructure.Services;
using LaundryMgmt.Infrastructure.Services.WhatsApp;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace LaundryMgmt.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddScoped<AuditableEntitySaveChangesInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
            options.AddInterceptors(sp.GetRequiredService<AuditableEntitySaveChangesInterceptor>());
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<INotificationService, NotificationService>();

        // WhatsApp OTP sender — Strategy pattern (see IWhatsAppSender). Defaults to logging
        // only; set "WhatsApp:Provider" to "Twilio" once you have real Twilio credentials
        // (WhatsApp:Twilio:AccountSid/AuthToken/FromNumber) to actually send messages.
        services.AddHttpClient<TwilioWhatsAppSender>();
        services.AddScoped<IWhatsAppSender>(sp =>
            configuration["WhatsApp:Provider"]?.Equals("Twilio", StringComparison.OrdinalIgnoreCase) == true
                ? sp.GetRequiredService<TwilioWhatsAppSender>()
                : ActivatorUtilities.CreateInstance<LoggingWhatsAppSender>(sp));
        services.AddScoped<Application.Common.Interfaces.IOtpService, OtpService>();

        // ASP.NET Core Identity — backs Admin/StoreManager/Staff/DepartmentHead/PickupAgent/DeliveryAgent logins.
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = false; // flip on once email sending is wired up
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<Application.Common.Interfaces.IIdentityAuthService, IdentityAuthService>();
        services.AddScoped<Application.Common.Interfaces.ITokenService, TokenServiceAdapter>();
        services.AddScoped<Application.Common.Interfaces.IUserManagementService, UserManagementService>();

        // JWT auth
        var jwtSection = configuration.GetSection("Jwt");
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSection["Issuer"],
                ValidAudience = jwtSection["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"] ?? string.Empty))
            };
        });

        services.AddAuthorization();

        // Background jobs (notifications, reminders, low-stock checks, report generation)
        services.AddHangfire(cfg => cfg.UseSqlServerStorage(connectionString));
        services.AddHangfireServer();

        return services;
    }
}
