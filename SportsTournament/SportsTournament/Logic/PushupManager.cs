using Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Logic
{
    public class PushupManager
    {
        private readonly List<User> _users;
        private readonly Dictionary<string, string> _activeTokens;

        public PushupManager(List<User> users, Dictionary<string, string> activeTokens)
        {
            _users = users;
            _activeTokens = activeTokens;
        }

        /// <summary>
        /// Fügt einen Pushup-Eintrag für den Benutzer hinzu.
        /// </summary>
        /// <param name="token">Authentifizierungs-Token</param>
        /// <param name="count">Anzahl der Pushups</param>
        /// <param name="duration">Dauer in Sekunden</param>
        /// <param name="errorMessage">Fehlermeldung (wenn fehlgeschlagen)</param>
        /// <param name="username">Benutzername (wenn erfolgreich)</param>
        /// <returns>true wenn erfolgreich, sonst false</returns>
        public bool AddPushup(string token, int count, int duration, out string errorMessage, out string username)
        {
            errorMessage = "";
            username = "";

            // Token prüfen
            if (!_activeTokens.ContainsKey(token))
            {
                errorMessage = "Token ungültig.";
                return false;
            }

            // Username direkt aus dem Token holen
            var resolvedUsername = _activeTokens[token];
            username = resolvedUsername;

            // Benutzer suchen (nicht im Lambda mit 'out' arbeiten!)
            var user = _users.FirstOrDefault(u => u.Username == resolvedUsername);
            if (user == null)
            {
                errorMessage = "Benutzer nicht gefunden.";
                return false;
            }

            if (count <= 0 || duration <= 0)
            {
                errorMessage = "Ungültige Werte für Pushups oder Dauer.";
                return false;
            }

            user.TotalPushups += count;

            user.History.Add(new PushupEntry
            {
                Count = count,
                DurationInSeconds = duration,
                Timestamp = DateTime.Now
            });

            Console.WriteLine($"💪 {username} hat {count} Pushups gemacht ({duration}s)");

            return true;
        }

    }
}
