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
            "https://sorder1004.daouoffice.com/gw/app/calendar/";

        // 페이지에서 오늘 일정 추출하는 JS
        public static string GetExtractScript()
        {
            return @"(function() {
  try {
    var items = [];
    var seen = {};

    // 다우오피스 캘린더 이벤트 셀렉터 (여러 구조 대응)
    var selectors = [
      '.cal-event-item', '.event-item', '.schedule_item', '.cal_event',
      '.day-event-item', '[data-type=""event""]', '.plan-item',
      '.schedule-event', '.event', '.fc-event'
    ];

    var found = [];
    for (var i = 0; i < selectors.length; i++) {
      var els = document.querySelectorAll(selectors[i]);
      if (els.length > 0) { found = Array.from(els); break; }
    }

    // 못 찾으면 클래스명 기반 휴리스틱 탐색
    if (found.length === 0) {
      var all = document.querySelectorAll('[class]');
      all.forEach(function(el) {
        var cls = (el.className || '').toString();
        if (/event|schedule|plan/i.test(cls) && el.children.length < 8) {
          var t = el.textContent.trim().replace(/\s+/g, ' ');
          if (t.length > 1 && t.length < 120) found.push(el);
        }
      });
    }

    found.forEach(function(el) {
      var title = (el.getAttribute('title') || el.textContent || '')
                    .trim().replace(/\s+/g, ' ');
      if (!title || seen[title]) return;
      seen[title] = true;

      var time = '';
      var timeEl = el.querySelector('time, .time, [class*=""time""], .fc-time');
      if (timeEl) time = timeEl.textContent.trim();

      items.push({ title: title, time: time });
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

                    var si = new ScheduleItem
                    {
                        RawTitle = title,
                        StoreName = ParseStoreName(title),
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

        // "[매장명]..." 또는 "매장명 /" 또는 "매장명 -" 패턴 파싱
        private static string ParseStoreName(string title)
        {
            // [내용] 형식
            var m = Regex.Match(title, @"\[([^\]]+)\]");
            if (m.Success) return m.Groups[1].Value.Trim();

            // 슬래시 앞
            int idx = title.IndexOf('/');
            if (idx > 1) return title.Substring(0, idx).Trim();

            // 대시 앞 (단, 시간 패턴 앞에 오는 대시 제외)
            idx = title.IndexOf(" - ");
            if (idx > 1) return title.Substring(0, idx).Trim();

            // 괄호 앞
            idx = title.IndexOf('(');
            if (idx > 1) return title.Substring(0, idx).Trim();

            // 공백 없는 짧은 텍스트면 그대로
            return title.Length <= 30 ? title : title.Split(' ')[0];
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

