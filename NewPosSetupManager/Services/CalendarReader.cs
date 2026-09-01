using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using NewPosSetupManager.Models;

namespace NewPosSetupManager.Services
{
    public static class CalendarReader
    {
        public static readonly string BaseUrl =
            "https://sorder1004.daouoffice.com/gw/app/calendar";

        public static string TodayAgendaUrl()
        {
            return BaseUrl + "/agenda/" + DateTime.Today.ToString("yyyy-MM-dd");
        }

        // 페이지에서 오늘 일정 추출하는 JS
        public static string GetExtractScript()
        {
            return @"(function() {
  try {
    var items = [];
    var seen = {};
    var urlMatch = window.location.href.match(/\/agenda\/\d{4}-(\d{2})-(\d{2})/);
    var targetDate = urlMatch ? (urlMatch[1] + '.' + urlMatch[2]) : null;
    var table = document.querySelector('table.tb_agenda, table[class*=""tb_agenda""]');
    if (!table) return JSON.stringify({ ok: false, error: '오늘 일정 테이블을 찾지 못했습니다.', items: [] });

    var currentDate = null;
    table.querySelectorAll('tbody tr').forEach(function(row) {
      var th = row.querySelector('th');
      if (th) {
        currentDate = (th.innerText || th.textContent || '')
          .trim().replace(/\([^)]*\)/g, '').trim();
      }
      if (targetDate && currentDate !== targetDate) return;

      var link = row.querySelector('.event_link');
      var nameCell = row.querySelector('td.align_l');
      if (!link || !nameCell) return;
      var title = (nameCell.innerText || nameCell.textContent || '')
        .trim().replace(/\s+/g, ' ');
      if (!title || title.length > 200 || seen[title]) return;
      seen[title] = true;

      var timeEl = row.querySelector('time, .time, [class*=""time""]');
      items.push({ title: title, time: timeEl ? timeEl.textContent.trim() : '' });
    });

    return JSON.stringify({ ok: true, items: items, count: items.length });
  } catch (e) {
    return JSON.stringify({ ok: false, error: e.message, items: [] });
  }
})()";
        }

        // 진단용: 페이지 내 이벤트 관련 클래스 탐색
        public static string GetDiagScript()
        {
            return @"(function() {
  var counts = {};
  document.querySelectorAll('[class]').forEach(function(el) {
    var cls = (el.className || '').toString().split(' ');
    cls.forEach(function(c) {
      if (c && /event|schedule|plan|cal/i.test(c)) {
        counts[c] = (counts[c] || 0) + 1;
      }
    });
  });
  var top = Object.entries(counts)
    .sort(function(a,b){ return b[1]-a[1]; })
    .slice(0, 20)
    .map(function(e){ return e[0] + ': ' + e[1]; });
  return top.join('\n') || '관련 클래스 없음';
})()";
        }

        public static List<ScheduleItem> ParseExtracted(string json, DateTime targetDate)
        {
            var result = new List<ScheduleItem>();
            try
            {
                var obj = JObject.Parse(json);
                if (!(bool)obj["ok"]) return result;
                var arr = (JArray)obj["items"];
                if (arr == null) return result;

                foreach (var item in arr)
                {
                    string title = ((string)item["title"] ?? "").Trim();
                    string time = ((string)item["time"] ?? "").Trim();
                    if (string.IsNullOrEmpty(title)) continue;
                    bool isPrepaid = Regex.IsMatch(title, @"^\s*\[선불\]");
                    string storeName = ParseStoreName(title);
                    if (string.IsNullOrWhiteSpace(storeName)) continue;

                    var si = new ScheduleItem
                    {
                        RawTitle = title,
                        StoreName = storeName,
                        TableMode = isPrepaid ? "선불" : "후불",
                        InstallDate = targetDate,
                        InstallTime = ParseTime(time.Length > 0 ? time : title),
                        RemoteManager = ParseRemoteManager(title),
                        EngineerContact = ParseContact(title),
                    };
                    result.Add(si);
                }
            }
            catch { }
            return result;
        }

        // agenda 제목: "[선불]매장명(담당자)", "[필드]매장명(담당자)", "매장명(담당자)"
        private static string ParseStoreName(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return "";

            // [선불], [필드] 등 일정 분류 태그는 매장명이 아니다.
            string cleaned = Regex.Replace(title.Trim(), @"^\s*(?:\[[^\]]+\]\s*)+", "");

            // 마지막 괄호는 담당자 표기: (조해찬)
            cleaned = Regex.Replace(cleaned, @"\s*\([^()]*\)\s*$", "").Trim();
            if (string.IsNullOrEmpty(cleaned)) return "";

            // 슬래시 앞
            int idx = cleaned.IndexOf('/');
            if (idx > 1) return cleaned.Substring(0, idx).Trim();

            // 대시 앞 (단, 시간 패턴 앞에 오는 대시 제외)
            idx = cleaned.IndexOf(" - ");
            if (idx > 1) return cleaned.Substring(0, idx).Trim();

            return cleaned;
        }

        // "원격: 이름" 또는 "원격담당: 이름" 패턴
        private static string ParseRemoteManager(string title)
        {
            var m = Regex.Match(title, @"원격[\s:：]*([가-힣a-zA-Z]{2,6})");
            return m.Success ? m.Groups[1].Value.Trim() : "";
        }

        // 전화번호 패턴
        private static string ParseContact(string title)
        {
            var m = Regex.Match(title, @"0\d{1,2}[-\s]?\d{3,4}[-\s]?\d{4}");
            return m.Success ? m.Value.Trim() : "";
        }

        // HH:mm 패턴
        private static string ParseTime(string s)
        {
            var m = Regex.Match(s, @"\b(\d{1,2}):(\d{2})\b");
            if (!m.Success) return "";
            int h = int.Parse(m.Groups[1].Value);
            int min = int.Parse(m.Groups[2].Value);
            if (h < 0 || h > 23 || min < 0 || min > 59) return "";
            return string.Format("{0:D2}:{1:D2}", h, min);
        }
    }
}

