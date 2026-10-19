using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Clinix
{
    [BsonIgnoreExtraElements]
    public class HoSoBenhAn
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } // Khóa chính _id tự động sinh

        [BsonElement("ma_benh_nhan")]
        public string MaBenhNhan { get; set; }

        [BsonElement("ten_benh_nhan")]
        public string TenBenhNhan { get; set; }

        [BsonElement("ma_bac_si")]
        public string MaBacSi { get; set; }

        [BsonElement("so_thu_tu")]
        public int SoThuTu { get; set; }

        [BsonElement("ma_phieu")]
        public string MaPhieu { get; set; }

        [BsonElement("ngay_kham")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime NgayKham { get; set; }

        [BsonElement("trang_thai")]
        public string TrangThai { get; set; }

        [BsonElement("ly_do_kham")]
        public string LyDoKham { get; set; }

        [BsonElement("chan_doan")]
        public string ChanDoan { get; set; }

        [BsonElement("don_thuoc")]
        public List<ChiTietThuoc> DonThuoc { get; set; } = new List<ChiTietThuoc>();

        [BsonElement("dich_vu_chi_dinh")]
        public List<ChiTietDichVu> DichVuChiDinh { get; set; } = new List<ChiTietDichVu>();

        [BsonElement("thanh_toan")]
        public ThongTinThanhToan ThanhToan { get; set; }
    }

    // 1. Class cho mảng Đơn thuốc 
    [BsonIgnoreExtraElements]
    public class ChiTietThuoc
    {
        [BsonElement("ma_thuoc")]
        public string MaThuoc { get; set; }

        [BsonElement("ten_thuoc")]
        public string TenThuoc { get; set; }

        [BsonElement("don_gia")]
        public decimal DonGia { get; set; }

        [BsonElement("so_luong")]
        public int SoLuong { get; set; }

        [BsonElement("cach_dung")]
        public string CachDung { get; set; }
    }

    // 2. Class cho mảng Dịch vụ chỉ định
    [BsonIgnoreExtraElements]
    public class ChiTietDichVu
    {
        [BsonElement("ma_dich_vu")]
        public string MaDichVu { get; set; }

        [BsonElement("ten_dich_vu")]
        public string TenDichVu { get; set; }

        [BsonElement("don_gia")]
        public decimal DonGia { get; set; }

        [BsonElement("so_luong")]
        public int SoLuong { get; set; }
    }

    // 3. Class cho đối tượng Thanh toán
    [BsonIgnoreExtraElements]
    public class ThongTinThanhToan
    {
        [BsonElement("tong_tien")]
        public decimal TongTien { get; set; }

        [BsonElement("hinh_thuc")]
        public string HinhThuc { get; set; }

        [BsonElement("da_thanh_toan")]
        public bool DaThanhToan { get; set; }
    }
}