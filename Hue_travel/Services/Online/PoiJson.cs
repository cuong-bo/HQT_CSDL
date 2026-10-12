using System.Collections.Generic;
using Hue_travel.Models;

namespace Hue_travel.Services.Online
{
    /// <summary>Converts between the API's place JSON and the app's <see cref="Poi"/>.</summary>
    public static class PoiJson
    {
        public static PoiCategory ToCategory(string group)
        {
            switch (group)
            {
                case "scenery": return PoiCategory.Scenery;
                case "food": return PoiCategory.Food;
                case "stay": return PoiCategory.Stay;
                case "service": return PoiCategory.Service;
                case "transport": return PoiCategory.Transport;
                default: return PoiCategory.Heritage;
            }
        }

        public static string ToGroup(PoiCategory category)
        {
            switch (category)
            {
                case PoiCategory.Scenery: return "scenery";
                case PoiCategory.Food: return "food";
                case PoiCategory.Stay: return "stay";
                case PoiCategory.Service: return "service";
                case PoiCategory.Transport: return "transport";
                default: return "heritage";
            }
        }

        public static Poi ToPoi(IDictionary<string, object> d)
        {
            string summary = JsonHelper.Str(d, "summary") ?? string.Empty;
            return new Poi
            {
                Id = JsonHelper.Str(d, "id") ?? string.Empty,
                Name = JsonHelper.Str(d, "name") ?? string.Empty,
                Category = ToCategory(JsonHelper.Str(d, "group")),
                TypeLabel = JsonHelper.Str(d, "type") ?? string.Empty,
                Address = JsonHelper.Str(d, "address") ?? string.Empty,
                OpeningHours = JsonHelper.Str(d, "openingHours") ?? string.Empty,
                TicketPrice = JsonHelper.Str(d, "priceText") ?? string.Empty,
                Phone = JsonHelper.Str(d, "phone") ?? string.Empty,
                Website = JsonHelper.Str(d, "website") ?? string.Empty,
                Summary = summary,
                Description = summary,
                Longitude = JsonHelper.Dbl(d, "lon"),
                Latitude = JsonHelper.Dbl(d, "lat"),
                X = JsonHelper.Dbl(d, "x"),
                Y = JsonHelper.Dbl(d, "y"),
            };
        }

        public static Dictionary<string, object> ToDictionary(Poi p)
        {
            return new Dictionary<string, object>
            {
                { "id", p.Id }, { "group", ToGroup(p.Category) }, { "type", p.TypeLabel }, { "name", p.Name }, { "address", p.Address },
                { "openingHours", p.OpeningHours }, { "priceText", p.TicketPrice }, { "phone", p.Phone }, { "website", p.Website },
                { "summary", p.Summary }, { "lon", p.Longitude }, { "lat", p.Latitude }, { "x", p.X }, { "y", p.Y },
            };
        }
    }
}
