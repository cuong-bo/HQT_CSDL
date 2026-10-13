using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Script.Serialization;

namespace Hue_travel.Services.Online
{
    /// <summary>Small helpers over JavaScriptSerializer: parse to dictionaries / lists and read typed values safely.</summary>
    public static class JsonHelper
    {
        private static JavaScriptSerializer Create() => new JavaScriptSerializer { MaxJsonLength = int.MaxValue, RecursionLimit = 64 };

        /// <summary>Dictionary, list or primitive; null when the text is empty or not JSON.</summary>
        public static object Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                return Create().DeserializeObject(json);
            }
            catch (ArgumentException)
            {
                return null;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        public static string Serialize(object value) => Create().Serialize(value);

        public static string Str(IDictionary<string, object> d, string key)
        {
            object v;
            return d != null && d.TryGetValue(key, out v) && v != null ? Convert.ToString(v, CultureInfo.InvariantCulture) : null;
        }

        public static double Dbl(IDictionary<string, object> d, string key)
        {
            object v;
            if (d == null || !d.TryGetValue(key, out v) || v == null) return 0;
            try
            {
                return Convert.ToDouble(v, CultureInfo.InvariantCulture);
            }
            catch (FormatException)
            {
                return 0;
            }
            catch (InvalidCastException)
            {
                return 0;
            }
        }

        public static long Long(IDictionary<string, object> d, string key) => (long)Dbl(d, key);

        public static bool Bool(IDictionary<string, object> d, string key)
        {
            object v;
            return d != null && d.TryGetValue(key, out v) && v is bool && (bool)v;
        }

        public static IDictionary<string, object> Obj(IDictionary<string, object> d, string key)
        {
            object v;
            return d != null && d.TryGetValue(key, out v) ? v as IDictionary<string, object> : null;
        }

        public static IList<object> List(IDictionary<string, object> d, string key)
        {
            object v;
            return d != null && d.TryGetValue(key, out v) ? AsList(v) : null;
        }

        public static IList<object> AsList(object value)
        {
            IEnumerable sequence = value as IEnumerable;
            if (sequence == null || value is string || value is IDictionary<string, object>) return null;
            return sequence.Cast<object>().ToList();
        }
    }
}
