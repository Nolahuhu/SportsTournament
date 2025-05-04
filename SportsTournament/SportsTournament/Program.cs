using Server;
using Logic; // Stelle sicher, dass du die Klasse DatabaseHelper hier einbindest.

class Program
{
    static void Main(string[] args)
    {
        // Datenbankverbindung testen
        var dbHelper = new DatabaseHelper();
        var connection = dbHelper.ConnectToDatabase();

        if (connection != null)
        {
            // Teste, ob Daten hinzugefügt oder abgerufen werden können
            dbHelper.InsertUser(connection, "newuser", "newpassword123"); // Hier kannst du Benutzerdaten einfügen
            dbHelper.GetUsers(connection); // Hier kannst du Benutzer abrufen
            dbHelper.CloseConnection(connection); // Schließe die Verbindung nach der Nutzung
        }

        // HTTP Server starten
        var server = new HTTPServer(10001);
        server.Start();
    }
}
