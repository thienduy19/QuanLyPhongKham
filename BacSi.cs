using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Clinix
{
    [BsonIgnoreExtraElements]
    public class BacSi
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("ma_bac_si")]
        public string MaBacSi { get; set; }

        [BsonElement("ten_bac_si")]
        public string TenBacSi { get; set; }

        [BsonElement("chuyen_khoa")]
        public string ChuyenKhoa { get; set; }

        [BsonElement("sdt")]
        public string SDT { get; set; }
    }
}