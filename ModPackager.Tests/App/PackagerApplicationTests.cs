using FluentAssertions;
using ModPackager.App;
using Moq;
using NUnit.Framework;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.Extensions.Configuration;

namespace ModPackager.Tests.App;

public class PackagerApplicationTests
{
    private Mock<ILogger<PackagerApplication>>? _logger;
    private Mock<IConfiguration>? _config;

    private PackagerApplication? _app;

    [SetUp]
    public void Setup()
    {
        _logger = new Mock<ILogger<PackagerApplication>>();
        _config = new Mock<IConfiguration>();
        _app = new(_logger.Object, _config.Object);
    }

    [Test]
    public void PackagerApplication_ShouldThrow_WhenPassedNullAssemblyPath()
    {
        var args = new CommandLineArgs
        {
            AssemblyPath = null
        };

        _app!.Invoking(async app => await app.PackageModAsync(args))
            .Should()
            .ThrowAsync<ArgumentNullException>();
    }

    [Test]
    public void PackagerApplication_Should_When()
    {
    }
}