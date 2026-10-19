using System.Text.RegularExpressions;
using MongoDB.Bson;

namespace Clinix
{
    internal static class MongoHelper
    {
        public static BsonRegularExpression Like(string s)
        {
            return new BsonRegularExpression(Regex.Escape(s ?? ""), "i");
        }
    }
}