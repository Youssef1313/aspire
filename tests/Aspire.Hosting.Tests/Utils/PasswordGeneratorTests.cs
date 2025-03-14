// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using static Aspire.Hosting.Utils.PasswordGenerator;

namespace Aspire.Hosting.Tests.Utils;

[TestClass]
public class PasswordGeneratorTests
{
    [TestMethod]
    public void ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Generate(-1, true, true, true, true, 0, 0, 0, 0));

        Assert.Throws<ArgumentOutOfRangeException>(() => Generate(10, true, true, true, true, -1, 0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Generate(10, true, true, true, true, 0, -1, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Generate(10, true, true, true, true, 0, 0, -1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Generate(10, true, true, true, true, 0, 0, 0, -1));
    }

    [TestMethod]
    public void ThrowsArgumentException()
    {
        // can't have a minimum requirement when that type is disabled
        Assert.Throws<ArgumentException>(() => Generate(10, false, true, true, true, 1, 0, 0, 0));
        Assert.Throws<ArgumentException>(() => Generate(10, true, false, true, true, 0, 1, 0, 0));
        Assert.Throws<ArgumentException>(() => Generate(10, true, true, false, true, 0, 0, 1, 0));
        Assert.Throws<ArgumentException>(() => Generate(10, true, true, true, false, 0, 0, 0, 1));

        Assert.Throws<ArgumentException>(() => Generate(10, false, false, false, false, 0, 0, 0, 0));
    }

    [TestMethod]
    public void ThrowsOverflowException()
    {
        Assert.Throws<OverflowException>(() => Generate(10, true, true, true, true, int.MaxValue, 1, 0, 0));
    }

    [TestMethod]
    [DataRow(true, true, true, true, LowerCaseChars + UpperCaseChars + NumericChars + SpecialChars, null)]
    [DataRow(true, true, true, false, LowerCaseChars + UpperCaseChars + NumericChars, SpecialChars)]
    [DataRow(true, true, false, true, LowerCaseChars + UpperCaseChars + SpecialChars, NumericChars)]
    [DataRow(true, true, false, false, LowerCaseChars + UpperCaseChars, NumericChars + SpecialChars)]
    [DataRow(true, false, true, true, LowerCaseChars + NumericChars + SpecialChars, UpperCaseChars)]
    [DataRow(true, false, true, false, LowerCaseChars + NumericChars, UpperCaseChars + SpecialChars)]
    [DataRow(true, false, false, true, LowerCaseChars + SpecialChars, UpperCaseChars + NumericChars)]
    [DataRow(true, false, false, false, LowerCaseChars, UpperCaseChars + NumericChars + SpecialChars)]
    [DataRow(false, true, true, true, UpperCaseChars + NumericChars + SpecialChars, LowerCaseChars)]
    [DataRow(false, true, true, false, UpperCaseChars + NumericChars, LowerCaseChars + SpecialChars)]
    [DataRow(false, true, false, true, UpperCaseChars + SpecialChars, LowerCaseChars + NumericChars)]
    [DataRow(false, true, false, false, UpperCaseChars, LowerCaseChars + NumericChars + SpecialChars)]
    [DataRow(false, false, true, true, NumericChars + SpecialChars, LowerCaseChars + UpperCaseChars)]
    [DataRow(false, false, true, false, NumericChars, LowerCaseChars + UpperCaseChars + SpecialChars)]
    [DataRow(false, false, false, true, SpecialChars, LowerCaseChars + UpperCaseChars + NumericChars)]
    // NOTE: all false throws ArgumentException
    public void TestGenerate(bool lower, bool upper, bool numeric, bool special, string includes, string? excludes)
    {
        var password = Generate(10, lower, upper, numeric, special, 0, 0, 0, 0);

        Assert.AreEqual(10, password.Length);
        Assert.IsTrue(password.All(includes.Contains));

        if (excludes is not null)
        {
            Assert.IsTrue(!password.Any(excludes.Contains));
        }
    }

    [TestMethod]
    [DataRow(1, 0, 0, 0)]
    [DataRow(0, 1, 0, 0)]
    [DataRow(0, 0, 1, 0)]
    [DataRow(0, 0, 0, 1)]
    [DataRow(0, 2, 1, 0)]
    [DataRow(0, 0, 2, 3)]
    [DataRow(1, 0, 2, 0)]
    [DataRow(5, 1, 1, 1)]
    public void TestGenerateMin(int minLower, int minUpper, int minNumeric, int minSpecial)
    {
        var password = Generate(10, true, true, true, true, minLower, minUpper, minNumeric, minSpecial);

        Assert.AreEqual(10, password.Length);

        if (minLower > 0)
        {
            Assert.IsTrue(password.Count(LowerCaseChars.Contains) >= minLower);
        }
        if (minUpper > 0)
        {
            Assert.IsTrue(password.Count(UpperCaseChars.Contains) >= minUpper);
        }
        if (minNumeric > 0)
        {
            Assert.IsTrue(password.Count(NumericChars.Contains) >= minNumeric);
        }
        if (minSpecial > 0)
        {
            Assert.IsTrue(password.Count(SpecialChars.Contains) >= minSpecial);
        }
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(22)]
    public void ValidUriCharacters(int minLength)
    {
        var password = Generate(minLength, true, true, true, false, 0, 0, 0, 0);
        password += SpecialChars;

        Exception exception = Record.Exception(() => new Uri($"https://guest:{password}@localhost:12345"));

        Assert.IsTrue((exception is null), $"Password contains invalid chars: {password}");
    }

    [TestMethod]
    public void MinLengthLessThanSumMinTypes()
    {
        var password = Generate(7, true, true, true, true, 2, 2, 2, 2);

        Assert.AreEqual(8, password.Length);
    }

    [TestMethod]
    public void WorksWithLargeLengths()
    {
        var password = Generate(1025, true, true, true, true, 0, 0, 0, 0);
        Assert.AreEqual(1025, password.Length);

        password = Generate(10, true, true, true, true, 1024, 1024, 1024, 1025);
        Assert.AreEqual(4097, password.Length);
    }
}
