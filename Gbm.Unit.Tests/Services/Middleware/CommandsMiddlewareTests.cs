using Gbm.Services.Middleware;
using RA.Console.DependencyInjection.Args;
using RA.Console.DependencyInjection.Middleware;

namespace Gbm.Unit.Tests.Services.Middleware;

public class CommandsMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ReturnsNextResult_OnSuccess()
    {
        var sut = new CommandsMiddleware();
        CommandContext ctx = null;
        var result = await sut.InvokeAsync(ctx, _ => Task.FromResult(0));
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task InvokeAsync_Returns2_OnArgsValidationException()
    {
        var sut = new CommandsMiddleware();
        CommandContext ctx = null;
        var result = await sut.InvokeAsync(ctx, _ => throw new ArgsValidationException("bad args"));
        Assert.Equal(2, result);
    }

    [Fact]
    public async Task InvokeAsync_Returns1_OnGenericException()
    {
        var sut = new CommandsMiddleware();
        CommandContext ctx = null;
        var result = await sut.InvokeAsync(ctx, _ => throw new System.Exception("oops"));
        Assert.Equal(1, result);
    }
}
