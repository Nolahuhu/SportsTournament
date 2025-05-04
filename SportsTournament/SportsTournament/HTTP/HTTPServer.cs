using System.Net.Sockets;
using System.Net;
using System.Text;
using HTTP;
using System.Text.Json;
using Models;
using Logic;

namespace Server
{
    public class HTTPServer
    {
        private PushupManager _pushupManager;
        private TournamentManager _tournamentManager;
        private readonly TcpListener _listener;
        private Dictionary<string, string> activeTokens = new(); // Token → Username
        private List<User> users = new();
        public HTTPServer(int port = 10001)
        {
            _listener = new TcpListener(IPAddress.Any, port);
        }

        public void Start()
        {
            _listener.Start();
            Console.WriteLine("Server läuft auf http://localhost:10001");
            _pushupManager = new PushupManager(users, activeTokens);
            _tournamentManager = new TournamentManager(users);


            while (true)
            {
                var client = _listener.AcceptTcpClient();
                var stream = client.GetStream();

                using var reader = new StreamReader(stream);
                using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

                // Lese Request
                var buffer = new char[1024 * 4];
                int read = reader.Read(buffer, 0, buffer.Length);
                if (read == 0) continue;

                var requestData = new string(buffer, 0, read);
                var request = new HTTPRequest().Parse(requestData);

                // Route verarbeiten

                var response = ProcessRequest(request);

                // Sende Response zurück
                var responseBytes = response.GetBytes();
                writer.Write(responseBytes);
                client.Close();
            }
        }

        private HTTPResponse ProcessRequest(HTTPRequest request)
        {
            var method = request.Method.ToUpper();
            var path = request.Path.ToLower().TrimEnd('/');

            Console.WriteLine($"␦ Request: {method} {path}");

            // Registrierung: POST /users
            if (method == "POST" && path == "/users")
                return HandleRegister(request);

            // Login: POST /sessions
            if (method == "POST" && path == "/sessions")
                return HandleLogin(request);

            // Profil ändern: PUT /users/{username}
            if (method == "PUT" && path.StartsWith("/users/"))
                return HandleEditUser(request);

            // Nutzer abfragen: GET /users/{username}
            if (method == "GET" && path.StartsWith("/users/"))
                return HandleUserStats(request);

            // Gesamtstatistik: GET /stats
            if (method == "GET" && path == "/stats")
                return HandleGlobalStats(request);

            // Scoreboard: GET /score
            if (method == "GET" && path == "/score")
                return HandleScoreboard(request);

            // Pushup-History abfragen: GET /history
            if (method == "GET" && path == "/history")
                return HandleGetPushups(request);

            // Pushup-History hinzufügen: POST /history
            if (method == "POST" && path == "/history")
                return HandleAddPushup(request);

            // Turnierstatus: GET /tournament
            if (method == "GET" && path == "/tournament")
                return HandleTournamentState(request);

            return HTTPResponse.NotFound("Diese Route existiert nicht.");
        }




        private HTTPResponse HandleRegister(HTTPRequest request)
        {
            try
            {
                var userDTO = JsonSerializer.Deserialize<UserRegisterDTO>(request.Body);

                // Überprüfen, ob der Benutzername oder Passwort leer sind
                if (string.IsNullOrWhiteSpace(userDTO.Username) || string.IsNullOrWhiteSpace(userDTO.Password))
                    return HTTPResponse.BadRequest("Benutzername und Passwort erforderlich.");

                // Verbindung zur Datenbank herstellen
                var dbHelper = new DatabaseHelper();
                var connection = dbHelper.ConnectToDatabase();

                // Überprüfen, ob der Benutzer bereits existiert
                var existingUser = dbHelper.GetUserByUsername(connection, userDTO.Username);

                // Wenn der Benutzer existiert, eine Fehlermeldung zurückgeben
                if (existingUser != null)
                {
                    dbHelper.CloseConnection(connection);
                    return HTTPResponse.BadRequest("Benutzer existiert bereits.");
                }

                // Benutzer in die Datenbank einfügen
                dbHelper.InsertUser(connection, userDTO.Username, userDTO.Password);
                dbHelper.CloseConnection(connection);

                return HTTPResponse.Created("{ \"message\": \"Benutzer erfolgreich registriert.\" }");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler bei der Registrierung: {ex.Message}");
                return HTTPResponse.Error("Fehler bei der Registrierung.");
            }
        }



