using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TranscriptExtractor.Core;

namespace TranscriptExtractor.Tests.Api;

public sealed class TranscriptApiFactory : WebApplicationFactory<Program>
{
    private readonly InMemoryDatabaseRoot _databaseRoot = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TranscriptExtractorDbContext>();
            services.RemoveAll<DbContextOptions<TranscriptExtractorDbContext>>();

            var optionConfigurations = services
                .Where(descriptor =>
                    descriptor.ServiceType.IsGenericType &&
                    descriptor.ServiceType.GetGenericTypeDefinition().FullName == "Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration`1" &&
                    descriptor.ServiceType.GenericTypeArguments[0] == typeof(TranscriptExtractorDbContext))
                .ToList();

            foreach (var descriptor in optionConfigurations)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<TranscriptExtractorDbContext>(options =>
                options.UseInMemoryDatabase("TranscriptExtractorTests", _databaseRoot));
        });
    }
}
