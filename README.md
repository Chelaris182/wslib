## .NET Websocket server classes
Supports:
- [RFC6455 The WebSocket Protocol](https://tools.ietf.org/html/rfc6455)
- [Per message compression extension](https://tools.ietf.org/html/draft-ietf-hybi-permessage-compression)
- SSL

## Build & Test Status
[![Build status](https://ci.appveyor.com/api/projects/status/bfvv534tpj7t2e8t/branch/master?svg=true)](https://ci.appveyor.com/project/chelaris/wslib/branch/master)

## Quickstart
### Install
```
PM> Install-Package wslib -Pre
```
### Usage
```cs
public static void Main(string[] args)
{
    var listenerOptions = new WebSocketListenerOptions { Endpoint = new IPEndPoint(IPAddress.Loopback, 8080) };
    using (var listener = new WebSocketListener(listenerOptions, appFunc))
    {
        listener.StartAccepting();
        Console.ReadLine();
    }
}

private static async Task appFunc(IWebSocket webSocket)
{
    while (webSocket.IsConnected())
    {
        using (WsMessage msg = await webSocket.ReadMessageAsync(CancellationToken.None).ConfigureAwait(false))
        {
            if (msg == null) continue;

            using (var ms = new MemoryStream())
            {
                await msg.ReadStream.CopyToAsync(ms).ConfigureAwait(false);
                byte[] array = ms.ToArray();
                using (WsMessageWriter w = await webSocket.CreateMessageWriter(msg.Type, CancellationToken.None).ConfigureAwait(false))
                {
                    await w.WriteMessageAsync(array, 0, array.Length, CancellationToken.None).ConfigureAwait(false);
                }
            }
        }
    }
}
```
