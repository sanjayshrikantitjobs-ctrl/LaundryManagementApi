using System.Reflection;
using LaundryMgmt.Application.Common.Behaviors;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace LaundryMgmt.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddAutoMapper(cfg => { }, assembly);

        return services;
    }
}
