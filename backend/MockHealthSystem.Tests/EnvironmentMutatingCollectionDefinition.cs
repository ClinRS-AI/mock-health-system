using Xunit;

namespace MockHealthSystem.Tests;

/// <summary>
/// Groups tests that mutate process environment variables. If test parallelization is enabled for the assembly,
/// this collection keeps those tests from running concurrently with each other.
/// <see cref="AssemblyInfo"/> currently disables parallelization for all tests; the collection documents intent for a safer default if that changes.
/// </summary>
[CollectionDefinition("EnvironmentMutating", DisableParallelization = true)]
public sealed class EnvironmentMutatingCollectionDefinition
{
}
