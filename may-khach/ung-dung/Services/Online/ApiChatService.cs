using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hue_travel.Services.Online
{
    public sealed class ChatTurn
    {
        public ChatTurn(string role, string content)
        {
            Role = role;
            Content = content;
        }

        public string Role { get; }
        public string Content { get; }
    }

    public sealed class ChatPlace
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Group { get; set; }
    }

    public sealed class ChatRoute
    {
        public IReadOnlyList<string> Order { get; set; }
        public double Meters { get; set; }
        public double Seconds { get; set; }
        public IReadOnlyList<ChatPlace> Stops { get; set; }
    }

    public sealed class ChatReply
    {
        public string Reply { get; set; }
        public IReadOnlyList<ChatPlace> Places { get; set; } = new List<ChatPlace>();
        public ChatRoute Route { get; set; }
        public int Remaining { get; set; }
        public string Error { get; set; }
        public bool Ok => Error == null;
    }

    /// <summary>The assistant call: one request, one signed reply. The conversation lives only in the panel's memory.</summary>
    public sealed class ApiChatService
    {
        private readonly ApiClient api;

        public ApiChatService(ApiClient api)
        {
            this.api = api;
        }

        public async Task<ChatReply> AskAsync(IReadOnlyList<ChatTurn> history, string selectedPoiId, CancellationToken ct)
        {
            var messages = new List<object>();
            foreach (ChatTurn turn in history.Skip(Math.Max(0, history.Count - 10)))
                messages.Add(new Dictionary<string, object> { { "role", turn.Role }, { "content", turn.Content } });
            var body = new Dictionary<string, object> { { "messages", messages.ToArray() } };
            if (!string.IsNullOrWhiteSpace(selectedPoiId))
                body["context"] = new Dictionary<string, object> { { "selectedPoiId", selectedPoiId } };

            ApiResponse response = await api.ChatAsync(body, ct).ConfigureAwait(true);
            if (!response.IsSuccess) return new ChatReply { Error = response.Error };

            var reply = new ChatReply
            {
                Reply = JsonHelper.Str(response.Object, "reply") ?? "",
                Remaining = (int)JsonHelper.Long(response.Object, "remaining"),
            };
            IList<object> places = JsonHelper.List(response.Object, "places");
            if (places != null)
                reply.Places = places.OfType<IDictionary<string, object>>().Select(ToPlace).Where(p => p.Id != null).ToList();

            IDictionary<string, object> route = JsonHelper.Obj(response.Object, "route");
            if (route != null)
            {
                IList<object> order = JsonHelper.List(route, "order");
                IList<object> stops = JsonHelper.List(route, "stops");
                reply.Route = new ChatRoute
                {
                    Order = order == null ? new List<string>() : order.Select(o => Convert.ToString(o)).ToList(),
                    Meters = JsonHelper.Dbl(route, "meters"),
                    Seconds = JsonHelper.Dbl(route, "seconds"),
                    Stops = stops == null ? new List<ChatPlace>() : stops.OfType<IDictionary<string, object>>().Select(ToPlace).Where(p => p.Id != null).ToList(),
                };
            }
            return reply;
        }

        private static ChatPlace ToPlace(IDictionary<string, object> d) => new ChatPlace
        {
            Id = JsonHelper.Str(d, "id"),
            Name = JsonHelper.Str(d, "name"),
            Group = JsonHelper.Str(d, "group"),
        };
    }
}
