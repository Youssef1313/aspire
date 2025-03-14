// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.ConsoleLogs;
using Aspire.Hosting.ConsoleLogs;

namespace Aspire.Dashboard.Tests.ConsoleLogsTests;

[TestClass]
public class LogEntriesTests
{
    private static LogEntries CreateLogEntries(int? maximumEntryCount = null, int? baseLineNumber = null)
    {
        var logEntries = new LogEntries(maximumEntryCount: maximumEntryCount ?? int.MaxValue);
        logEntries.BaseLineNumber = baseLineNumber ?? 1;
        return logEntries;
    }

    private static void AddLogLine(LogEntries logEntries, string content, bool isError)
    {
        var logParser = new LogParser();
        var logEntry = logParser.CreateLogEntry(content, isError);
        logEntries.InsertSorted(logEntry);
    }

    [TestMethod]
    public void AddLogLine_Single()
    {
        // Arrange
        var logEntries = CreateLogEntries();

        // Act
        AddLogLine(logEntries, "Hello world", isError: false);

        // Assert
        var entry = Assert.ContainsSingle(logEntries.GetEntries());
        Assert.AreEqual("Hello world", entry.Content);
        Assert.IsNull(entry.Timestamp);
    }

    [TestMethod]
    public void AddLogLine_MultipleLines()
    {
        // Arrange
        var logEntries = CreateLogEntries();

        // Act
        AddLogLine(logEntries, "Hello world", isError: false);
        AddLogLine(logEntries, "Hello world 2", isError: false);
        AddLogLine(logEntries, "Hello world 3", isError: true);

        // Assert
        Assert.That.Collection(logEntries.GetEntries(),
            l => Assert.AreEqual("Hello world", l.Content),
            l => Assert.AreEqual("Hello world 2", l.Content),
            l => Assert.AreEqual("Hello world 3", l.Content));
    }

    [TestMethod]
    public void AddLogLine_MultipleLines_MixDatePrefix()
    {
        // Arrange
        var logEntries = CreateLogEntries();

        // Act
        AddLogLine(logEntries, "Hello world", isError: false);
        AddLogLine(logEntries, "2024-08-19T06:10:01.000Z Hello world 2", isError: false);
        AddLogLine(logEntries, "2024-08-19T06:10:02.000Z Hello world 3", isError: false);
        AddLogLine(logEntries, "Hello world 4", isError: false);
        AddLogLine(logEntries, "2024-08-19T06:10:03.000Z Hello world 5", isError: false);

        // Assert
        var entries = logEntries.GetEntries();
        Assert.That.Collection(entries,
            l =>
            {
                Assert.AreEqual("Hello world", l.Content);
                Assert.AreEqual(1, l.LineNumber);
            },
            l =>
            {
                Assert.AreEqual("Hello world 2", l.Content);
                Assert.AreEqual(2, l.LineNumber);
            },
            l =>
            {
                Assert.AreEqual("Hello world 3", l.Content);
                Assert.AreEqual(3, l.LineNumber);
            },
            l =>
            {
                Assert.AreEqual("Hello world 4", l.Content);
                Assert.AreEqual(4, l.LineNumber);
            },
            l =>
            {
                Assert.AreEqual("Hello world 5", l.Content);
                Assert.AreEqual(5, l.LineNumber);
            });
    }

    [TestMethod]
    public void AddLogLine_MultipleLines_MixDatePrefix_OutOfOrder()
    {
        // Arrange
        var logEntries = CreateLogEntries();

        // Act
        AddLogLine(logEntries, "Hello world", isError: false);
        AddLogLine(logEntries, "2024-08-19T06:12:00.000Z Hello world 2", isError: false);
        AddLogLine(logEntries, "2024-08-19T06:11:00.000Z Hello world 3", isError: false);
        AddLogLine(logEntries, "Hello world 4", isError: false);
        AddLogLine(logEntries, "2024-08-19T06:13:00.000Z Hello world 5", isError: false);
        AddLogLine(logEntries, "2024-08-19T06:10:00.000Z Hello world 6", isError: false);

        // Assert
        var entries = logEntries.GetEntries();
        Assert.That.Collection(entries,
            l =>
            {
                Assert.AreEqual("Hello world", l.Content);
                Assert.AreEqual(1, l.LineNumber);
            },
            l =>
            {
                Assert.AreEqual("Hello world 6", l.Content);
                Assert.AreEqual(2, l.LineNumber);
            },
            l =>
            {
                Assert.AreEqual("Hello world 3", l.Content);
                Assert.AreEqual(3, l.LineNumber);
            },
            l =>
            {
                Assert.AreEqual("Hello world 2", l.Content);
                Assert.AreEqual(4, l.LineNumber);
            },
            l =>
            {
                Assert.AreEqual("Hello world 4", l.Content);
                Assert.AreEqual(5, l.LineNumber);
            },
            l =>
            {
                Assert.AreEqual("Hello world 5", l.Content);
                Assert.AreEqual(6, l.LineNumber);
            });
    }

