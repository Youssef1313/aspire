// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


namespace Aspire.Hosting.Tests.Utils;

[TestClass]
public class WithAnnotationTests
{
    [TestMethod]
    public void WithAnnotationWithTypeParameterAndNoExplicitBehaviorAppends()
    {
        var builder = DistributedApplication.CreateBuilder();
        var redis = builder.AddRedis("redis")
                           .WithAnnotation<DummyAnnotation>()
                           .WithAnnotation<DummyAnnotation>();

        var dummyAnnotations = redis.Resource.Annotations.OfType<DummyAnnotation>();

        Assert.AreEqual(2, dummyAnnotations.Count());
        Assert.AreNotEqual(dummyAnnotations.First(), dummyAnnotations.Last());
    }

    [TestMethod]
    public void WithAnnotationWithTypeParameterAndArgumentAndNoExplicitBehaviorAppends()
    {
        var builder = DistributedApplication.CreateBuilder();
        var redis = builder.AddRedis("redis")
                           .WithAnnotation<DummyAnnotation>(new DummyAnnotation())
                           .WithAnnotation<DummyAnnotation>(new DummyAnnotation());

        var dummyAnnotations = redis.Resource.Annotations.OfType<DummyAnnotation>();

        Assert.AreEqual(2, dummyAnnotations.Count());
        Assert.AreNotEqual(dummyAnnotations.First(), dummyAnnotations.Last());
    }

    [TestMethod]
    public void WithAnnotationWithTypeParameterAndArgumentAndAddReplaceBehaviorReplaces()
    {
        var builder = DistributedApplication.CreateBuilder();
        var redis = builder.AddRedis("redis").WithAnnotation<DummyAnnotation>();

        var firstAnnotation = redis.Resource.Annotations.OfType<DummyAnnotation>().Single();

        redis.WithAnnotation<DummyAnnotation>(ResourceAnnotationMutationBehavior.Replace);

        var secondAnnotation = redis.Resource.Annotations.OfType<DummyAnnotation>().Single();

        Assert.AreNotEqual(firstAnnotation, secondAnnotation);
    }
}

public class DummyAnnotation : IResourceAnnotation
{
}