        public HTTPResponse HandleLogin(HTTPRequest request)
        {
            try
            {
                var userDTO = JsonSerializer.Deserialize<UserRegisterDTO>(request.Body);

                if (string.IsNullOrWhiteSpace(userDTO.Username) || string.IsNullOrWhiteSpace(userDTO.Password))
                    return HTTPResponse.BadRequest("Benutzername und Passwort erforderlich.");

                // Verbindung zur Datenbank herstellen
                var dbHelper = new DatabaseHelper();
                var connection = dbHelper.ConnectToDatabase();

                // Benutzer aus der Datenbank holen
                var user = dbHelper.GetUserByUsername(connection, userDTO.Username);

                if (user == null || user.Password != userDTO.Password)
                {
                    Console.WriteLine($"Login fehlgeschlagen: Benutzername {userDTO.Username} nicht gefunden oder Passwort falsch.");
                    dbHelper.CloseConnection(connection);
                    return HTTPResponse.BadRequest("Benutzername oder Passwort falsch.");
                }

                // Token generieren (hier so, dass es mit dem Curl-Skript kompatibel ist)
                var token = $"{userDTO.Username}-sebToken";
                activeTokens[token] = userDTO.Username;

                Console.WriteLine($"Token für Benutzer {userDTO.Username} gespeichert: {token}");

                // Benutzer zur internen Liste hinzufügen, falls noch nicht vorhanden
                if (!users.Any(u => u.Username == user.Username))
                {
                    users.Add(user);
                    Console.WriteLine($"Benutzer {user.Username} zur internen users-Liste hinzugefügt.");
                }

                dbHelper.CloseConnection(connection);

                // Token im Response zurückgeben
                return HTTPResponse.Ok($"{{ \"message\": \"Login erfolgreich\", \"token\": \"{token}\" }}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler bei der Anmeldung: {ex.Message}");
                return HTTPResponse.Error($"Fehler bei der Anmeldung: {ex.Message}");
            }
        }








        private HTTPResponse HandleEditUser(HTTPRequest request)
        {
            // Überprüfen, ob der Token im Header vorhanden ist
            if (!request.Headers.TryGetValue("Authorization", out var token))
            {
                Console.WriteLine("Token fehlt im Header");
                return HTTPResponse.Unauthorized("Token fehlt.");
            }

            // Unterstützt sowohl "Bearer <token>" als auch "Basic <token>"
            if (token.StartsWith("Bearer "))
                token = token.Substring("Bearer ".Length).Trim();
            else if (token.StartsWith("Basic "))
                token = token.Substring("Basic ".Length).Trim();
            else
            {
                Console.WriteLine("Ungültiger Authorization-Header");
                return HTTPResponse.Unauthorized("Ungültiger Authorization-Header.");
            }

            Console.WriteLine($"Token aus Header extrahiert: {token}");

            // Token gültig?
            if (!activeTokens.ContainsKey(token))
            {
                Console.WriteLine($"Ungültiger Token: {token}");
                return HTTPResponse.Unauthorized("Ungültiger Token.");
            }

            // Wer ist eingeloggt?
            var usernameFromToken = activeTokens[token];
            Console.WriteLine($"Token gehört zu Benutzer: {usernameFromToken}");

            // Aus dem Pfad den Benutzernamen extrahieren, der geändert werden soll
            var pathParts = request.Path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (pathParts.Length < 2)
            {
                Console.WriteLine("Ungültiger Pfad");
                return HTTPResponse.BadRequest("Ungültiger Pfad.");
            }

            var requestedUser = pathParts[1];

            // Nur der eigene Benutzer darf bearbeitet werden
            if (requestedUser != usernameFromToken)
            {
                Console.WriteLine($"Benutzer {requestedUser} versucht, Daten eines anderen Benutzers zu bearbeiten.");
                return HTTPResponse.Unauthorized("Nicht dein Account.");
            }

            // Optional: Body analysieren (z. B. Name, Bio oder Image ändern)
            try
            {
                var userDTO = JsonSerializer.Deserialize<UserEditDTO>(request.Body);

                if (userDTO == null || string.IsNullOrWhiteSpace(userDTO.Name) || string.IsNullOrWhiteSpace(userDTO.Bio))
                    return HTTPResponse.BadRequest("Ungültige Daten.");

                // Verbindung zur Datenbank herstellen
                var dbHelper = new DatabaseHelper();
                var connection = dbHelper.ConnectToDatabase();

                // Benutzer in der Datenbank aktualisieren
                dbHelper.UpdateUserProfile(connection, requestedUser, userDTO.Name, userDTO.Bio, userDTO.Image);
                dbHelper.CloseConnection(connection);

                Console.WriteLine($"Benutzer {requestedUser} hat seine Daten erfolgreich aktualisiert.");

                return HTTPResponse.Ok("{\"message\": \"Benutzerdaten erfolgreich geändert.\"}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler: {ex.Message}");
                return HTTPResponse.Error("Fehler bei der Aktualisierung der Benutzerdaten.");
            }
        }






