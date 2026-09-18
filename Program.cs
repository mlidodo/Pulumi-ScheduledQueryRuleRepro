using Pulumi;
using Pulumi.AzureNative.ApplicationInsights;
using Pulumi.AzureNative.Monitor;
using Pulumi.AzureNative.Monitor.Inputs;
using Pulumi.AzureNative.OperationalInsights;
using Pulumi.AzureNative.OperationalInsights.Inputs;
using Pulumi.AzureNative.Resources;

return await Pulumi.Deployment.RunAsync(() =>
{
    var config = new Config();
    var location = config.Get("location") ?? "westeurope";
    var existingResourceGroupId = config.Get("resourceGroupId");

    var resourceGroup = existingResourceGroupId is null
        ? new ResourceGroup("repro-rg", new ResourceGroupArgs
        {
            Location = location,
        })
        : ResourceGroup.Get("repro-rg", existingResourceGroupId, new CustomResourceOptions { Protect = true });

    var workspace = new Workspace("repro-workspace", new WorkspaceArgs
    {
        ResourceGroupName = resourceGroup.Name,
        Location = location,
        Sku = new WorkspaceSkuArgs
        {
            Name = "PerGB2018",
        },
        RetentionInDays = 30,
    });

    var appInsights = new Component("repro-appinsights", new ComponentArgs
    {
        ResourceGroupName = resourceGroup.Name,
        Location = location,
        ApplicationType = ApplicationType.Web,
        Kind = "web",
        WorkspaceResourceId = workspace.Id,
    });

    var scheduledQueryRule = new ScheduledQueryRule("repro-scheduled-query-rule", new ScheduledQueryRuleArgs
    {
        ResourceGroupName = resourceGroup.Name,
        Location = location,
        RuleName = "repro-scheduled-query-rule",
        DisplayName = "Scheduled query rule scope replacement repro",
        Description = "Change Scopes to reproduce Azure's non-updatable scope behavior.",
        Enabled = true,
        EvaluationFrequency = "PT5M",
        WindowSize = "PT5M",
        Severity = 3,
        SkipQueryValidation = true,
        Scopes =
        {
            // REPRO: swap these two
            //appInsights.Id,
            workspace.Id,
        },
        Criteria = new ScheduledQueryRuleCriteriaArgs
        {
            AllOf =
            {
                new ConditionArgs
                {
                    // REPRO: swap these two
                    //Query = "requests | summarize Count = count()",   // when rule is scoped to AppInsights
                    Query = "AppRequests | summarize Count = count()",  // when rule is scoped to Log Analytics workspace
                    Operator = "GreaterThan",
                    Threshold = 0,
                    TimeAggregation = "Count",
                },
            },
        },
    }, new CustomResourceOptions { ReplaceOnChanges = { "scopes" } });
});
