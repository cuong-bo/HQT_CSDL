using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hue_travel.Services.Online
{
    /// <summary>A place the assistant named in an answer (the server only lists places its tools really returned).</summary>
    public sealed class ChatPlaceInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Group { get; set; }
    }

    /// <summary>A route the assistant planned, as the server computed it: the stops in visiting order (the first is the start).</summary>
    public sealed class ChatRouteInfo
    {
        public IReadOnlyList<ChatPlaceInfo> Stops { get; set; }
        public double Meters { get; set; }
        public double Seconds { get; set; }

        /// <summary>"5 điểm · 12,3 km · 35 phút".</summary>
        public string Summary
        {
            get
            {
                int minutes = (int)Math.Round(Seconds / 60);
                string time = minutes >= 60 ? (minutes / 60) + " giờ" + (minutes % 60 > 0 ? " " + (minutes % 60) + " phút" : "") : minutes + " phút";
                return Stops.Count + " điểm · " + (Meters / 1000).ToString("0.0", System.Globalization.CultureInfo.CurrentCulture) + " km · " + time;
            }
        }
    }

    public sealed class ChatAnswer
    {
        public string Reply { get; set; }
        public IReadOnlyList<ChatPlaceInfo> Places { get; set; }
        public ChatRouteInfo Route { get; set; }
        public int? Remaining { get; set; }
    }

    /// <summary>One turn of the conversation, kept in memory only.</summary>
    public sealed class ChatTurn
    {
        public bool IsUser { get; set; }
        public string Text { get; set; }
    }

    /// <summary>Talks to the server's assistant (POST /v1/chat). The key of the model provider is never here: it stays on the server.</summary>
    public sealed class ApiChatService
    {
        public const int KeepTurns = 10;                                         // the server uses at most this many
        private static readonly TimeSpan AnswerTimeout = TimeSpan.FromSeconds(90);   // the model may call tools several times

        private readonly ApiClient api;

        public ApiChatService(ApiClient api)
        {
            this.api = api;
        }

        /// <summary>True when the server has the assistant switched on (a key is set). Any failure counts as "not available".</summary>
        public async Task<bool> IsEnabledAsync(CancellationToken ct)
        {
            ApiResponse r = await api.GetAsync("/v1/chat/status", ct).ConfigureAwait(true);
            return r.IsSuccess && r.Object != null && JsonHelper.Bool(r.Object, "enabled");
        }

        /// <param name="turns">The conversation so far; the last one must be the person's question.</param>
        /// <param name="selectedPoiId">The place open on the map, so "chỗ này" can be understood; null for none.</param>
        public async Task<ApiResult<ChatAnswer>> AskAsync(IList<ChatTurn> turns, string selectedPoiId, CancellationToken ct)
        {
            object[] messages = turns.Skip(Math.Max(0, turns.Count - KeepTurns))
                .Select(t => (object)new Dictionary<string, object> { { "role", t.IsUser ? "user" : "assistant" }, { "content", t.Text } }).ToArray();
            var body = new Dictionary<string, object> { { "messages", messages } };
            if (!string.IsNullOrEmpty(selectedPoiId)) body["context"] = new Dictionary<string, object> { { "selectedPoiId", selectedPoiId } };

            ApiResponse r = await api.PostAsync("/v1/chat", body, AnswerTimeout, ct).ConfigureAwait(true);
            if (!r.IsSuccess) return new ApiResult<ChatAnswer> { Error = r.Error };
            ChatAnswer answer = Parse(r.Object);
            return answer == null
                ? new ApiResult<ChatAnswer> { Error = "Trợ lý trả về câu trả lời không hợp lệ." }
                : new ApiResult<ChatAnswer> { Value = answer };
        }

        /// <summary>Reads the answer of the server; null when it has no text.</summary>
        public static ChatAnswer Parse(IDictionary<string, object> d)
        {
            if (d == null) return null;
            string reply = JsonHelper.Str(d, "reply");
            if (string.IsNullOrWhiteSpace(reply)) return null;
            var answer = new ChatAnswer
            {
                Reply = reply,
                Places = ParsePlaces(JsonHelper.List(d, "places")),
                Remaining = d.ContainsKey("remaining") && d["remaining"] != null ? (int?)(int)JsonHelper.Dbl(d, "remaining") : null,
            };
            IDictionary<string, object> route = JsonHelper.Obj(d, "route");
            if (route != null)
            {
                IReadOnlyList<ChatPlaceInfo> stops = ParsePlaces(JsonHelper.List(route, "stops"));
                if (stops.Count >= 2)
                    answer.Route = new ChatRouteInfo { Stops = stops, Meters = JsonHelper.Dbl(route, "meters"), Seconds = JsonHelper.Dbl(route, "seconds") };
            }
            return answer;
        }

        private static IReadOnlyList<ChatPlaceInfo> ParsePlaces(IList<object> list)
        {
            var places = new List<ChatPlaceInfo>();
            if (list == null) return places;
            foreach (IDictionary<string, object> item in list.OfType<IDictionary<string, object>>())
            {
                string id = JsonHelper.Str(item, "id");
                if (string.IsNullOrEmpty(id)) continue;
                places.Add(new ChatPlaceInfo { Id = id, Name = JsonHelper.Str(item, "name") ?? id, Group = JsonHelper.Str(item, "group") });
            }
            return places;
        }
    }
}
