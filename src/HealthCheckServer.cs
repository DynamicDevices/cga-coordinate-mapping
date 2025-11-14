#nullable enable
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace InstDotNet;

/// <summary>
/// HTTP server for health check endpoint
/// </summary>
public static class HealthCheckServer
{
    private static HttpListener? _listener;
    private static Task? _serverTask;
    private static CancellationTokenSource? _cts;
    private static ILogger? _logger;
    private static int _port = 8080;

    /// <summary>
    /// Start the health check HTTP server
    /// </summary>
    public static void Start(int port = 8080, ILogger? logger = null)
    {
        _port = port;
        _logger = logger;
        _cts = new CancellationTokenSource();

        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://+:{port}/");
        
        try
        {
            _listener.Start();
            _logger?.LogInformation("Health check server started on port {Port}", port);
            
            _serverTask = Task.Run(async () => await ListenAsync(_cts.Token), _cts.Token);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to start health check server on port {Port}", port);
            throw;
        }
    }

    /// <summary>
    /// Stop the health check server
    /// </summary>
    public static void Stop()
    {
        _cts?.Cancel();
        _listener?.Stop();
        _listener?.Close();
        _logger?.LogInformation("Health check server stopped");
    }

    private static async Task ListenAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _listener != null && _listener.IsListening)
        {
            try
            {
                var context = await _listener.GetContextAsync().ConfigureAwait(false);
                _ = Task.Run(() => HandleRequest(context), cancellationToken);
            }
            catch (ObjectDisposedException)
            {
                // Listener was closed, exit gracefully
                break;
            }
            catch (HttpListenerException ex)
            {
                _logger?.LogWarning(ex, "Health check server error");
                if (!_listener.IsListening)
                    break;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error in health check server");
            }
        }
    }

    private static void HandleRequest(HttpListenerContext context)
    {
        try
        {
            var request = context.Request;
            var response = context.Response;

            // Only handle GET requests to /health
            if (request.HttpMethod == "GET" && request.Url?.AbsolutePath == "/health")
            {
                var status = HealthCheck.GetStatus();
                var json = HealthCheck.GetStatusJson();
                
                // Set status code based on health
                response.StatusCode = status.Status == "healthy" ? 200 : 503;
                response.ContentType = "application/json";
                response.ContentEncoding = Encoding.UTF8;

                var buffer = Encoding.UTF8.GetBytes(json);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();

                _logger?.LogDebug("Health check request: {Status}", status.Status);
            }
            else
            {
                // 404 for other paths
                response.StatusCode = 404;
                response.Close();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling health check request");
            try
            {
                context.Response.StatusCode = 500;
                context.Response.Close();
            }
            catch
            {
                // Ignore errors when closing response
            }
        }
    }
}

