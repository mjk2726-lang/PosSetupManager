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

        private static readonly string ActiveTemp = Path.Combine(SaveDir, "active_temp.json");
        private static readonly string HistoryTemp = Path.Combine(SaveDir, "history_temp.json");

        public List<StoreSession> ActiveSessions { get; private set; } = new List<StoreSession>();
        public List<StoreSession> CompletedSessions { get; private set; } = new List<StoreSession>();

        private static readonly JsonSerializerSettings _settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None,
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        public WorkspaceManager()
        {
            Directory.CreateDirectory(SaveDir);
            // 시작 시 잔류 임시 파일 정리
            CleanupTempFiles();
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
                // 1. 임시 파일에 먼저 저장
                var activeJson = JsonConvert.SerializeObject(ActiveSessions, _settings);
                var historyJson = JsonConvert.SerializeObject(CompletedSessions, _settings);

                File.WriteAllText(ActiveTemp, activeJson);
                File.WriteAllText(HistoryTemp, historyJson);

                // 2. 저장된 임시 파일 검증 (역직렬화 성공 여부 확인)
                JsonConvert.DeserializeObject<List<StoreSession>>(File.ReadAllText(ActiveTemp), _settings);
                JsonConvert.DeserializeObject<List<StoreSession>>(File.ReadAllText(HistoryTemp), _settings);

                // 3. 검증 통과 시 원자적 교체
                AtomicReplace(ActiveTemp, ActiveFile);
                AtomicReplace(HistoryTemp, HistoryFile);
            }
            catch
            {
                // 저장 실패 시 임시 파일 정리 (기존 파일은 그대로 유지)
                CleanupTempFiles();
            }
        }

        private void AtomicReplace(string tempFile, string targetFile)
        {
            if (!File.Exists(tempFile)) return;

            if (File.Exists(targetFile))
            {
                // 기존 파일을 .bak으로 백업 후 교체
                string bakFile = targetFile + ".bak";
                File.Replace(tempFile, targetFile, bakFile);
                // .bak 파일 삭제 (교체 성공 확인 후)
                try { File.Delete(bakFile); } catch { }
            }
            else
            {
                File.Move(tempFile, targetFile);
            }
        }

        private void CleanupTempFiles()
        {
            try { if (File.Exists(ActiveTemp)) File.Delete(ActiveTemp); } catch { }
            try { if (File.Exists(HistoryTemp)) File.Delete(HistoryTemp); } catch { }
        }

        private void Load()
        {
            ActiveSessions = SafeLoad(ActiveFile) ?? new List<StoreSession>();
            CompletedSessions = SafeLoad(HistoryFile) ?? new List<StoreSession>();
        }

        private List<StoreSession> SafeLoad(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return null;
                var json = File.ReadAllText(filePath);
                if (string.IsNullOrWhiteSpace(json)) return null;
                return JsonConvert.DeserializeObject<List<StoreSession>>(json, _settings);
            }
            catch
            {
                // 메인 파일 실패 시 .bak 파일로 복구 시도
                string bakFile = filePath + ".bak";
                try
                {
                    if (!File.Exists(bakFile)) return null;
                    var json = File.ReadAllText(bakFile);
                    return JsonConvert.DeserializeObject<List<StoreSession>>(json, _settings);
                }
                catch { return null; }
            }
        }
    }
}