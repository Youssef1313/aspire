// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


namespace Aspire.Dashboard.Tests;

[TestClass]
public class CircularBufferTests
{
    private static CircularBuffer<string> CreateBuffer(int capacity) => new(capacity);

    [TestMethod]
    public void AddUntilFull()
    {
        var b = CreateBuffer(5);

        b.Add("0");
        b.Add("1");
        b.Add("2");
        b.Add("3");
        b.Add("4");
        b.Add("5");
        b.Add("6");

        Assert.That.Collection(b,
            i => Assert.AreEqual("2", i),
            i => Assert.AreEqual("3", i),
            i => Assert.AreEqual("4", i),
            i => Assert.AreEqual("5", i),
            i => Assert.AreEqual("6", i));

        Assert.That.Collection(b._buffer,
            i => Assert.AreEqual("5", i),
            i => Assert.AreEqual("6", i),
            i => Assert.AreEqual("2", i),
            i => Assert.AreEqual("3", i),
            i => Assert.AreEqual("4", i));
        Assert.AreEqual(2, b._start);
        Assert.AreEqual(2, b._end);
    }

    [TestMethod]
    public void InsertAtZeroUntilFull()
    {
        var b = CreateBuffer(5);

        b.Insert(0, "0");
        b.Insert(0, "1");
        b.Insert(0, "2");
        b.Insert(0, "3");
        b.Insert(0, "4");
        b.Insert(0, "5");
        b.Insert(0, "6");

        Assert.That.Collection(b,
            i => Assert.AreEqual("4", i),
            i => Assert.AreEqual("3", i),
            i => Assert.AreEqual("2", i),
            i => Assert.AreEqual("1", i),
            i => Assert.AreEqual("0", i));
    }

    [TestMethod]
    public void InsertAtEndUntilFull()
    {
        var b = CreateBuffer(5);

        b.Insert(0, "0");
        b.Insert(1, "1");
        b.Insert(2, "2");
        b.Insert(3, "3");
        b.Insert(4, "4");
        b.Insert(5, "5");
        b.Insert(5, "6");

        Assert.That.Collection(b,
            i => Assert.AreEqual("2", i),
            i => Assert.AreEqual("3", i),
            i => Assert.AreEqual("4", i),
            i => Assert.AreEqual("5", i),
            i => Assert.AreEqual("6", i));
    }

    [TestMethod]
    public void InsertAtPositionUntilFull()
    {
        var b = CreateBuffer(10);

        b.Insert(0, "1");
        b.Insert(1, "2");
        b.Insert(2, "3");
        b.Insert(3, "10");
        b.Insert(3, "9");
        b.Insert(3, "4");
        b.Insert(4, "5");
        b.Insert(5, "7");
        b.Insert(5, "6");
        b.Insert(7, "8");

        Assert.That.Collection(b,
            i => Assert.AreEqual("1", i),
            i => Assert.AreEqual("2", i),
            i => Assert.AreEqual("3", i),
            i => Assert.AreEqual("4", i),
            i => Assert.AreEqual("5", i),
            i => Assert.AreEqual("6", i),
            i => Assert.AreEqual("7", i),
            i => Assert.AreEqual("8", i),
            i => Assert.AreEqual("9", i),
            i => Assert.AreEqual("10", i));
    }

