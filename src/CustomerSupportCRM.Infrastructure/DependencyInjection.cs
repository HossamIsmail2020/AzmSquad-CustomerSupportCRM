using CustomerSupportCRM.Application.Auth;
using CustomerSupportCRM.Application.Customers;
using CustomerSupportCRM.Application.Dashboard;
using CustomerSupportCRM.Application.Tickets;
using CustomerSupportCRM.Infrastructure.Auth;
using CustomerSupportCRM.Infrastructure.Identity;
using CustomerSupportCRM.Infrastructure.Persistence;
using CustomerSupportCRM.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CustomerSupportCRM.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default") ?? "Data Source=crm.db";
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<JwtTokenGenerator>();

        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ITicketService, TicketService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
