// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.VisualStudio.TestTools.UnitTesting;

internal static class AssertExtensions
{
    // Workaround https://github.com/microsoft/testfx/issues/1573
    public static T IsInstanceOfType<T>(Assert _, object value)
    {
        Assert.IsInstanceOfType<T>(value);
        return (T)value;
    }

    public static void Collection<T>(IEnumerable<T> collection, params Action<T>[] elementInspectors)
    {
        var index = 0;
        foreach (var element in collection)
        {
            if (index >= elementInspectors.Length)
            {
                Assert.Fail($"Expected {elementInspectors.Length} elements, but found more.");
            }

            elementInspectors[index](element);
            index++;
        }

        if (index != elementInspectors.Length)
        {
            Assert.Fail($"Expected {elementInspectors.Length} elements, but found {index}.");
        }
    }
}
