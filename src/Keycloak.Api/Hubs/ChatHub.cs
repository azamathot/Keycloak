using Keycloak.Api.Services;
using Microsoft.AspNetCore.SignalR;

namespace Keycloak.Api.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;

        public ChatHub(IChatService chatService)
        {
            _chatService = chatService;
        }

        public async Task SendMessage(int taskId, string message)
        {
            var userId = Context.UserIdentifier;
            await _chatService.SendMessage(userId, taskId, message);

            // Отправьте сообщение всем клиентам, подключенным к этой задаче
            await Clients.Group(taskId.ToString()).SendAsync("ReceiveMessage", userId, message);
        }

        public override Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            var taskId = Context.GetHttpContext().Request.Query["taskId"];
            if (int.TryParse(taskId, out int taskIdValue))
            {
                Groups.AddToGroupAsync(Context.ConnectionId, taskIdValue.ToString());
            }
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.UserIdentifier;
            // Удаление пользователя из групп
            var groups = Context.GetHttpContext().Request.Query["taskId"];
            if (!string.IsNullOrEmpty(groups))
            {
                foreach (var group in groups.ToString().Split(','))
                {
                    if (int.TryParse(group, out int taskIdValue))
                    {
                        Groups.RemoveFromGroupAsync(Context.ConnectionId, taskIdValue.ToString());
                    }
                }
            }

            return base.OnDisconnectedAsync(exception);
        }
    }
}