    [TestMethod]
    public void InsertInMiddleWhileFull()
    {
        var b = CreateBuffer(10);

        b.Add("0");
        b.Add("1");
        b.Add("2");
        b.Add("3");
        b.Add("4");
        b.Add("5");
        b.Add("6");
        b.Add("7");
        b.Add("8");
        b.Add("9");
        b.Add("10");
        b.Add("11");

        b.Insert(3, "4.5");

        Assert.That.Collection(b,
            i => Assert.AreEqual("3", i),
            i => Assert.AreEqual("4", i),
            i => Assert.AreEqual("4.5", i),
            i => Assert.AreEqual("5", i),
            i => Assert.AreEqual("6", i),
            i => Assert.AreEqual("7", i),
            i => Assert.AreEqual("8", i),
            i => Assert.AreEqual("9", i),
            i => Assert.AreEqual("10", i),
            i => Assert.AreEqual("11", i));

        Assert.That.Collection(b._buffer,
            i => Assert.AreEqual("9", i),
            i => Assert.AreEqual("10", i),
            i => Assert.AreEqual("11", i),
            i => Assert.AreEqual("3", i),
            i => Assert.AreEqual("4", i),
            i => Assert.AreEqual("4.5", i),
            i => Assert.AreEqual("5", i),
            i => Assert.AreEqual("6", i),
            i => Assert.AreEqual("7", i),
            i => Assert.AreEqual("8", i));
        Assert.AreEqual(3, b._start);
        Assert.AreEqual(3, b._end);

        b.Insert(7, "8.5");

        Assert.That.Collection(b,
            i => Assert.AreEqual("4", i),
            i => Assert.AreEqual("4.5", i),
            i => Assert.AreEqual("5", i),
            i => Assert.AreEqual("6", i),
            i => Assert.AreEqual("7", i),
            i => Assert.AreEqual("8", i),
            i => Assert.AreEqual("8.5", i),
            i => Assert.AreEqual("9", i),
            i => Assert.AreEqual("10", i),
            i => Assert.AreEqual("11", i));

        Assert.That.Collection(b._buffer,
            i => Assert.AreEqual("8.5", i),
            i => Assert.AreEqual("9", i),
            i => Assert.AreEqual("10", i),
            i => Assert.AreEqual("11", i),
            i => Assert.AreEqual("4", i),
            i => Assert.AreEqual("4.5", i),
            i => Assert.AreEqual("5", i),
            i => Assert.AreEqual("6", i),
            i => Assert.AreEqual("7", i),
            i => Assert.AreEqual("8", i));
        Assert.AreEqual(4, b._start);
        Assert.AreEqual(4, b._end);

        b.Insert(5, "7.5");

        Assert.That.Collection(b,
            i => Assert.AreEqual("4.5", i),
            i => Assert.AreEqual("5", i),
            i => Assert.AreEqual("6", i),
            i => Assert.AreEqual("7", i),
            i => Assert.AreEqual("7.5", i),
            i => Assert.AreEqual("8", i),
            i => Assert.AreEqual("8.5", i),
            i => Assert.AreEqual("9", i),
            i => Assert.AreEqual("10", i),
            i => Assert.AreEqual("11", i));
    }

    [TestMethod]
    public void InsertAfterRemove()
    {
        var b = CreateBuffer(5);

        b.Add("0");
        b.Add("1");
        b.Add("2");
        b.Add("3");
        b.Add("4");
        b.Add("5");
        b.Add("6");

        b.RemoveAt(2);

        b.Insert(3, "5.5");

        Assert.That.Collection(b,
            i => Assert.AreEqual("2", i),
            i => Assert.AreEqual("3", i),
            i => Assert.AreEqual("5", i),
            i => Assert.AreEqual("5.5", i),
            i => Assert.AreEqual("6", i));

        b.Add("7");

        Assert.That.Collection(b,
            i => Assert.AreEqual("3", i),
            i => Assert.AreEqual("5", i),
            i => Assert.AreEqual("5.5", i),
            i => Assert.AreEqual("6", i),
            i => Assert.AreEqual("7", i));
    }

    [TestMethod]
    public void RemoveAtMiddleUnderCapacity()
    {
        var b = CreateBuffer(10);

        b.Add("0");
        b.Add("1");
        b.Add("2");
        b.Add("3");
        b.Add("4");
        b.Add("5");
        b.Add("6");

        b.RemoveAt(2);

        Assert.That.Collection(b,
            i => Assert.AreEqual("0", i),
            i => Assert.AreEqual("1", i),
            i => Assert.AreEqual("3", i),
            i => Assert.AreEqual("4", i),
            i => Assert.AreEqual("5", i),
            i => Assert.AreEqual("6", i));

        b.Add("7");
        b.RemoveAt(2);

        Assert.That.Collection(b,
            i => Assert.AreEqual("0", i),
            i => Assert.AreEqual("1", i),
            i => Assert.AreEqual("4", i),
            i => Assert.AreEqual("5", i),
            i => Assert.AreEqual("6", i),
            i => Assert.AreEqual("7", i));

        b.Add("8");
        b.RemoveAt(4);

        Assert.That.Collection(b,
            i => Assert.AreEqual("0", i),
            i => Assert.AreEqual("1", i),
            i => Assert.AreEqual("4", i),
            i => Assert.AreEqual("5", i),
            i => Assert.AreEqual("7", i),
            i => Assert.AreEqual("8", i));

        b.Add("9");
        b.RemoveAt(5);

        Assert.That.Collection(b,
            i => Assert.AreEqual("0", i),
            i => Assert.AreEqual("1", i),
            i => Assert.AreEqual("4", i),
            i => Assert.AreEqual("5", i),
            i => Assert.AreEqual("7", i),
            i => Assert.AreEqual("9", i));

        b.Add("10");
        b.RemoveAt(0);

        Assert.That.Collection(b,
            i => Assert.AreEqual("1", i),
            i => Assert.AreEqual("4", i),
            i => Assert.AreEqual("5", i),
            i => Assert.AreEqual("7", i),
            i => Assert.AreEqual("9", i),
            i => Assert.AreEqual("10", i));
    }

