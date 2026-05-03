using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MultiStreamVlc;

public class CompanionListener
{
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private readonly Func<string, string, bool> _onStreamReceived;

    public bool IsRunning => _listener?.IsListening == true;

    public CompanionListener(Func<string, string, bool> onStreamReceived)
    {
        _onStreamReceived = onStreamReceived;
    }

    public void Start(string host, int port)
    {
        Stop();

        var prefix = $"http://{host}:{port}/";

        _listener = new HttpListener();
        _listener.Prefixes.Add(prefix);

        try
        {
            _listener.Start();
        }
        catch (HttpListenerException)
        {
            _listener = null;
            return;
        }

        _cts = new CancellationTokenSource();
        _ = ListenLoop(_cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        if (_listener != null)
        {
            try { _listener.Stop(); } catch { }
            _listener.Close();
            _listener = null;
        }
    }

    private async Task ListenLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var context = await _listener!.GetContextAsync();

                if (ct.IsCancellationRequested) break;

                if (context.Request.HttpMethod != "POST")
                {
                    context.Response.StatusCode = 405;
                    context.Response.Close();
                    continue;
                }

                string body;
                using (var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8))
                {
                    body = await reader.ReadToEndAsync(ct);
                }

                CompanionPayload? payload = null;
                try
                {
                    payload = JsonSerializer.Deserialize<CompanionPayload>(body);
                }
                catch { }

                if (payload == null || string.IsNullOrWhiteSpace(payload.url))
                {
                    if (payload?.test == true)
                    {
                        context.Response.StatusCode = 200;
                        context.Response.ContentType = "application/json";
                        var pong = Encoding.UTF8.GetBytes("{\"status\":\"ok\"}");
                        await context.Response.OutputStream.WriteAsync(pong, ct);
                        context.Response.Close();
                        continue;
                    }

                    context.Response.StatusCode = 400;
                    context.Response.ContentType = "application/json";
                    var msg = Encoding.UTF8.GetBytes("{\"error\":\"Invalid JSON. Expected {\\\"name\\\":\\\"...\\\",\\\"url\\\":\\\"...\\\"}\"}");
                    await context.Response.OutputStream.WriteAsync(msg, ct);
                    context.Response.Close();
                    continue;
                }

                bool accepted = false;
                Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
                {
                    accepted = _onStreamReceived(payload.name ?? "Stream", payload.url);
                });

                if (!accepted)
                {
                    context.Response.StatusCode = 400;
                    context.Response.ContentType = "application/json";
                    var err = Encoding.UTF8.GetBytes("{\"error\":\"Unsupported URL. Must use http/https scheme and a valid streaming extension (.m3u8, .mp4, etc.)\"}");
                    await context.Response.OutputStream.WriteAsync(err, ct);
                    context.Response.Close();
                    continue;
                }

                context.Response.StatusCode = 200;
                context.Response.ContentType = "application/json";
                var ok = Encoding.UTF8.GetBytes("{\"status\":\"ok\"}");
                await context.Response.OutputStream.WriteAsync(ok, ct);
                context.Response.Close();
            }
            catch (ObjectDisposedException) { break; }
            catch (HttpListenerException) { break; }
            catch (OperationCanceledException) { break; }
            catch { }
        }
    }

    private class CompanionPayload
    {
        public string? name { get; set; }
        public string? url { get; set; }
        public bool test { get; set; }
    }
}
