﻿namespace Keycloak.Api.Hubs
{
    public class ChatMessage
    {
        public string UserId { get; set; }
        public string Message { get; set; }
        public string Sender { get; set; } // Добавьте имя отправителя
    }
}