        private HTTPResponse HandleGetPushups(HTTPRequest request)
{
    // 🔐 Token aus dem Header lesen
    if (!request.Headers.TryGetValue("Authorization", out var rawAuth))
        return HTTPResponse.Unauthorized("Token fehlt.");

    // "Basic xyz" → Token extrahieren
    var token = rawAuth.StartsWith("Basic ") ? rawAuth.Substring(6).Trim() : rawAuth;

    // Username über Token herausfinden
    if (!activeTokens.TryGetValue(token, out var username))
        return HTTPResponse.Unauthorized("Token ungültig.");

    // Benutzer in der users-Liste finden
    var user = users.FirstOrDefault(u => u.Username == username);
    if (user == null)
        return HTTPResponse.BadRequest("Benutzer nicht gefunden.");

    // 🔁 PushupEntry in PushupEntryDTO umwandeln
    var dtoList = user.History.Select(entry => new PushupEntryDTO
    {
        Count = entry.Count,
        DurationInSeconds = entry.DurationInSeconds
    }).ToList();

    // In JSON umwandeln und zurückgeben
    var json = JsonSerializer.Serialize(dtoList);
    return HTTPResponse.Ok(json);
}



        private HTTPResponse HandleAddPushup(HTTPRequest request)
        {
            // Token prüfen
            if (!request.Headers.TryGetValue("Authorization", out var rawAuth))
                return HTTPResponse.Unauthorized("Token fehlt.");

            var token = rawAuth.StartsWith("Basic ") ? rawAuth.Substring(6).Trim() : rawAuth;

            if (!activeTokens.TryGetValue(token, out var username))
                return HTTPResponse.Unauthorized("Ungültiger Token.");

            // Benutzer finden
            var user = users.FirstOrDefault(u => u.Username == username);
            if (user == null)
                return HTTPResponse.BadRequest("Benutzer nicht gefunden.");

            // Pushup-Daten lesen
            var data = JsonSerializer.Deserialize<PushupEntryDTO>(request.Body);
            if (data == null || data.Count <= 0 || data.DurationInSeconds <= 0)
                return HTTPResponse.BadRequest("Ungültige Pushup-Daten.");

            // Eintrag in Datenbank speichern
            try
            {
                var db = new DatabaseHelper();
                var conn = db.ConnectToDatabase();
                db.InsertPushupEntry(conn, username, data.Count, data.DurationInSeconds);
                db.CloseConnection(conn);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim DB-Speichern: {ex.Message}");
                return HTTPResponse.Error("Fehler beim Speichern in der Datenbank.");
            }

            // Statistik im RAM aktualisieren (optional)
            var eintrag = new PushupEntry
            {
                Count = data.Count,
                DurationInSeconds = data.DurationInSeconds,
                Timestamp = DateTime.Now
            };
            user.History.Add(eintrag);
            user.TotalPushups += data.Count;

            // Turnierregistrierung
            _tournamentManager.RegisterPush(username);

            return HTTPResponse.Ok("{\"message\": \"Pushup gespeichert und in DB eingetragen.\"}");
        }





        private HTTPResponse HandleStartTournament(HTTPRequest request)
        {
            return HTTPResponse.Ok("{\"message\": \"Turnier gestartet (Platzhalter)\"}");
        }

        private HTTPResponse HandleScoreboard(HTTPRequest request)
        {
            var scoreboard = users
                .OrderByDescending(u => u.Elo)
                .ThenByDescending(u => u.TotalPushups)
                .Select(u => new
                {
                    Username = u.Username,
                    Elo = u.Elo,
                    TotalPushups = u.TotalPushups
                })
                .ToList();

            var json = JsonSerializer.Serialize(scoreboard);
            return HTTPResponse.Ok(json);
        }

        private HTTPResponse HandleTournamentState(HTTPRequest request)
        {
            var json = JsonSerializer.Serialize(_tournamentManager.GetState());
            return HTTPResponse.Ok(json);
        }


        private HTTPResponse HandleGlobalStats(HTTPRequest request)
        {
            if (!request.Headers.TryGetValue("Authorization", out var rawAuth))
                return HTTPResponse.Unauthorized("Token fehlt.");

            var token = rawAuth.StartsWith("Basic ") ? rawAuth.Substring(6).Trim() : rawAuth;

            if (!activeTokens.TryGetValue(token, out var username))
                return HTTPResponse.Unauthorized("Token ungültig.");

            var user = users.FirstOrDefault(u => u.Username == username);
            if (user == null)
                return HTTPResponse.BadRequest("Benutzer nicht gefunden.");

            var result = new
            {
                Username = user.Username,
                Elo = user.Elo,
                TotalPushups = user.TotalPushups
            };

            var json = JsonSerializer.Serialize(result);
            return HTTPResponse.Ok(json);
        }


        private HTTPResponse HandleUserStats(HTTPRequest request)
        {
            return HTTPResponse.Ok("{\"message\": \"User-Statistik geladen (Platzhalter)\"}");
        }

    }
}
