
using FEB2026Project.RUSTApi.Application.Services.AuthenticationServices;
using FEB2026Project.RUSTApi.Application.Services.ErrorHandlingServices;
using FEB2026Project.RUSTApi.Application.Services.RoleServices;
using FEB2026Project.RUSTApi.Application.Services.UserServices;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace FEB2026Project.RUSTApi.Application.Extensions
{
    public static class ApplicationServicesExtensions
    {
        public static IServiceCollection AddApplicationRegistrationServices(this IServiceCollection services)
        {
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<IErrorHandlingService, ErrorHandlingService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IUserService, UserService>();

            return services;
        }
    }
}