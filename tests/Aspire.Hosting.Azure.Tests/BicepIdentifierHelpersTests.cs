// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Azure.Utils;

namespace Aspire.Hosting.Azure.Tests;

[TestClass]
public class BicepIdentifierHelpersTests
{
    [TestMethod]
    [DataRow("my-variable")]
    [DataRow("my variable")]
    [DataRow("_my-variable")]
    [DataRow("_my variable")]
    [DataRow("1my_variable")]
    [DataRow("1my-variable")]
    [DataRow("1my variable")]
    [DataRow("my_variable@")]
    [DataRow("my_variable-")]
    [DataRow("my_\u212A_variable")] // tests the Kelvin sign
    [DataRow("my_\u0130_variable")] // non-ASCII letter
    public void TestThrowIfInvalid(string value)
    {
        var e = Assert.Throws<ArgumentException>(() => BicepIdentifierHelpers.ThrowIfInvalid(value));

        // Verify the parameter name is from the caller member name. In this case, the "value" parameter above
        Assert.AreEqual(nameof(value), e.ParamName); 
    }
}
