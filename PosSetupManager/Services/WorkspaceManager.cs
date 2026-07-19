using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using PosSetupManager.Models;

namespace PosSetupManager.Services
{
    public class WorkspaceManager
    {
        private static readonly string SaveDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PosSetupManager", "Workspaces");

        private static readonly string ActiveFile = Path.Combine(SaveDir, "active.json");
        private static readonly string HistoryFile = Path.Combine(SaveDir, "history.json");

        public List<StoreSession> ActiveSessions { get; private set; } = new List<StoreSession>();
        public List<StoreSession> CompletedSessions { get; private set; } = new List<StoreSession>();

        private static readonly JsonSerializerSettings _settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None,
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        };

        public WorkspaceManager()
        {
            Directory.CreateDirectory(SaveDir);
            Load();
        }

        public StoreSession AddSession()
        {
            var session = new StoreSession();
            ActiveSessions.Add(session);
            Save();
            return session;
        }

        public void CompleteSession(string id)
        {
            var session = ActiveSessions.Find(s => s.Id == id);
            if (session == null) return;
            session.Status = "완료";
            session.CompletedAt = DateTime.Now;
            ActiveSessions.Remove(session);
            CompletedSessions.Insert(0, session);
            Save();
        }

        public void RemoveSession(string id)
        {
            ActiveSessions.RemoveAll(s => s.Id == id);
            Save();
        }

        public void Save()
        {
            try
            {
                File.WriteAllText(ActiveFile, JsonConvert.SerializeObject(ActiveSessions, _settings));
                File.WriteAllText(HistoryFile, JsonConvert.SerializeObject(CompletedSessions, _settings));
            }
            catch { }
        }

        private void Load()
        {
            try
            {
                if (File.Exists(ActiveFile))
                    ActiveSessions = JsonConvert.DeserializeObject<List<StoreSession>>(
                        File.ReadAllText(ActiveFile), _settings) ?? new List<StoreSession>();
            }
            catch { ActiveSessions = new List<StoreSession>(); }

            try
            {
                if (File.Exists(HistoryFile))
                    CompletedSessions = JsonConvert.DeserializeObject<List<StoreSession>>(
                        File.ReadAllText(HistoryFile), _settings) ?? new List<StoreSession>();
            }
            catch { CompletedSessions = new List<StoreSession>(); }
        }
    }
}