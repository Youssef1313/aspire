//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
namespace Aspire.Azure.Npgsql
{
    public sealed partial class AzureNpgsqlSettings : Aspire.Npgsql.NpgsqlSettings
    {
        public global::Azure.Core.TokenCredential? Credential { get { throw null; } set { } }
    }
}

namespace Microsoft.Extensions.Hosting
{
    public static partial class AspireAzureNpgsqlExtensions
    {
        public static void AddAzureNpgsqlDataSource(this IHostApplicationBuilder builder, string connectionName, System.Action<Aspire.Azure.Npgsql.AzureNpgsqlSettings>? configureSettings = null, System.Action<Npgsql.NpgsqlDataSourceBuilder>? configureDataSourceBuilder = null) { }

        public static void AddKeyedAzureNpgsqlDataSource(this IHostApplicationBuilder builder, string name, System.Action<Aspire.Azure.Npgsql.AzureNpgsqlSettings>? configureSettings = null, System.Action<Npgsql.NpgsqlDataSourceBuilder>? configureDataSourceBuilder = null) { }
    }
}