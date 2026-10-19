using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Clinix
{
    [BsonIgnoreExtraElements]
    public class BenhNhan
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("ma_benh_nhan")]
        public string MaBenhNhan { get; set; }

        [BsonElement("ten_benh_nhan")]
        public string TenBenhNhan { get; set; }

        [BsonElement("ngay_sinh")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime NgaySinh { get; set; }

        [BsonElement("gioi_tinh")]
        public string GioiTinh { get; set; }

        [BsonElement("sdt")]
        public string SDT { get; set; }

        [BsonElement("dia_chi")]
        public string DiaChi { get; set; }
    }
}