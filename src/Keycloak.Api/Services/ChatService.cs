using Keycloak.Api.Hubs;

namespace Keycloak.Api.Services
{
    public class ChatService : IChatService
    {
        // ... Реализуйте методы для работы с базой данных
        public Task<List<ChatMessage>> GetMessagesByTask(string userId, int taskId)
        {
            return Task.FromResult(new List<ChatMessage>());
        }

        public Task SendMessage(string userId, int taskId, string message)
        {
            return Task.CompletedTask;
        }
    }
}
