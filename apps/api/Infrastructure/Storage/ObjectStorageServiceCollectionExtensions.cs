namespace Documate.Api.Infrastructure.Storage;

using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Documate.Api.Infrastructure.Options;
using Documate.Api.Infrastructure.Settings;
using Microsoft.Extensions.Options;

public static class ObjectStorageServiceCollectionExtensions
{
    public static IServiceCollection AddDocumateObjectStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));

        services.AddSingleton<IAmazonS3>(sp =>
        {
            var opts = sp.GetRequiredService<IOptionsMonitor<StorageOptions>>().CurrentValue;
            var aws = sp.GetRequiredService<IOptions<AwsOptions>>().Value;
            var region = RegionEndpoint.GetBySystemName(
                string.IsNullOrWhiteSpace(opts.Region) ? "us-west-2" : opts.Region);

            AmazonS3Config config = new() { RegionEndpoint = region };
            if (!string.IsNullOrWhiteSpace(opts.ServiceUrl))
            {
                config.ServiceURL = opts.ServiceUrl;
                config.ForcePathStyle = true;
            }

            var credentials = ResolveCredentials(opts, aws);
            if (credentials is not null)
            {
                return new AmazonS3Client(credentials, config);
            }

            return new AmazonS3Client(config);
        });

        services.AddSingleton<LocalObjectStorage>();
        services.AddSingleton<S3ObjectStorage>();
        services.AddSingleton<IObjectStorage>(sp =>
        {
            var opts = sp.GetRequiredService<IOptionsMonitor<StorageOptions>>().CurrentValue;
            var provider = opts.Provider;
            // Prefer DB-backed value if cache already loaded (same as PostConfigure).
            try
            {
                var system = sp.GetService<ISystemSettings>();
                if (system is not null)
                {
                    provider = system.GetOrDefault(SystemSettingKeys.StorageProvider, provider);
                }
            }
            catch
            {
                // Seeder not ready — fall back to bound options.
            }

            return string.Equals(provider, "s3", StringComparison.OrdinalIgnoreCase)
                ? sp.GetRequiredService<S3ObjectStorage>()
                : sp.GetRequiredService<LocalObjectStorage>();
        });

        return services;
    }

    private static AWSCredentials? ResolveCredentials(StorageOptions opts, AwsOptions aws)
    {
        if (!string.IsNullOrWhiteSpace(opts.AccessKey) && !string.IsNullOrWhiteSpace(opts.SecretKey))
        {
            return new BasicAWSCredentials(opts.AccessKey, opts.SecretKey);
        }

        return aws.TryCreateCredentials();
    }
}
