using Models;
using Npgsql;
using System;

namespace Logic
{
    public class DatabaseHelper
    {
        private string _connectionString = "Host=localhost;Port=5432;Username=Alon;Password=test;Database=sportsdb";

        public NpgsqlConnection ConnectToDatabase()
        {
            var connection = new NpgsqlConnection(_connectionString);
            try
            {
                connection.Open();
                Console.WriteLine("Erfolgreich mit der Datenbank verbunden!");
                return connection;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler bei der Verbindung zur Datenbank: {ex.Message}");
                return null;
            }
        }
        public void InsertPushupEntry(NpgsqlConnection connection, string username, int count, int duration)
        {
            var cmd = new NpgsqlCommand("INSERT INTO pushups (username, count, duration, timestamp) VALUES (@u, @c, @d, @t)", connection);
            cmd.Parameters.AddWithValue("@u", username);
            cmd.Parameters.AddWithValue("@c", count);
            cmd.Parameters.AddWithValue("@d", duration);
            cmd.Parameters.AddWithValue("@t", DateTime.UtcNow);
            cmd.ExecuteNonQuery();
        }

        public void UpdateUserProfile(NpgsqlConnection connection, string username, string name, string bio, string image)
        {
            using (var cmd = new NpgsqlCommand("UPDATE users SET name = @name, bio = @bio, image = @image WHERE username = @username", connection))
            {
                cmd.Parameters.AddWithValue("username", username);
                cmd.Parameters.AddWithValue("name", name);
                cmd.Parameters.AddWithValue("bio", bio);
                cmd.Parameters.AddWithValue("image", image);
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateUser(NpgsqlConnection connection, string oldUsername, string newUsername, string newPassword)
        {
            string query = "UPDATE users SET username = @username, password = @password WHERE username = @oldUsername";

            using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("username", newUsername);
            command.Parameters.AddWithValue("password", newPassword);
            command.Parameters.AddWithValue("oldUsername", oldUsername);

            var result = command.ExecuteNonQuery();

            if (result > 0)
            {
                Console.WriteLine($"Benutzer {oldUsername} erfolgreich aktualisiert.");
            }
            else
            {
                Console.WriteLine("Fehler beim Aktualisieren des Benutzers.");
            }
        }

        public User GetUserByUsername(NpgsqlConnection connection, string username)
        {
            using (var cmd = new NpgsqlCommand("SELECT * FROM users WHERE username = @username", connection))
            {
                cmd.Parameters.AddWithValue("username", username);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new User
                        {
                            Id = reader.GetInt32(0),
                            Username = reader.GetString(1),
                            Password = reader.GetString(2)
                        };
                    }
                }
            }
            return null;
        }

        public void InsertUser(NpgsqlConnection connection, string username, string password)
        {
            try
            {
                string insertQuery = "INSERT INTO users (username, password) VALUES (@Username, @Password)";
                using (var cmd = new NpgsqlCommand(insertQuery, connection))
                {
                    cmd.Parameters.AddWithValue("Username", username);
                    cmd.Parameters.AddWithValue("Password", password);
                    cmd.ExecuteNonQuery();
                    Console.WriteLine("Benutzer erfolgreich hinzugefügt.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Einfügen des Benutzers: {ex.Message}");
            }
        }
        public void GetUsers(NpgsqlConnection connection)
        {
            try
            {
                string selectQuery = "SELECT * FROM users";
                using (var cmd = new NpgsqlCommand(selectQuery, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Console.WriteLine($"ID: {reader["id"]}, Username: {reader["username"]}, Password: {reader["password"]}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Abrufen der Daten: {ex.Message}");
            }
        }

        public void CloseConnection(NpgsqlConnection connection)
        {
            connection.Close();
            Console.WriteLine("Verbindung zur Datenbank geschlossen.");
        }

        // Weitere Methoden zum Abrufen/Einfügen von Daten hier hinzufügen
    }
}
