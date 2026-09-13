using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WinTune.Core.Orchestrator;
using WinTune.Sdk;
using Xunit;

namespace WinTune.Core.Tests;

public sealed class OrchestratorTests
{
    private static IOptimizerModule MakeModule(string id, IReadOnlyList<Finding> findings)
    {
        var mock = new Mock<IOptimizerModule>();
        mock.Setup(m => m.Metadata).Returns(new ModuleMetadata(
            id, id, string.Empty, ModuleCategory.Cleanup,
            new Version(1, 0, 0), new Version(0, 1, 0)));
        mock.Setup(m => m.RequestedCapabilities).Returns([]);
        mock.Setup(m => m.ScanAsync(It.IsAny<ScanContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(findings);
        return mock.Object;
    }

    [Fact]
    public async Task ScanAllAsync_AggregatesFindings_FromAllModules()
    {
        // Arrange
        var f1 = Finding.Create("mod1", "Issue A", "desc", FindingSeverity.Low, 100);
        var f2 = Finding.Create("mod2", "Issue B", "desc", FindingSeverity.High, 200);
        var modules = new[]
        {
            MakeModule("mod1", [f1]),
            MakeModule("mod2", [f2]),
        };
        var orchestrator = new ModuleOrchestrator(modules, NullLogger<ModuleOrchestrator>.Instance);

        // Act
        var result = await orchestrator.ScanAllAsync(new ScanContext());

        // Assert
        result.TotalFindings.Should().Be(2);
        result.TotalBytesSaved.Should().Be(300);
        result.Results.Should().HaveCount(2);
    }

    [Fact]
    public async Task ScanAllAsync_ContinuesOnModuleException()
    {
        // Arrange
        var failMock = new Mock<IOptimizerModule>();
        failMock.Setup(m => m.Metadata).Returns(new ModuleMetadata(
            "bad", "Bad", string.Empty, ModuleCategory.Cleanup,
            new Version(1, 0, 0), new Version(0, 1, 0)));
        failMock.Setup(m => m.RequestedCapabilities).Returns([]);
        failMock.Setup(m => m.ScanAsync(It.IsAny<ScanContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var good = MakeModule("good", [Finding.Create("good", "OK", "ok")]);
        var orchestrator = new ModuleOrchestrator(
            [failMock.Object, good], NullLogger<ModuleOrchestrator>.Instance);

        // Act
        var result = await orchestrator.ScanAllAsync(new ScanContext());

        // Assert
        result.Results.Should().HaveCount(2);
        result.Results.First(r => r.ModuleId == "bad").Error.Should().NotBeNull();
        result.Results.First(r => r.ModuleId == "good").Findings.Should().HaveCount(1);
    }

    [Fact]
    public async Task ScanAllAsync_ReportProgress_PerModule()
    {
        // Arrange
        var modules = Enumerable.Range(1, 3)
            .Select(i => MakeModule($"mod{i}", [Finding.Create($"mod{i}", "f", "d")]))
            .ToList();
        var orchestrator = new ModuleOrchestrator(modules, NullLogger<ModuleOrchestrator>.Instance);
        var progressReports = new List<ModuleScanProgress>();

        // Act
        await orchestrator.ScanAllAsync(
            new ScanContext(),
            new Progress<ModuleScanProgress>(p => progressReports.Add(p)));

        // Assert
        progressReports.Should().HaveCount(3);
        progressReports.Last().CompletedModules.Should().Be(3);
    }

    [Fact]
    public async Task ApplyModuleAsync_ThrowsForUnknownModule()
    {
        // Arrange
        var orchestrator = new ModuleOrchestrator([], NullLogger<ModuleOrchestrator>.Instance);

        // Act & Assert
        await orchestrator.Invoking(o => o.ApplyModuleAsync("unknown", ChangeSet.Empty("unknown")))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void Modules_ReturnsAllRegistered()
    {
        // Arrange
        var m1 = MakeModule("a", []);
        var m2 = MakeModule("b", []);
        var orchestrator = new ModuleOrchestrator([m1, m2], NullLogger<ModuleOrchestrator>.Instance);

        // Assert
        orchestrator.Modules.Should().HaveCount(2);
    }
}
