
using FEB2026Project.RUSTApi.Appilcation.Services.AuthenticationServices;
using FEB2026Project.RUSTApi.Appilcation.Services.ErrorHandlingServices;
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

            return services;
        }
    }
}