using Ambev.DeveloperEvaluation.Application.Sales.CancelSale;
using Ambev.DeveloperEvaluation.Application.Sales.CancelSaleItem;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;
using Ambev.DeveloperEvaluation.Common.Security;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Ambev.DeveloperEvaluation.IoC.ModuleInitializers;

public class ApplicationModuleInitializer : IModuleInitializer
{
    public void Initialize(WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();

        // Register validators so MediatR pipeline (ValidationBehavior) resolves them
        builder.Services.AddScoped<IValidator<CreateSaleCommand>, CreateSaleValidator>();
        builder.Services.AddScoped<IValidator<UpdateSaleCommand>, UpdateSaleValidator>();
        builder.Services.AddScoped<IValidator<CancelSaleCommand>, CancelSaleValidator>();
        builder.Services.AddScoped<IValidator<CancelSaleItemCommand>, CancelSaleItemValidator>();
    }
}