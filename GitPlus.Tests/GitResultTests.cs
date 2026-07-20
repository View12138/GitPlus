using System;
using GitPlus.Commons;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GitPlus.Tests;

/// <summary>Unit tests for <see cref="GitResult"/>.</summary>
[TestClass]
public sealed class GitResultTests
{
    [TestMethod]
    public void Success_IsSuccessTrue_AndHasOutput()
    {
        var r = GitResult.Success("abc");

        Assert.IsTrue(r.IsSuccess);
        Assert.AreEqual("abc", r.Output);
        Assert.IsNull(r.Error);
        Assert.IsNull(r.Exception);
    }

    [TestMethod]
    public void Success_WithoutOutput_HasEmptyOutput()
    {
        var r = GitResult.Success();
        Assert.IsTrue(r.IsSuccess);
        Assert.AreEqual(string.Empty, r.Output);
    }

    [TestMethod]
    public void Failure_IsSuccessFalse_HasError()
    {
        var r = GitResult.Failure("something broke");

        Assert.IsFalse(r.IsSuccess);
        Assert.AreEqual("something broke", r.Error);
        Assert.AreEqual(string.Empty, r.Output);
    }

    [TestMethod]
    public void Failure_WithException_CapturesException()
    {
        var ex = new InvalidOperationException("test");
        var r  = GitResult.Failure("msg", ex);

        Assert.IsFalse(r.IsSuccess);
        Assert.AreSame(ex, r.Exception);
    }

    [TestMethod]
    public void Failure_WithoutException_HasNullException()
    {
        var r = GitResult.Failure("msg");
        Assert.IsNull(r.Exception);
    }

    [TestMethod]
    public void ToString_Success_IncludesOutput()
    {
        var r = GitResult.Success("hello");
        Assert.Contains("hello", r.ToString());
    }

    [TestMethod]
    public void ToString_Failure_IncludesError()
    {
        var r = GitResult.Failure("broken");
        Assert.Contains("broken", r.ToString());
    }
}
