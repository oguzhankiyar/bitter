using System;
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

        private SymbolModel _symbol;
        private ClientWebSocket _socket;

        private event EventHandler<PriceModel> _handler;

        private const string _socketUrlFormat = "wss://stream.binance.com:9443/ws/{0}@trade";

        public Task InitAsync(SymbolModel symbol)
        {
            _symbol = symbol;

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
                var url = string.Format(_socketUrlFormat, _symbol.Name.Replace("|", string.Empty).ToLowerInvariant());

                await _socket.ConnectAsync(new Uri(url), cancellationToken);

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

                    var price = Convert.ToDecimal(root.GetProperty("p").GetString());
                    var time = new DateTime(1970, 1, 1).AddMilliseconds(root.GetProperty("T").GetInt64());

                    _handler?.Invoke(this, new PriceModel
                    {
                        SymbolId = _symbol.Id,
                        Date = time,
                        Price = price
                    });
                }
            }, TaskCreationOptions.LongRunning);

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            await _socket.CloseAsync(WebSocketCloseStatus.Empty, string.Empty, cancellationToken);
        }
    }
}