using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;
using TransactionProcessingSystem.Configuration;

namespace TransactionProcessingSystem.Components.Neo4jExporter;

public sealed class ExporterV2 : IAsyncDisposable
{
    private IDriver? _driver;
    private readonly Neo4jOptions _settings;
    private readonly Neo4jSecrets _secrets;
    private readonly ILogger<ExporterV2> _logger;

    public ExporterV2(Neo4jOptions settings, Neo4jSecrets secrets, ILogger<ExporterV2> logger)
    {
        _settings = settings;
        _secrets = secrets;
        _logger = logger;
    }

    public async Task InitializeDriverAsync()
    {
         _driver = GraphDatabase.Driver( _secrets.Uri, AuthTokens.Basic(_secrets.User, _secrets.Password));
        await _driver.VerifyConnectivityAsync();
    }

    public ValueTask DisposeAsync()
    {
        return _driver?.DisposeAsync() ?? ValueTask.CompletedTask;
    }
}

public class Neo4jLogger : Neo4j.Driver.ILogger {

    private bool debug = false;
    private bool trace = false;

    public void Log(string level, string message) {
        DateTime localDate = DateTime.Now;
        Console.WriteLine($"{localDate.TimeOfDay} {level} {message}");
    }

    public void Error(Exception cause, string message, params object[] args) {
        this.Log("ERROR", String.Format($"{message}", args));
    }
    public void Warn(Exception cause, string message, params object[] args) {
        this.Log("WARN", String.Format($"{message}", args));
    }

    public void Info(string message, params object[] args) {
        this.Log("INFO", String.Format($"{message}", args));
    }

    public void Debug(string message, params object[] args) {
        this.Log("DEBUG", String.Format($"{message}", args));
    }

    public void Trace(string message, params object[] args) {
        this.Log("TRACE", String.Format($"{message}", args));
    }

    public void EnableDebug() {
        this.debug = true;
    }

    public void EnableTrace() {
        this.trace = true;
    }

    public bool IsTraceEnabled() {
        return this.trace;
    }

    public bool IsDebugEnabled() {
        return this.debug;
    }
}