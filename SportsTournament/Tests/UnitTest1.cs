using NUnit.Framework;
using Logic;
using Models;
using System.Collections.Generic;
using System.Linq;

namespace Tests
{
    public class MainTests
    {
        private List<User> users;
        private Dictionary<string, string> tokens;
        private PushupManager pushupManager;
        private TournamentManager tournamentManager;

        [SetUp]
        public void Setup()
        {
            users = new List<User>
            {
                new User { Username = "a", Password = "pw", TotalPushups = 10 },
                new User { Username = "b", Password = "pw", TotalPushups = 5 }
            };

            tokens = new Dictionary<string, string>
            {
                { "valid", "a" }
            };

            pushupManager = new PushupManager(users, tokens);
            tournamentManager = new TournamentManager(users);
        }

        [Test]
        public void AddPushup_ValidToken_AddsHistory()
        {
            var result = pushupManager.AddPushup("valid", 10, 30, out var err, out var user);
            Assert.IsTrue(result);
            Assert.AreEqual("a", user);
            Assert.AreEqual(1, users[0].History.Count);
        }

        [Test]
        public void AddPushup_IncreasesTotalPushups()
        {
            pushupManager.AddPushup("valid", 15, 20, out _, out _);
            Assert.AreEqual(25, users[0].TotalPushups); // vorher 10
        }

        [Test]
        public void AddPushup_InvalidToken_Fails()
        {
            var result = pushupManager.AddPushup("wrong", 10, 20, out var error, out _);
            Assert.IsFalse(result);
            Assert.AreEqual("Token ungültig.", error);
        }

        [Test]
        public void AddPushup_UnknownUser_Fails()
        {
            tokens["ghost"] = "ghost";
            var result = pushupManager.AddPushup("ghost", 10, 20, out var error, out _);
            Assert.IsFalse(result);
            Assert.AreEqual("Benutzer nicht gefunden.", error);
        }

        [Test]
        public void AddPushup_Twice_AppendsHistory()
        {
            pushupManager.AddPushup("valid", 5, 10, out _, out _);
            pushupManager.AddPushup("valid", 5, 10, out _, out _);
            Assert.AreEqual(2, users[0].History.Count);
        }

        [Test]
        public void RegisterPush_StartsTournament()
        {
            tournamentManager.RegisterPush("a");
            Assert.IsTrue(tournamentManager.IsRunning);
        }

        [Test]
        public void RegisterPush_AddsParticipant()
        {
            tournamentManager.RegisterPush("a");
            var state = tournamentManager.GetState();
            var list = (List<string>)state.GetType().GetProperty("Participants").GetValue(state);
            Assert.Contains("a", list);
        }

        [Test]
        public void GetState_ReturnsCorrectLeader()
        {
            tournamentManager.RegisterPush("a");
            tournamentManager.RegisterPush("b");
            var state = tournamentManager.GetState();
            var leader = state.GetType().GetProperty("Leader").GetValue(state);
            Assert.AreEqual("a", leader);
        }

        [Test]
        public void EndTournament_UpdatesEloCorrectly()
        {
            tournamentManager.RegisterPush("a");
            tournamentManager.RegisterPush("b");
            typeof(TournamentManager).GetMethod("EndTournament", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(tournamentManager, null);

            Assert.AreEqual(110, users[0].Elo); // Gewinner
            Assert.AreEqual(95, users[1].Elo);  // Verlierer
        }

        [Test]
        public void NewUser_HasDefaultElo100()
        {
            var newUser = new User { Username = "newbie", Password = "pw" };
            Assert.AreEqual(100, newUser.Elo);
        }

    }
}
