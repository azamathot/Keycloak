using Keycloak.Api.Hubs;
using Keycloak.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Keycloak.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly IChatService _chatService;

        public ChatController(IHubContext<ChatHub> hubContext, IChatService chatService)
        {
            _hubContext = hubContext;
            _chatService = chatService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMessages(int taskId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Получите ID пользователя из Keycloak
            var messages = await _chatService.GetMessagesByTask(userId, taskId);
            return Ok(messages);
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage(int taskId, string message)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Получите ID пользователя из Keycloak
            await _chatService.SendMessage(userId, taskId, message);
            return Ok();
        }

        // ... другие методы API для управления чатом
    }
}
