using System.Collections.Generic;

namespace Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public int Elo { get; set; } = 100;
        public int TotalPushups { get; set; } = 0;

        // Liste von echten internen Pushup-Einträgen
        public List<PushupEntry> History { get; set; } = new();
    }
}
