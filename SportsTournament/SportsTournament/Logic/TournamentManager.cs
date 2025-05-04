using Models;

namespace Logic
{
    public class TournamentManager
    {
        public bool IsRunning { get; private set; } = false;
        public DateTime? StartedAt { get; private set; }
        private HashSet<string> participants = new();
        private readonly List<User> users;
        private string state = "none"; // "none", "running", "ended"
        private string lastLeader = null;

        public TournamentManager(List<User> users)
        {
            this.users = users;
        }

        public void RegisterPush(string username)
        {
            if (!IsRunning)
            {
                IsRunning = true;
                StartedAt = DateTime.UtcNow;
                state = "running";
            }

            participants.Add(username);
        }

        public object GetState()
        {
            CheckIfShouldEnd();

            if (!IsRunning && state != "ended")
            {
                return new { Message = "Kein Turnier aktiv." };
            }

            var leader = users
                .Where(u => participants.Contains(u.Username))
                .OrderByDescending(u => u.TotalPushups)
                .FirstOrDefault()?.Username;

            if (leader != null)
                lastLeader = leader;

            return new
            {
                StartedAt = StartedAt,
                Participants = participants.ToList(),
                Leader = lastLeader,
                State = state
            };
        }

        private void CheckIfShouldEnd()
        {
            if (!IsRunning || StartedAt == null)
                return;

            var duration = DateTime.UtcNow - StartedAt.Value;

            if (duration.TotalSeconds >= 120)
            {
                EndTournament();
            }
        }

        private void EndTournament()
        {
            Console.WriteLine("🏁 Turnier ist vorbei – Elo wird aktualisiert!");

            var sorted = users
                .Where(u => participants.Contains(u.Username))
                .OrderByDescending(u => u.TotalPushups)
                .ToList();

            for (int i = 0; i < sorted.Count; i++)
            {
                if (i == 0)
                {
                    sorted[i].Elo += 10; // Gewinner
                }
                else
                {
                    sorted[i].Elo = Math.Max(50, sorted[i].Elo - 5); // Verlierer
                }
            }

            // Status aktualisieren
            IsRunning = false;
            StartedAt = null;
            state = "ended";
            participants.Clear();
        }
    }
}
