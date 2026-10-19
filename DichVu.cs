using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Clinix
{
    [BsonIgnoreExtraElements] // Thêm dòng này để bỏ qua các cột rác/cũ trong DB không bị crash
    public class DichVu
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("ma_dich_vu")] 
        public string MaDichVu { get; set; }

        [BsonElement("ten_dich_vu")] 
        public string TenDichVu { get; set; }

        [BsonElement("don_gia")]
        public decimal DonGia { get; set; }

        [BsonElement("Loai")]
        public string Loai { get; set; }
    }
}