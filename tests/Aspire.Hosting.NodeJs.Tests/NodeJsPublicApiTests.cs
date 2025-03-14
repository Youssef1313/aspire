// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;

namespace Aspire.Hosting.NodeJs.Tests;

[TestClass]
public class NodeJsPublicApiTests
{
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void CtorNodeAppResourceShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var name = isNull ? null! : string.Empty;
        const string command = "npm";
        const string workingDirectory = ".\\app";

        Action action = () => new NodeAppResource(name, command, workingDirectory);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void CtorNodeAppResourceShouldThrowWhenCommandIsNullOrEmpty(bool isNull)
    {
        const string name = "NodeApp";
        var command = isNull ? null! : string.Empty;
        const string workingDirectory = ".\\app";

        Action action = () => new NodeAppResource(name, command, workingDirectory);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(command), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void CtorNodeAppResourceShouldThrowWhenWorkingDirectoryIsNullOrEmpty(bool isNull)
    {
        const string name = "NodeApp";
        const string command = "npm";
        var workingDirectory = isNull ? null! : string.Empty;

        Action action = () => new NodeAppResource(name, command, workingDirectory);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(workingDirectory), exception.ParamName);
    }

    [TestMethod]
    public void AddNodeAppShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "NodeApp";
        const string scriptPath = ".\\app.js";

        Action action = () => builder.AddNodeApp(name, scriptPath);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddNodeAppShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var name = isNull ? null! : string.Empty;
        const string scriptPath = ".\\app.js";

        Action action = () => builder.AddNodeApp(name, scriptPath);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddNodeAppShouldThrowWhenScriptPathIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create();
        const string name = "NodeApp";
        var scriptPath = isNull ? null! : string.Empty;

        Action action = () => builder.AddNodeApp(name, scriptPath);

        var exception = isNull
             ? Assert.Throws<ArgumentNullException>(action)
             : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(scriptPath), exception.ParamName);
    }

    [TestMethod]
    public void AddNpmAppShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "NpmApp";
        const string workingDirectory = ".\\app";

        Action action = () => builder.AddNpmApp(name: name, workingDirectory: workingDirectory);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.AreEqual(nameof(builder), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddNpmAppShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var name = isNull ? null! : string.Empty;
        const string workingDirectory = ".\\app";

        Action action = () => builder.AddNpmApp(name: name, workingDirectory);

        var exception = isNull
             ? Assert.Throws<ArgumentNullException>(action)
             : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(name), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddNpmAppShouldThrowWhenWorkingDirectoryIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create();
        const string name = "NpmApp";
        var workingDirectory = isNull ? null! : string.Empty;

        Action action = () => builder.AddNpmApp(name, workingDirectory);

        var exception = isNull
             ? Assert.Throws<ArgumentNullException>(action)
             : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(workingDirectory), exception.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AddNpmAppShouldThrowWhenScriptNameIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create();
        const string name = "NpmApp";
        const string workingDirectory = ".\\app";
        var scriptName = isNull ? null! : string.Empty;

        Action action = () => builder.AddNpmApp(name, workingDirectory, scriptName);

        var exception = isNull
             ? Assert.Throws<ArgumentNullException>(action)
             : Assert.Throws<ArgumentException>(action);
        Assert.AreEqual(nameof(scriptName), exception.ParamName);
    }
}
