using MongoDB.Bson;
using MongoDB.Driver;

namespace Clinix
{
    // Sinh mã phiếu khám theo số thứ tự: PK00001, PK00002, ...
    // Lấy mã lớn nhất đang có trong ho_so_benh_an rồi cộng 1. Chỉ xét mã đúng dạng PK + 5 chữ số,
    // nên các mã cũ kiểu timestamp (PK + 12 chữ số) không làm lệch dãy số.
    internal static class MaPhieuHelper
    {
        private const string TienTo = "PK";
        private const int SoChuSo = 5;

        public static string TaoMaPhieuTiepTheo(IMongoCollection<HoSoBenhAn> collectionHoSo)
        {
            var filter = Builders<HoSoBenhAn>.Filter.Regex(
                h => h.MaPhieu,
                new BsonRegularExpression("^" + TienTo + "\\d{" + SoChuSo + "}$"));

            // Cùng độ dài và có số 0 đệm nên sắp xếp chuỗi giảm dần cũng là sắp xếp số giảm dần
            var cuoi = collectionHoSo.Find(filter)
                                     .SortByDescending(h => h.MaPhieu)
                                     .Limit(1)
                                     .FirstOrDefault();

            int so = 0;
            if (cuoi != null)
                int.TryParse(cuoi.MaPhieu.Substring(TienTo.Length), out so);

            return TienTo + (so + 1).ToString("D" + SoChuSo);
        }
    }
}