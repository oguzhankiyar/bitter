using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OK.Bitter.Common.Models;

namespace OK.Bitter.Engine.Streams
{
    public class PriceStream : IPriceStream
    {
        public string State => _socket.State.ToString();

        private List<string> _symbols;
        private ClientWebSocket _socket;
        private Timer _timer;

        private event EventHandler<PriceModel> _handler;

        private const string _socketUrlFormat = "wss://stream.binance.com:9443/ws";

        public Task InitAsync(List<string> symbols)
        {
            _symbols = symbols;

            _socket = new ClientWebSocket();

            return Task.CompletedTask;
        }

        public Task SubscribeAsync(EventHandler<PriceModel> handler, CancellationToken cancellationToken = default)
        {
            _handler += handler;

            return Task.CompletedTask;
        }

        public Task UnsubscribeAsync(EventHandler<PriceModel> handler, CancellationToken cancellationToken = default)
        {
            _handler -= handler;

            return Task.CompletedTask;
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            Task.Factory.StartNew(async () =>
            {
                var url = _socketUrlFormat;

                await _socket.ConnectAsync(new Uri(url), cancellationToken);

                var req = @"{ ""method"": ""SUBSCRIBE"", ""params"": [" + string.Join(",", _symbols.Take(4).Select(x => $@"""{x.ToLowerInvariant()}@aggTrade""")) + @"], ""id"": 1 }";
                var bytes = Encoding.UTF8.GetBytes(req);
                await _socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);

                while (!cancellationToken.IsCancellationRequested && _socket.State == WebSocketState.Open)
                {
                    var buffer = new byte[256];
                    var offset = 0;

                    while (true)
                    {
                        var bytesReceived = new ArraySegment<byte>(buffer);
                        var result = await _socket.ReceiveAsync(bytesReceived, cancellationToken);

                        offset += result.Count;

                        if (result.EndOfMessage)
                        {
                            break;
                        }
                    }

                    var msg = Encoding.UTF8.GetString(buffer, 0, offset);

                    var json = JsonDocument.Parse(msg);
                    var root = json.RootElement;

                    if (root.TryGetProperty("result", out var elem))
                    {
                        continue;
                    }

                    var symbol = root.GetProperty("s").GetString();
                    var price = Convert.ToDecimal(root.GetProperty("p").GetString());
                    var time = new DateTime(1970, 1, 1).AddMilliseconds(root.GetProperty("T").GetInt64());

                    _handler?.Invoke(this, new PriceModel
                    {
                        SymbolId = symbol,
                        Date = time,
                        Price = price
                    });
                }
            }, TaskCreationOptions.LongRunning);

            Task.Factory.StartNew(() =>
            {
                if (_timer != null)
                {
                    _timer.Dispose();
                    _timer = null;
                }

                _timer = new Timer(async (_) =>
                {
                    await StopAsync(cancellationToken);
                    await StartAsync(cancellationToken);
                }, null, TimeSpan.FromHours(4), TimeSpan.FromHours(4));
            }, TaskCreationOptions.LongRunning);

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            await _socket.CloseAsync(WebSocketCloseStatus.Empty, string.Empty, cancellationToken);
        }
    }
}