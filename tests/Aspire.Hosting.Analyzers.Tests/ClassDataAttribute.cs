// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Reflection;

namespace Aspire.Hosting.Analyzers.Tests;

[AttributeUsage(AttributeTargets.Method)]
internal sealed class ClassDataAttribute : Attribute, ITestDataSource
{
    private readonly Type _type;

    public ClassDataAttribute(Type type)
        => _type = type;

    public IEnumerable<object?[]> GetData(MethodInfo methodInfo)
    {
        var dataSource = (IEnumerable)Activator.CreateInstance(_type)!;
        foreach (var data in dataSource)
        {
            yield return [data];
        }
    }

    public string? GetDisplayName(MethodInfo methodInfo, object?[]? data)
    {
        if (data is null)
        {
            return null;
        }

        ParameterInfo[] parameters = methodInfo.GetParameters();
        IEnumerable<object?> displayData = parameters.Length == 1 && parameters[0].ParameterType == typeof(object[])
            ? [data.AsEnumerable()]
            : data.AsEnumerable();
        string methodDisplayName = methodInfo.Name;
        return $"{methodInfo.Name} ({string.Join(",", displayData.Select(GetHumanizedArguments))})";
    }

    private static string? GetHumanizedArguments(object? data)
    {
        if (data is null)
        {
            return "null";
        }

        if (!data.GetType().IsArray)
        {
            return data switch
            {
                string s => $"\"{s}\"",
                char c => $"'{c}'",
                _ => data.ToString(),
            };
        }
        // We need to box the object here so that we can support value types
        IEnumerable<object> boxedObjectEnumerable = ((IEnumerable)data).Cast<object>();
        IEnumerable<string?> elementStrings = boxedObjectEnumerable.Select(GetHumanizedArguments);
        return $"[{string.Join(",", elementStrings)}]";
    }
}
