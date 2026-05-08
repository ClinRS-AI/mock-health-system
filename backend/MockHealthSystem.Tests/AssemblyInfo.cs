using Xunit;

// Serializes all tests. Env-mutating suites are additionally tagged with [Collection("EnvironmentMutating")]
// (see EnvironmentMutatingCollectionDefinition.cs) so they stay isolated if parallelization is re-enabled.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
