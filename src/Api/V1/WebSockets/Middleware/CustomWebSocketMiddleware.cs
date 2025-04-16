using BackBuddy.Api.Service.V1.Exceptions;
using BackBuddy.Api.Service.V1.WebSockets.Services;
using Microsoft.Extensions.Primitives;
using System.Net.WebSockets;
using System.Text;

namespace BackBuddy.Api.Service.V1.WebSockets.Middleware
{
    public class CustomWebSocketMiddleware(RequestDelegate _next, IServiceProvider _serviceProvider)
    {
        public async Task Invoke(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                await _next(context);
                return;
            }
            try
            {
                if (!context.Request.Headers.TryGetValue("Sec-WebSocket-Protocol", out StringValues token))
                {
                    await new UnauthorizedException().WriteToResponse(context.Response);
                    return;
                }

                if (token.Count == 0)
                {
                    await new UnauthorizedException().WriteToResponse(context.Response);
                    return;
                }

                string? authorization = token[0];
                if (string.IsNullOrWhiteSpace(authorization))
                {
                    await new UnauthorizedException().WriteToResponse(context.Response);
                    return;
                }
                WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync(new WebSocketAcceptContext
                {
                    SubProtocol = authorization
                });

                using var scope = _serviceProvider.CreateScope();
                IWebSocketService webSocketService = scope.ServiceProvider.GetRequiredService<IWebSocketService>();
                ILogger<CustomWebSocketMiddleware> logger = scope.ServiceProvider.GetRequiredService<ILogger<CustomWebSocketMiddleware>>();

                bool success = await webSocketService.OnConnect(webSocket, authorization);
                if (!success)
                    return;

                await Receive(webSocket, webSocketService, logger);
            }
            catch (AbstractBaseException ex)
            {
                await ex.WriteToResponse(context.Response);
            }
            catch (Exception)
            {
                await new InternalServerErrorException().WriteToResponse(context.Response);
            }
        }

        private static async Task HandleDisconnect(WebSocket socket, IWebSocketService webSocketService)
        {
            await webSocketService.OnDisconnect(socket);
        }

        private static async Task Receive(WebSocket socket, IWebSocketService webSocketService, ILogger<CustomWebSocketMiddleware> logger)
        {
            var buffer = new byte[1024 * 4];
            try
            {
                while (socket.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult result;
                    using MemoryStream inputStream = new();
                    do
                    {
                        result = await socket.ReceiveAsync(buffer: new ArraySegment<byte>(buffer),
                                                          cancellationToken: CancellationToken.None);
                        await inputStream.WriteAsync(buffer.AsMemory(0, result.Count));
                        // Check if the message is too big
                        if (inputStream.Length > 1024 * 1024 * 50)
                        {
                            await HandleDisconnect(socket, webSocketService);
                            return;
                        }
                    } while (!result.EndOfMessage);


                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string payload = Encoding.UTF8.GetString(inputStream.ToArray());
                        await webSocketService.OnReceive(socket, payload);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await HandleDisconnect(socket, webSocketService);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while receiving a message: {ErrorMessage}", ex.Message);
                await HandleDisconnect(socket, webSocketService);
            }
        }
    }
}