    [TestMethod]
    public void InsertInMiddleLarge()
    {
        var b = CreateBuffer(5);

        b.Add("0");
        b.Add("1");
        b.Add("2");
        b.Add("3");
        b.Add("4");
        b.Add("5");
        b.Add("6");
        b.Add("7");
        b.Add("8");
        b.Add("9");
        b.Add("10");
        b.Add("11");

        b.Insert(0, "6.5");
        Assert.That.Collection(b,
            i => Assert.AreEqual("7", i),
            i => Assert.AreEqual("8", i),
            i => Assert.AreEqual("9", i),
            i => Assert.AreEqual("10", i),
            i => Assert.AreEqual("11", i));

        b.Insert(1, "7.5");
        Assert.That.Collection(b,
            i => Assert.AreEqual("7.5", i),
            i => Assert.AreEqual("8", i),
            i => Assert.AreEqual("9", i),
            i => Assert.AreEqual("10", i),
            i => Assert.AreEqual("11", i));

        b.Insert(2, "8.5");
        Assert.That.Collection(b,
            i => Assert.AreEqual("8", i),
            i => Assert.AreEqual("8.5", i),
            i => Assert.AreEqual("9", i),
            i => Assert.AreEqual("10", i),
            i => Assert.AreEqual("11", i));

        b.Insert(3, "9.5");
        Assert.That.Collection(b,
            i => Assert.AreEqual("8.5", i),
            i => Assert.AreEqual("9", i),
            i => Assert.AreEqual("9.5", i),
            i => Assert.AreEqual("10", i),
            i => Assert.AreEqual("11", i));

        b.Insert(4, "10.5");
        Assert.That.Collection(b,
            i => Assert.AreEqual("9", i),
            i => Assert.AreEqual("9.5", i),
            i => Assert.AreEqual("10", i),
            i => Assert.AreEqual("10.5", i),
            i => Assert.AreEqual("11", i));

        b.Insert(5, "11.5");
        Assert.That.Collection(b,
            i => Assert.AreEqual("9.5", i),
            i => Assert.AreEqual("10", i),
            i => Assert.AreEqual("10.5", i),
            i => Assert.AreEqual("11", i),
            i => Assert.AreEqual("11.5", i));
    }

    [TestMethod]
    public void RemoveAtInMiddleLarge()
    {
        var b = CreateBuffer(5);

        b.Add("0");
        b.Add("1");
        b.Add("2");
        b.Add("3");
        b.Add("4");
        b.Add("5");
        b.Add("6");
        b.Add("7");
        b.Add("8");
        b.Add("9");
        b.Add("10");
        b.Add("11");

        b.RemoveAt(1);

        Assert.That.Collection(b,
            i => Assert.AreEqual("7", i),
            i => Assert.AreEqual("9", i),
            i => Assert.AreEqual("10", i),
            i => Assert.AreEqual("11", i));

        b.RemoveAt(1);

        Assert.That.Collection(b,
            i => Assert.AreEqual("7", i),
            i => Assert.AreEqual("10", i),
            i => Assert.AreEqual("11", i));

        b.RemoveAt(1);

        Assert.That.Collection(b,
            i => Assert.AreEqual("7", i),
            i => Assert.AreEqual("11", i));

        b.Add("12");
        b.Add("13");
        b.Add("14");

        Assert.That.Collection(b,
            i => Assert.AreEqual("7", i),
            i => Assert.AreEqual("11", i),
            i => Assert.AreEqual("12", i),
            i => Assert.AreEqual("13", i),
            i => Assert.AreEqual("14", i));

        b.Add("15");
        b.Add("16");

        Assert.That.Collection(b,
            i => Assert.AreEqual("12", i),
            i => Assert.AreEqual("13", i),
            i => Assert.AreEqual("14", i),
            i => Assert.AreEqual("15", i),
            i => Assert.AreEqual("16", i));
    }

    [TestMethod]
    public void RemoveAtAndInsertMiddleLarge()
    {
        var b = CreateBuffer(5);

        b.Add("0");
        b.Add("1");
        b.Add("2");
        b.Add("3");
        b.Add("4");
        b.Add("5");
        b.Add("6");
        b.Add("7");
        b.Add("8");
        b.Add("9");
        b.Add("10");
        b.Add("11");

        b.RemoveAt(1);

        Assert.That.Collection(b,
            i => Assert.AreEqual("7", i),
            i => Assert.AreEqual("9", i),
            i => Assert.AreEqual("10", i),
            i => Assert.AreEqual("11", i));

        b.RemoveAt(1);

        Assert.That.Collection(b,
            i => Assert.AreEqual("7", i),
            i => Assert.AreEqual("10", i),
            i => Assert.AreEqual("11", i));

        b.RemoveAt(1);

        Assert.That.Collection(b,
            i => Assert.AreEqual("7", i),
            i => Assert.AreEqual("11", i));

        b.Insert(0, "6");

        Assert.That.Collection(b,
            i => Assert.AreEqual("6", i),
            i => Assert.AreEqual("7", i),
            i => Assert.AreEqual("11", i));

        b.Insert(1, "6.5");

        Assert.That.Collection(b,
            i => Assert.AreEqual("6", i),
            i => Assert.AreEqual("6.5", i),
            i => Assert.AreEqual("7", i),
            i => Assert.AreEqual("11", i));

        b.Insert(3, "7.5");

        Assert.That.Collection(b,
            i => Assert.AreEqual("6", i),
            i => Assert.AreEqual("6.5", i),
            i => Assert.AreEqual("7", i),
            i => Assert.AreEqual("7.5", i),
            i => Assert.AreEqual("11", i));

        b.Insert(5, "12");

        Assert.That.Collection(b,
            i => Assert.AreEqual("6.5", i),
            i => Assert.AreEqual("7", i),
            i => Assert.AreEqual("7.5", i),
            i => Assert.AreEqual("11", i),
            i => Assert.AreEqual("12", i));
    }

