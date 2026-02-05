using Xunit;

// Disable parallel test execution since tests share static Services state
[assembly: CollectionBehavior(DisableTestParallelization = true)]
