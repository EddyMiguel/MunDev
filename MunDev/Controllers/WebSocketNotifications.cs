
using System;
using System.Collections.Concurrent; 
using System.Net.WebSockets; 
using System.Security.Claims; 
using System.Text; 
using System.Threading; /
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging; 

namespace MunDev.Controllers
{
    public class WebSocketNotificationsController : Controller
    {
     
        private static ConcurrentDictionary<int, ConcurrentBag<WebSocket>> _userSockets =
            new ConcurrentDictionary<int, ConcurrentBag<WebSocket>>();

        private readonly ILogger<WebSocketNotificationsController> _logger; 

        public WebSocketNotificationsController(ILogger<WebSocketNotificationsController> logger)
        {
            _logger = logger;
        }

        // GET: WebSocketNotifications/Connect (Endpoint para que el cliente se conecte vía WebSocket)
        public async Task<IActionResult> Connect()
        {
            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
                _logger.LogWarning("Solicitud no WebSocket al endpoint de notificaciones.");
                return BadRequest("Solo se permiten conexiones WebSocket.");
            }

           
            int userId = 0;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out userId))
            {
                _logger.LogWarning("Usuario no autenticado o ID de usuario no válido en la solicitud WebSocket.");
                return Unauthorized("Debe estar autenticado para establecer una conexión de notificación.");
            }

            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            _logger.LogInformation($"WebSocket conectado para el usuario {userId}");

            var userSockets = _userSockets.GetOrAdd(userId, new ConcurrentBag<WebSocket>());
            userSockets.Add(webSocket);

            try
            {
                var buffer = new byte[1024 * 4];
                WebSocketReceiveResult result;

                do
                {
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }
                while (!result.CloseStatus.HasValue);

                _logger.LogInformation($"WebSocket cerrado para el usuario {userId}. Estado: {result.CloseStatus} - {result.CloseStatusDescription}");
                await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            }
            catch (WebSocketException ex) 
            {
                _logger.LogError(ex, $"Error WebSocket para el usuario {userId}: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error inesperado en la conexión WebSocket para el usuario {userId}.");
            }
            finally
            {
               
                if (_userSockets.TryGetValue(userId, out var socketsToRemoveFrom))
                {
                    ConcurrentBag<WebSocket> tempBag = new ConcurrentBag<WebSocket>();
                    foreach (var s in socketsToRemoveFrom)
                    {
                        if (s != webSocket) 
                        {
                            tempBag.Add(s);
                        }
                    }
                    _userSockets.TryUpdate(userId, tempBag, socketsToRemoveFrom); 
                    if (tempBag.IsEmpty) 
                    {
                        _userSockets.TryRemove(userId, out _); 
                    }
                }
                _logger.LogInformation($"WebSocket desconectado y limpiado para el usuario {userId}.");
            }

            return new EmptyResult(); 
        }

        
        public static async Task SendNotificationToUser(int userId, string message)
        {
            if (_userSockets.TryGetValue(userId, out var sockets))
            {
                var bytes = Encoding.UTF8.GetBytes(message);
                foreach (var webSocket in sockets)
                {
                    if (webSocket.State == WebSocketState.Open)
                    {
                        try
                        {
                            await webSocket.SendAsync(new ArraySegment<byte>(bytes, 0, bytes.Length), WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error al enviar notificación a WebSocket del usuario {userId}: {ex.Message}");
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine($"Usuario {userId} no conectado para notificaciones WebSocket.");
            }
        }
    }
}