    [TestMethod]
    public void RemoveAtStartToZero()
    {
        var b = CreateBuffer(5);

        b.Add("0");
        b.Add("1");
        b.Add("2");
        b.Add("3");
        b.Add("4");
        b.Add("5");
        b.Add("6");
        b.Add("7");
        b.Add("8");
        b.Add("9");
        b.Add("10");
        b.Add("11");

        b.RemoveAt(0);
        Assert.That.Collection(b,
            i => Assert.AreEqual("8", i),
            i => Assert.AreEqual("9", i),
            i => Assert.AreEqual("10", i),
            i => Assert.AreEqual("11", i));

        b.RemoveAt(0);
        Assert.That.Collection(b,
            i => Assert.AreEqual("9", i),
            i => Assert.AreEqual("10", i),
            i => Assert.AreEqual("11", i));

        b.RemoveAt(0);
        Assert.That.Collection(b,
            i => Assert.AreEqual("10", i),
            i => Assert.AreEqual("11", i));

        b.RemoveAt(0);
        Assert.That.Collection(b,
            i => Assert.AreEqual("11", i));

        b.RemoveAt(0);
        Assert.IsEmpty(b);
    }

    [TestMethod]
    public void RemoveAtEndToZero()
    {
        var b = CreateBuffer(5);

        b.Add("0");
        b.Add("1");
        b.Add("2");
        b.Add("3");
        b.Add("4");
        b.Add("5");
        b.Add("6");
        b.Add("7");
        b.Add("8");
        b.Add("9");
        b.Add("10");
        b.Add("11");

        b.RemoveAt(4);
        Assert.That.Collection(b,
            i => Assert.AreEqual("7", i),
            i => Assert.AreEqual("8", i),
            i => Assert.AreEqual("9", i),
            i => Assert.AreEqual("10", i));

        b.RemoveAt(3);
        Assert.That.Collection(b,
            i => Assert.AreEqual("7", i),
            i => Assert.AreEqual("8", i),
            i => Assert.AreEqual("9", i));

        b.RemoveAt(2);
        Assert.That.Collection(b,
            i => Assert.AreEqual("7", i),
            i => Assert.AreEqual("8", i));

        b.RemoveAt(1);
        Assert.That.Collection(b,
            i => Assert.AreEqual("7", i));

        b.RemoveAt(0);
        Assert.IsEmpty(b);
    }

    [TestMethod]
    public void Insert_BeforeEnd_EndInMiddle()
    {
        var values = new List<string>
        {
            "10",
            "12",
            "0",
            "2",
            "2",
            "4",
            "4",
            "6",
            "6",
            "8",
        };

        var buffer = new CircularBuffer<string>(values, capacity: 10, start: 2, end: 2);
        buffer.Insert(9, "11");

        Assert.That.Collection(buffer,
            i => Assert.AreEqual("2", i),
            i => Assert.AreEqual("2", i),
            i => Assert.AreEqual("4", i),
            i => Assert.AreEqual("4", i),
            i => Assert.AreEqual("6", i),
            i => Assert.AreEqual("6", i),
            i => Assert.AreEqual("8", i),
            i => Assert.AreEqual("10", i),
            i => Assert.AreEqual("11", i),
            i => Assert.AreEqual("12", i));
    }

    [TestMethod]
    public void Clear_EmptiesBuffer_ResetsIndex()
    {
        var b = CreateBuffer(5);

        b.Insert(0, "0");
        b.Insert(0, "1");
        b.Insert(0, "2");

        b.Clear();

        Assert.IsEmpty(b);

        b.Insert(0, "0");
        b.Insert(0, "1");
        b.Insert(0, "2");

        Assert.That.Collection(b,
            i => Assert.AreEqual("2", i),
            i => Assert.AreEqual("1", i),
            i => Assert.AreEqual("0", i));
    }
}
