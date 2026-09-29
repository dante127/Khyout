using System.Reflection;
using FluentValidation;
using Khyout.Application.Common.Cqrs;
using Khyout.Application.Common.Security;
using Microsoft.Extensions.DependencyInjection;

namespace Khyout.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetAssembly(typeof(DependencyInjection))!;

        services.AddValidatorsFromAssembly(assembly);
        services.AddScoped<OtpVerifier>();
        services.AddScoped<ISender, Sender>();

        foreach (var type in assembly.GetTypes().Where(t => t is { IsAbstract: false, IsInterface: false }))
        {
            foreach (var iface in type.GetInterfaces().Where(i => i.IsGenericType &&
                         (i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>) ||
                          i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>))))
            {
                services.AddScoped(iface, type);
            }
        }

        return services;
    }
}