    [TestMethod]
    public void AddLogLine_MultipleLines_SameDate_InOrder()
    {
        // Arrange
        var logEntries = CreateLogEntries();

        // Act
        AddLogLine(logEntries, "2024-08-19T06:10:00.000Z Hello world 1", isError: false);
        AddLogLine(logEntries, "2024-08-19T06:10:00.000Z Hello world 2", isError: false);

        // Assert
        var entries = logEntries.GetEntries();
        Assert.That.Collection(entries,
            l =>
            {
                Assert.AreEqual("Hello world 1", l.Content);
                Assert.AreEqual(1, l.LineNumber);
            },
            l =>
            {
                Assert.AreEqual("Hello world 2", l.Content);
                Assert.AreEqual(2, l.LineNumber);
            });
    }

    [TestMethod]
    public void InsertSorted_OutOfOrderWithSameTimestamp_ReturnInOrder()
    {
        // Arrange
        var logEntries = CreateLogEntries();

        var timestamp = DateTime.UtcNow;

        // Act
        logEntries.InsertSorted(LogEntry.Create(timestamp.AddSeconds(1), "1", isErrorMessage: false));
        logEntries.InsertSorted(LogEntry.Create(timestamp.AddSeconds(3), "3", isErrorMessage: false));
        logEntries.InsertSorted(LogEntry.Create(timestamp.AddSeconds(2), "2", isErrorMessage: false));

        // Assert
        var entries = logEntries.GetEntries();
        Assert.That.Collection(entries,
            l => Assert.AreEqual("1", l.Content),
            l => Assert.AreEqual("2", l.Content),
            l => Assert.AreEqual("3", l.Content));
    }

    [TestMethod]
    public void InsertSorted_TrimsToMaximumEntryCount_Ordered()
    {
        // Arrange
        var logEntries = CreateLogEntries(maximumEntryCount: 2);

        var timestamp = DateTime.UtcNow;

        // Act
        logEntries.InsertSorted(LogEntry.Create(timestamp.AddSeconds(1), "1", isErrorMessage: false));
        logEntries.InsertSorted(LogEntry.Create(timestamp.AddSeconds(2), "2", isErrorMessage: false));
        logEntries.InsertSorted(LogEntry.Create(timestamp.AddSeconds(3), "3", isErrorMessage: false));

        // Assert
        var entries = logEntries.GetEntries();
        Assert.That.Collection(entries,
            l => Assert.AreEqual("2", l.Content),
            l => Assert.AreEqual("3", l.Content));
    }

    [TestMethod]
    public void InsertSorted_TrimsToMaximumEntryCount_OutOfOrder()
    {
        // Arrange
        var logEntries = CreateLogEntries(maximumEntryCount: 2);

        var timestamp = DateTime.UtcNow;

        // Act
        logEntries.InsertSorted(LogEntry.Create(timestamp.AddSeconds(1), "1", isErrorMessage: false));
        logEntries.InsertSorted(LogEntry.Create(timestamp.AddSeconds(3), "3", isErrorMessage: false));
        logEntries.InsertSorted(LogEntry.Create(timestamp.AddSeconds(2), "2", isErrorMessage: false));

        // Assert
        var entries = logEntries.GetEntries();
        Assert.That.Collection(entries,
            l => Assert.AreEqual("2", l.Content),
            l => Assert.AreEqual("3", l.Content));
    }

    [TestMethod]
    public void CreateLogEntry_AnsiAndUrl_HasUrlAnchor()
    {
        // Arrange
        var parser = new LogParser();

        // Act
        var entry = parser.CreateLogEntry("\x1b[36mhttps://www.example.com\u001b[0m", isErrorOutput: false);

        // Assert
        Assert.AreEqual("<span class=\"ansi-fg-cyan\"></span><a target=\"_blank\" href=\"https://www.example.com\" rel=\"noopener noreferrer nofollow\">https://www.example.com</a>", entry.Content);
    }
}
