using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.OpenApi.Models;
using SmeBackend.Controllers;
using Swashbuckle.AspNetCore.Swagger;

namespace SmeBackend.Tests;

public class SwaggerGenerationTests
{
    [Fact]
    public void SwaggerIncludesInventoryAndMultipartUploads()
    {
        var builder = WebApplication.CreateBuilder();
        var services = builder.Services;
        services.AddLogging();
        services.AddControllers().AddApplicationPart(typeof(InventoryController).Assembly);
        services.AddSwaggerGen(options => options.SwaggerDoc("v1", new OpenApiInfo { Title = "Unify API", Version = "v1" }));
        using var app = builder.Build();
        var provider = app.Services;
        var document = provider.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");
        Assert.Contains("/api/inventory", document.Paths.Keys);
        Assert.Contains("/api/inventory/physical-count-approvals/{countId}/review", document.Paths.Keys);
        var upload = document.Paths["/api/media/upload"].Operations[OperationType.Post];
        Assert.Contains("multipart/form-data", upload.RequestBody.Content.Keys);
        Assert.Contains("/api/inventory/physical-count-audits/{countId}/photos", document.Paths.Keys);
    }
}
