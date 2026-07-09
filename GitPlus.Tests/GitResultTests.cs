using System;
using GitPlus.Commons;
using Xunit;

namespace GitPlus.Tests;

/// <summary>Unit tests for <see cref="GitResult"/>.</summary>
public sealed class GitResultTests
{
    [Fact]
    public void Success_IsSuccessTrue_AndHasOutput()
    {
        var r = GitResult.Success("abc");

        Assert.True(r.IsSuccess);
        Assert.Equal("abc", r.Output);
        Assert.Null(r.Error);
        Assert.Null(r.Exception);
    }

    [Fact]
    public void Success_WithoutOutput_HasEmptyOutput()
    {
        var r = GitResult.Success();
        Assert.True(r.IsSuccess);
        Assert.Equal(string.Empty, r.Output);
    }

    [Fact]
    public void Failure_IsSuccessFalse_HasError()
    {
        var r = GitResult.Failure("something broke");

        Assert.False(r.IsSuccess);
        Assert.Equal("something broke", r.Error);
        Assert.Equal(string.Empty, r.Output);
    }

    [Fact]
    public void Failure_WithException_CapturesException()
    {
        var ex = new InvalidOperationException("test");
        var r  = GitResult.Failure("msg", ex);

        Assert.False(r.IsSuccess);
        Assert.Same(ex, r.Exception);
    }

    [Fact]
    public void Failure_WithoutException_HasNullException()
    {
        var r = GitResult.Failure("msg");
        Assert.Null(r.Exception);
    }

    [Fact]
    public void ToString_Success_IncludesOutput()
    {
        var r = GitResult.Success("hello");
        Assert.Contains("hello", r.ToString());
    }

    [Fact]
    public void ToString_Failure_IncludesError()
    {
        var r = GitResult.Failure("broken");
        Assert.Contains("broken", r.ToString());
    }
}
