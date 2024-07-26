using Keycloak.Api.Hubs;

namespace Keycloak.Api.Services
{
    public interface IChatService
    {
        Task<List<ChatMessage>> GetMessagesByTask(string userId, int taskId);
        Task SendMessage(string userId, int taskId, string message);
        // ... другие методы
    }

}
