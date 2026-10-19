using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using MongoDB.Driver;

namespace Clinix
{
    // Một dòng trong danh sách lịch sử KCB (gán thẳng vào DataGridView)
    public class LichSuKCBItem
    {
        public string Id { get; set; }
        public string MaPhieu { get; set; }
        public string MaBenhNhan { get; set; }
        public string TenBenhNhan { get; set; }
        public DateTime NgayKham { get; set; }
        public string MaBacSi { get; set; }
        public string TenBacSi { get; set; }
        public string TrangThai { get; set; }
        public string ChanDoan { get; set; }
        public int SoLoaiThuoc { get; set; }
        public int SoDichVu { get; set; }
        public decimal TongTien { get; set; }
        public bool DaThanhToan { get; set; }

        // Giữ nguyên hồ sơ gốc để xem chi tiết toa thuốc / dịch vụ, không hiện lên lưới
        [Browsable(false)]
        public HoSoBenhAn HoSo { get; set; }
    }

    public class LichSuKCBService
    {
        private readonly IMongoCollection<HoSoBenhAn> collectionHoSo;
        private readonly IMongoCollection<BacSi> collectionBacSi;

        public LichSuKCBService(IMongoDatabase database)
        {
            collectionHoSo = database.GetCollection<HoSoBenhAn>("ho_so_benh_an");
            collectionBacSi = database.GetCollection<BacSi>("bac_si");
        }

        private static string ChuanHoaMa(string ma)
        {
            return (ma ?? "").Trim().ToUpperInvariant();
        }

        // maBN rỗng/null = lấy tất cả hồ sơ. Mới nhất xếp trước.
        public List<LichSuKCBItem> LayLichSu(string maBN = null, DateTime? tuNgay = null, DateTime? denNgay = null)
        {
            var b = Builders<HoSoBenhAn>.Filter;
            var filter = b.Empty;

            if (!string.IsNullOrWhiteSpace(maBN))
                filter &= b.Eq(h => h.MaBenhNhan, maBN.Trim());

            if (tuNgay.HasValue)
            {
                var tu = DateTime.SpecifyKind(tuNgay.Value.Date, DateTimeKind.Local);
                filter &= b.Gte(h => h.NgayKham, tu);
            }
            if (denNgay.HasValue)
            {
                var den = DateTime.SpecifyKind(denNgay.Value.Date.AddDays(1), DateTimeKind.Local);
                filter &= b.Lt(h => h.NgayKham, den);
            }

            var dsHoSo = collectionHoSo.Find(filter)
                                       .SortByDescending(h => h.NgayKham)
                                       .ToList();

            // Tra tên bác sĩ 1 lần cho cả danh sách, tránh query lặp theo từng hồ sơ
            var tenBacSi = collectionBacSi.Find(FilterDefinition<BacSi>.Empty).ToList()
                .Where(bs => !string.IsNullOrWhiteSpace(bs.MaBacSi))
                .GroupBy(bs => ChuanHoaMa(bs.MaBacSi))
                .ToDictionary(g => g.Key, g => g.First().TenBacSi);

            return dsHoSo.Select(hs =>
            {
                string ten;
                tenBacSi.TryGetValue(ChuanHoaMa(hs.MaBacSi), out ten);

                var thuoc = hs.DonThuoc ?? new List<ChiTietThuoc>();
                var dichVu = hs.DichVuChiDinh ?? new List<ChiTietDichVu>();

                // Ưu tiên tổng tiền đã lưu; thiếu thì tự tính lại từ toa + dịch vụ
                decimal tong = hs.ThanhToan != null
                    ? hs.ThanhToan.TongTien
                    : thuoc.Sum(t => t.DonGia * t.SoLuong) + dichVu.Sum(d => d.DonGia * d.SoLuong);

                return new LichSuKCBItem
                {
                    Id = hs.Id,
                    MaPhieu = hs.MaPhieu,
                    MaBenhNhan = hs.MaBenhNhan,
                    TenBenhNhan = hs.TenBenhNhan,
                    NgayKham = hs.NgayKham,
                    MaBacSi = hs.MaBacSi,
                    TenBacSi = ten ?? "(không rõ bác sĩ)",
                    TrangThai = hs.TrangThai,
                    ChanDoan = hs.ChanDoan,
                    SoLoaiThuoc = thuoc.Count,
                    SoDichVu = dichVu.Count,
                    TongTien = tong,
                    DaThanhToan = hs.ThanhToan != null && hs.ThanhToan.DaThanhToan,
                    HoSo = hs
                };
            }).ToList();
        }

        // Dùng khi người dùng chọn 1 dòng lịch sử và muốn xem chi tiết toa thuốc, dịch vụ
        public HoSoBenhAn LayChiTiet(string hoSoId)
        {
            return collectionHoSo.Find(h => h.Id == hoSoId).FirstOrDefault();
        }
    }
}