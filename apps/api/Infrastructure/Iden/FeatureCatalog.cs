namespace Documate.Api.Infrastructure.Iden;

using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class FeatureCatalogDocument
{
    public List<FeatureCatalogModule> Modules { get; set; } = new();
    public List<FeatureCatalogFeature> Features { get; set; } = new();

    public static FeatureCatalogDocument LoadEmbeddedOrFile()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Iden", "feature-catalog.json");
        if (!File.Exists(path))
        {
            // Dev: content root relative
            path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Iden", "feature-catalog.json"));
        }

        if (!File.Exists(path))
        {
            return BuildDefault();
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<FeatureCatalogDocument>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        }) ?? BuildDefault();
    }

    public static FeatureCatalogDocument BuildDefault() => FeatureCatalogSeed.Create();
}

public sealed class FeatureCatalogModule
{
    public string ModuleKey { get; set; } = "";
    public string DisplayName { get; set; } = "";
}

public sealed class FeatureCatalogFeature
{
    public string ModuleKey { get; set; } = "";
    public string FeatureKey { get; set; } = "";
    public string Name { get; set; } = "";
    public string CapabilityType { get; set; } = "action";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Compile-time FeatureKey constants (mirror Iden catalog).</summary>
public static class FeatureKeys
{
    public const string CustomerHomeView = "documate.customer.home.view";
    public const string CustomerFilesList = "documate.customer.files.list";
    public const string CustomerFilesView = "documate.customer.files.view";
    public const string CustomerFilesUpload = "documate.customer.files.upload";
    public const string CustomerFilesUpdate = "documate.customer.files.update";
    public const string CustomerFilesDelete = "documate.customer.files.delete";
    public const string CustomerDocumentsView = "documate.customer.documents.view";
    public const string CustomerDocumentsEdit = "documate.customer.documents.edit";
    public const string CustomerDocumentsPublish = "documate.customer.documents.publish";
    public const string CustomerSearchView = "documate.customer.search.view";
    public const string CustomerAgentsList = "documate.customer.agents.list";
    public const string CustomerAgentsView = "documate.customer.agents.view";
    public const string CustomerAgentsCreate = "documate.customer.agents.create";
    public const string CustomerAgentsUpdate = "documate.customer.agents.update";
    public const string CustomerAgentsDelete = "documate.customer.agents.delete";
    public const string CustomerAgentsTemplatesView = "documate.customer.agents.templates.view";
    public const string CustomerChannelsList = "documate.customer.channels.list";
    public const string CustomerChannelsView = "documate.customer.channels.view";
    public const string CustomerChannelsCreate = "documate.customer.channels.create";
    public const string CustomerChannelsUpdate = "documate.customer.channels.update";
    public const string CustomerChannelsDelete = "documate.customer.channels.delete";
    public const string CustomerIntakeView = "documate.customer.intake.view";
    public const string CustomerIntakeManage = "documate.customer.intake.manage";
    public const string CustomerUsersList = "documate.customer.users.list";
    public const string CustomerUsersInvite = "documate.customer.users.invite";
    public const string CustomerUsersPermissionsManage = "documate.customer.users.permissions.manage";
    public const string CustomerUsersDeactivate = "documate.customer.users.deactivate";
    public const string CustomerBusinessProfileView = "documate.customer.business.profile.view";
    public const string CustomerBusinessProfileUpdate = "documate.customer.business.profile.update";
    public const string CustomerBusinessSwitch = "documate.customer.business.switch";
    public const string CustomerApiKeysList = "documate.customer.integrations.apikeys.list";
    public const string CustomerApiKeysManage = "documate.customer.integrations.apikeys.manage";
    public const string CustomerBillingView = "documate.customer.billing.overview.view";
    public const string CustomerSettingsView = "documate.customer.settings.view";
    public const string CustomerAuditsView = "documate.customer.audits.view";
    public const string CustomerDocsView = "documate.customer.docs.view";
}

public static class FeatureCatalogSeed
{
    public static FeatureCatalogDocument Create()
    {
        var doc = new FeatureCatalogDocument
        {
            Modules =
            [
                new() { ModuleKey = "customer.agents_and_channels", DisplayName = "Agents & Channels" },
                new() { ModuleKey = "customer.files_and_documents", DisplayName = "Files & Documents" },
                new() { ModuleKey = "customer.system", DisplayName = "System" },
                new() { ModuleKey = "admin.tenancy", DisplayName = "Tenancy" },
                new() { ModuleKey = "admin.platform", DisplayName = "Platform" },
                new() { ModuleKey = "admin.operations", DisplayName = "Operations" },
            ],
        };

        void Add(string module, string key, string name, string type, int sort)
            => doc.Features.Add(new FeatureCatalogFeature
            {
                ModuleKey = module,
                FeatureKey = key,
                Name = name,
                CapabilityType = type,
                SortOrder = sort,
                IsActive = true,
            });

        var m = "customer.agents_and_channels";
        Add(m, FeatureKeys.CustomerAgentsList, "Agents list", "page", 10);
        Add(m, FeatureKeys.CustomerAgentsView, "View agent", "action", 20);
        Add(m, FeatureKeys.CustomerAgentsCreate, "Create agent", "action", 30);
        Add(m, FeatureKeys.CustomerAgentsUpdate, "Update agent", "action", 40);
        Add(m, FeatureKeys.CustomerAgentsDelete, "Delete agent", "sensitive", 50);
        Add(m, FeatureKeys.CustomerAgentsTemplatesView, "Agent templates", "page", 60);
        Add(m, FeatureKeys.CustomerChannelsList, "Channels list", "page", 70);
        Add(m, FeatureKeys.CustomerChannelsView, "View channel", "action", 80);
        Add(m, FeatureKeys.CustomerChannelsCreate, "Create channel", "action", 90);
        Add(m, FeatureKeys.CustomerChannelsUpdate, "Update channel", "action", 100);
        Add(m, FeatureKeys.CustomerChannelsDelete, "Delete channel", "sensitive", 110);
        Add(m, FeatureKeys.CustomerIntakeView, "Intake view", "page", 120);
        Add(m, FeatureKeys.CustomerIntakeManage, "Intake manage", "action", 130);

        m = "customer.files_and_documents";
        Add(m, FeatureKeys.CustomerHomeView, "Home", "page", 10);
        Add(m, FeatureKeys.CustomerFilesList, "Files list", "page", 20);
        Add(m, FeatureKeys.CustomerFilesView, "View file", "action", 30);
        Add(m, FeatureKeys.CustomerFilesUpload, "Upload file", "action", 40);
        Add(m, FeatureKeys.CustomerFilesUpdate, "Update file", "action", 50);
        Add(m, FeatureKeys.CustomerFilesDelete, "Delete file", "sensitive", 60);
        Add(m, FeatureKeys.CustomerDocumentsView, "View document", "action", 70);
        Add(m, FeatureKeys.CustomerDocumentsEdit, "Edit document", "action", 80);
        Add(m, FeatureKeys.CustomerDocumentsPublish, "Publish document", "sensitive", 90);
        Add(m, FeatureKeys.CustomerSearchView, "Search", "page", 100);

        m = "customer.system";
        Add(m, FeatureKeys.CustomerUsersList, "Users list", "page", 10);
        Add(m, FeatureKeys.CustomerUsersInvite, "Invite user", "action", 20);
        Add(m, FeatureKeys.CustomerUsersPermissionsManage, "Manage user permissions", "sensitive", 30);
        Add(m, FeatureKeys.CustomerUsersDeactivate, "Deactivate user", "sensitive", 40);
        Add(m, FeatureKeys.CustomerBusinessProfileView, "Business profile", "page", 50);
        Add(m, FeatureKeys.CustomerBusinessProfileUpdate, "Update business profile", "action", 60);
        Add(m, FeatureKeys.CustomerBusinessSwitch, "Switch business", "action", 70);
        Add(m, FeatureKeys.CustomerApiKeysList, "API keys list", "page", 80);
        Add(m, FeatureKeys.CustomerApiKeysManage, "Manage API keys", "sensitive", 90);
        Add(m, FeatureKeys.CustomerBillingView, "Billing overview", "page", 100);
        Add(m, FeatureKeys.CustomerSettingsView, "Settings", "page", 110);
        Add(m, FeatureKeys.CustomerAuditsView, "Audits", "page", 120);
        Add(m, FeatureKeys.CustomerDocsView, "Docs", "page", 130);

        m = "admin.tenancy";
        Add(m, "documate.admin.tenancy.tenants.list", "Tenants list", "page", 10);
        Add(m, "documate.admin.tenancy.tenants.manage", "Manage tenants", "sensitive", 20);
        Add(m, "documate.admin.tenancy.businesses.list", "Businesses list", "page", 30);
        Add(m, "documate.admin.tenancy.businesses.manage", "Manage businesses", "sensitive", 40);

        m = "admin.platform";
        Add(m, "documate.admin.platform.templates.manage", "Manage templates", "action", 10);
        Add(m, "documate.admin.platform.agents.view", "View agents", "page", 20);
        Add(m, "documate.admin.platform.catalog.manage", "Manage catalog", "action", 30);
        Add(m, "documate.admin.platform.events.manage", "Manage events", "action", 40);

        m = "admin.operations";
        Add(m, "documate.admin.operations.dashboard.view", "Dashboard", "page", 10);
        Add(m, "documate.admin.operations.ops.view", "Ops", "page", 20);
        Add(m, "documate.admin.operations.monitoring.view", "Monitoring", "page", 30);
        Add(m, "documate.admin.operations.support.view", "Support", "page", 40);

        return doc;
    }
}
