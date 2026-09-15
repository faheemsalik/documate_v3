namespace Documate.Api.Infrastructure.Storage;

using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Documate.Api.Infrastructure.Options;
using Microsoft.Extensions.Options;

public static class ObjectStorageServiceCollectionExtensions
{
    public static IServiceCollection AddDocumateObjectStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));

        var provider = configuration.GetSection(StorageOptions.SectionName).GetValue<string>("Provider") ?? "local";
        if (string.Equals(provider, "s3", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IAmazonS3>(sp =>
            {
                var opts = sp.GetRequiredService<IOptions<StorageOptions>>().Value;
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

                // Default credential chain (IAM role / env / profile) — preferred in AWS.
                return new AmazonS3Client(config);
            });
            services.AddSingleton<IObjectStorage, S3ObjectStorage>();
        }
        else
        {
            services.AddSingleton<IObjectStorage, LocalObjectStorage>();
        }

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
