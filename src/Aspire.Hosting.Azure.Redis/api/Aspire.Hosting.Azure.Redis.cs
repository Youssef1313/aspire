//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
namespace Aspire.Hosting
{
    public static partial class AzureRedisExtensions
    {
        public static ApplicationModel.IResourceBuilder<Azure.AzureRedisCacheResource> AddAzureRedis(this IDistributedApplicationBuilder builder, string name) { throw null; }

        [System.Obsolete("This method is obsolete and will be removed in a future version. Use AddAzureRedis instead to add an Azure Cache for Redis resource.")]
        public static ApplicationModel.IResourceBuilder<ApplicationModel.RedisResource> AsAzureRedis(this ApplicationModel.IResourceBuilder<ApplicationModel.RedisResource> builder) { throw null; }

        [System.Obsolete("This method is obsolete and will be removed in a future version. Use AddAzureRedis instead to add an Azure Cache for Redis resource.")]
        public static ApplicationModel.IResourceBuilder<ApplicationModel.RedisResource> PublishAsAzureRedis(this ApplicationModel.IResourceBuilder<ApplicationModel.RedisResource> builder) { throw null; }

        public static ApplicationModel.IResourceBuilder<Azure.AzureRedisCacheResource> RunAsContainer(this ApplicationModel.IResourceBuilder<Azure.AzureRedisCacheResource> builder, System.Action<ApplicationModel.IResourceBuilder<ApplicationModel.RedisResource>>? configureContainer = null) { throw null; }

        public static ApplicationModel.IResourceBuilder<Azure.AzureRedisCacheResource> WithAccessKeyAuthentication(this ApplicationModel.IResourceBuilder<Azure.AzureRedisCacheResource> builder, ApplicationModel.IResourceBuilder<Azure.IKeyVaultResource> keyVaultBuilder) { throw null; }

        public static ApplicationModel.IResourceBuilder<Azure.AzureRedisCacheResource> WithAccessKeyAuthentication(this ApplicationModel.IResourceBuilder<Azure.AzureRedisCacheResource> builder) { throw null; }
    }
}

namespace Aspire.Hosting.Azure
{
    public partial class AzureRedisCacheResource : AzureProvisioningResource, ApplicationModel.IResourceWithConnectionString, ApplicationModel.IResource, ApplicationModel.IManifestExpressionProvider, ApplicationModel.IValueProvider, ApplicationModel.IValueWithReferences
    {
        public AzureRedisCacheResource(string name, System.Action<AzureResourceInfrastructure> configureInfrastructure) : base(default!, default!) { }

        public override ApplicationModel.ResourceAnnotationCollection Annotations { get { throw null; } }

        public ApplicationModel.ReferenceExpression ConnectionStringExpression { get { throw null; } }

        [System.Diagnostics.CodeAnalysis.MemberNotNullWhen(true, "ConnectionStringSecretOutput")]
        public bool UseAccessKeyAuthentication { get { throw null; } }

        public override global::Azure.Provisioning.Primitives.ProvisionableResource AddAsExistingResource(AzureResourceInfrastructure infra) { throw null; }

        public override void AddRoleAssignments(IAddRoleAssignmentsContext roleAssignmentContext) { }
    }

    [System.Obsolete("This class is obsolete and will be removed in a future version. Use AddAzureRedis instead to add an Azure Cache for Redis resource.")]
    public partial class AzureRedisResource : AzureProvisioningResource, ApplicationModel.IResourceWithConnectionString, ApplicationModel.IResource, ApplicationModel.IManifestExpressionProvider, ApplicationModel.IValueProvider, ApplicationModel.IValueWithReferences
    {
        public AzureRedisResource(ApplicationModel.RedisResource innerResource, System.Action<AzureResourceInfrastructure> configureInfrastructure) : base(default!, default!) { }

        public override ApplicationModel.ResourceAnnotationCollection Annotations { get { throw null; } }

        public BicepSecretOutputReference ConnectionString { get { throw null; } }

        public ApplicationModel.ReferenceExpression ConnectionStringExpression { get { throw null; } }

        public override string Name { get { throw null; } }
    }
}