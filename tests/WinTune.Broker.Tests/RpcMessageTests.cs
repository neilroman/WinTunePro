using FluentAssertions;
using System.Text.Json;
using WinTune.Broker.Contracts;
using Xunit;

namespace WinTune.Broker.Tests;

public sealed class RpcMessageTests
{
    [Fact]
    public void RpcRequest_Create_GeneratesUniqueIds()
    {
        var r1 = RpcRequest.Create(BrokerMethods.Ping);
        var r2 = RpcRequest.Create(BrokerMethods.Ping);
        r1.Id.Should().NotBe(r2.Id);
    }

    [Fact]
    public void RpcRequest_Create_SetsMethod()
    {
        var req = RpcRequest.Create(BrokerMethods.Ping);
        req.Method.Should().Be("ping");
        req.PayloadJson.Should().BeNull();
    }

    [Fact]
    public void RpcRequest_Create_WithPayload_SerializesJson()
    {
        var paths = new[] { @"C:\tmp\file.txt" };
        var req = RpcRequest.Create(BrokerMethods.DeleteFiles, paths);
        req.PayloadJson.Should().NotBeNullOrEmpty();
        req.PayloadJson.Should().Contain("file.txt");
    }

    [Fact]
    public void RpcRequest_RoundTrips_ThroughJson()
    {
        var original = RpcRequest.Create(BrokerMethods.GetSystemInfo);
        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<RpcRequest>(json);

        restored.Should().NotBeNull();
        restored!.Id.Should().Be(original.Id);
        restored.Method.Should().Be(original.Method);
    }

    [Fact]
    public void RpcResponse_Ok_HasNullError()
    {
        var resp = new RpcResponse("id1", true, "{}", null);
        resp.Ok.Should().BeTrue();
        resp.Error.Should().BeNull();
        resp.ResultJson.Should().Be("{}");
    }

    [Fact]
    public void RpcResponse_Failure_HasNullResult()
    {
        var resp = new RpcResponse("id2", false, null, "access denied");
        resp.Ok.Should().BeFalse();
        resp.Error.Should().Be("access denied");
        resp.ResultJson.Should().BeNull();
    }

    [Fact]
    public void BrokerMethods_PipeName_IsNotEmpty()
    {
        BrokerMethods.PipeName.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void BrokerMethods_AllConstants_AreUnique()
    {
        var constants = new[]
        {
            BrokerMethods.Ping,
            BrokerMethods.CreateRestorePoint,
            BrokerMethods.DeleteFiles,
            BrokerMethods.RunDism,
            BrokerMethods.RegistryWrite,
            BrokerMethods.RegistryDelete,
            BrokerMethods.ServiceSetState,
            BrokerMethods.GetSystemInfo,
            BrokerMethods.FlushDns,
        };
        constants.Should().OnlyHaveUniqueItems();
    }
}
