// Controllers/WebSocketNotificationsController.cs
// Este controlador gestiona las conexiones WebSocket para notificaciones
// en tiempo real dirigidas a usuarios individuales.
// Renombrado para evitar conflicto con tu NotificationsController existente.

using System;
using System.Collections.Concurrent; // Para ConcurrentDictionary (hilo-seguro)
using System.Net.WebSockets; // Para WebSockets
using System.Security.Claims; // Para acceder a los Claims del usuario autenticado
using System.Text; // Para codificación de texto (UTF8)
using System.Threading; // Para CancellationToken
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging; // Opcional: para logging

namespace MunDev.Controllers
{
    // Cambiamos el nombre de la clase del controlador
    public class WebSocketNotificationsController : Controller
    {
        // Un diccionario estático para almacenar las conexiones WebSocket de cada usuario.
        // La clave es el UserId (int), y el valor es una lista hilo-segura de WebSockets,
        // ya que un usuario podría tener múltiples pestañas/conexiones abiertas.
        private static ConcurrentDictionary<int, ConcurrentBag<WebSocket>> _userSockets =
            new ConcurrentDictionary<int, ConcurrentBag<WebSocket>>();

        private readonly ILogger<WebSocketNotificationsController> _logger; // Opcional para logging

        // Inyecta el logger. No necesita el MunDevContext porque no interactúa directamente con la DB para CRUD.
        public WebSocketNotificationsController(ILogger<WebSocketNotificationsController> logger)
        {
            _logger = logger;
        }

        // GET: WebSocketNotifications/Connect (Endpoint para que el cliente se conecte vía WebSocket)
        // Requiere que el usuario esté autenticado para identificarlo.
        public async Task<IActionResult> Connect()
        {
            // 1. Verificar si la solicitud es una conexión WebSocket.
            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
                _logger.LogWarning("Solicitud no WebSocket al endpoint de notificaciones.");
                return BadRequest("Solo se permiten conexiones WebSocket.");
            }

            // 2. Obtener el ID del usuario autenticado.
            // Asume que el UserId se almacena como un claim de tipo NameIdentifier y es un INT.
            // Adapta esto si tu UserId es un GUID (string) o un tipo diferente.
            int userId = 0;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out userId))
            {
                _logger.LogWarning("Usuario no autenticado o ID de usuario no válido en la solicitud WebSocket.");
                return Unauthorized("Debe estar autenticado para establecer una conexión de notificación.");
            }

            // 3. Aceptar la conexión WebSocket.
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            _logger.LogInformation($"WebSocket conectado para el usuario {userId}");

            // 4. Añadir el WebSocket a la lista de sockets del usuario.
            var userSockets = _userSockets.GetOrAdd(userId, new ConcurrentBag<WebSocket>());
            userSockets.Add(webSocket);

            try
            {
                // Bucle principal para mantener la conexión abierta y procesar mensajes (si los hay desde el cliente).
                var buffer = new byte[1024 * 4];
                WebSocketReceiveResult result;

                do
                {
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }
                while (!result.CloseStatus.HasValue);

                // 5. La conexión se está cerrando.
                _logger.LogInformation($"WebSocket cerrado para el usuario {userId}. Estado: {result.CloseStatus} - {result.CloseStatusDescription}");
                await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            }
            catch (WebSocketException ex) // Manejar interrupciones de conexión inesperadas
            {
                _logger.LogError(ex, $"Error WebSocket para el usuario {userId}: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error inesperado en la conexión WebSocket para el usuario {userId}.");
            }
            finally
            {
                // 6. Eliminar el WebSocket de la lista del usuario.
                if (_userSockets.TryGetValue(userId, out var socketsToRemoveFrom))
                {
                    ConcurrentBag<WebSocket> tempBag = new ConcurrentBag<WebSocket>();
                    foreach (var s in socketsToRemoveFrom)
                    {
                        if (s != webSocket) // Reconstruye la bolsa sin el socket que se está cerrando
                        {
                            tempBag.Add(s);
                        }
                    }
                    _userSockets.TryUpdate(userId, tempBag, socketsToRemoveFrom); // Actualiza la bolsa
                    if (tempBag.IsEmpty) // Si ya no quedan sockets para este usuario
                    {
                        _userSockets.TryRemove(userId, out _); // Elimina la entrada del usuario si no hay conexiones
                    }
                }
                _logger.LogInformation($"WebSocket desconectado y limpiado para el usuario {userId}.");
            }

            return new EmptyResult(); // No hay contenido HTTP que devolver para una conexión WebSocket
        }

        // === Método Estático para Enviar Notificaciones ===
        // Este método puede ser llamado desde cualquier parte de tu aplicación (ej. InvitationsController)
        // para enviar un mensaje a un usuario específico.
        public static async Task SendNotificationToUser(int userId, string message)
        {
            if (_userSockets.TryGetValue(userId, out var sockets))
            {
                var bytes = Encoding.UTF8.GetBytes(message);
                // Itera sobre todas las conexiones del usuario para enviar el mensaje.
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
