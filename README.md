# ScheduledQueryRule scope replacement repro

Pulumi .NET repro for `azure-native:monitor:ScheduledQueryRule` scope changes.

`Program.cs` creates one resource group, one Log Analytics workspace, one Application Insights component, and one scheduled query rule. The rule initially scopes to App Insights:

```csharp
appInsights.Id,
// workspace.Id,
```

For the second deployment, comment `appInsights.Id` and uncomment `workspace.Id` (and also rule query due to change of table names). This changes only the rule scope and should expose the provider behavior.


By default, the stack creates its resource group. To reuse an existing resource group, set its full ARM resource ID in `Pulumi.dev.yaml`:

```yaml
config:
  scheduled-query-rule-repro:existingResourceGroupId: /subscriptions/<subscription-id>/resourceGroups/<resource-group-name>
```

## Test

Create stack:

```
pulumi login --local
pulumi stack init dev
pulumi up
```

After the first `pulumi up`, swap in stack code:
1. the two scope lines
```csharp
appInsights.Id,
//workspace.Id,
```

2. the two query lines (because table names are different depending on rule's scope)
```csharp
Query = "requests | summarize Count = count()",		  // when rule is scoped to AppInsights
//Query = "AppRequests | summarize Count = count()",  // when rule is scoped to Log Analytics workspace
```

then run:

```
pulumi up
```

Expected current behavior: Pulumi plans an in-place update, then Azure rejects it with `BadRequest: Scope can not be updated`. Expected fix: replacing the rule instead of updating it when `scopes` changes.

Clean up after testing:

```
pulumi destroy
pulumi stack rm dev
```
