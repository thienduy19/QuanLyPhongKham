using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using MongoDB.Driver;

namespace Clinix
{
    public partial class KeToa : Form
    {
        private IMongoCollection<DichVu> collectionDichVu;
        private IMongoCollection<HoSoBenhAn> collectionHoSo;
        private BindingList<ChiTietThuoc> danhSachThuocTam = new BindingList<ChiTietThuoc>();
        private string maBenhNhanHienTai;
        private string hoSoId;

        public KeToa(string maBN, string hoSoId)
        {
            InitializeComponent();
            maBenhNhanHienTai = maBN;
            this.hoSoId = hoSoId;

            var database = new MongoClient("mongodb://localhost:27017").GetDatabase("QuanLyPhongKhamDB");
            collectionDichVu = database.GetCollection<DichVu>("dich_vu");
            collectionHoSo = database.GetCollection<HoSoBenhAn>("ho_so_benh_an");
        }

        private void KeToa_Load(object sender, EventArgs e)
        {
            txtMaBN.Text = maBenhNhanHienTai;
            dgvToaThuoc.DataSource = danhSachThuocTam;
            // Khóa lưới: đơn giá/số lượng chỉ đổi qua nút Kê thêm thuốc, không sửa tay trên lưới
            dgvToaThuoc.ReadOnly = true;
            dgvToaThuoc.AllowUserToAddRows = false;
            dgvToaThuoc.AllowUserToDeleteRows = false;
            dgvToaThuoc.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            LoadDanhSachThuoc();
        }

        private void LoadDanhSachThuoc()
        {
            try
            {
                var filter = Builders<DichVu>.Filter.Eq(dv => dv.Loai, "Thuoc");
                var danhSach = collectionDichVu.Find(filter).ToList();

                if (danhSach.Count == 0)
                {
                    MessageBox.Show("Chưa có thuốc nào (document trong dich_vu cần có Loai = \"Thuoc\").");
                    return;
                }

                cbbThuoc.DataSource = danhSach;
                cbbThuoc.DisplayMember = "TenDichVu";
                cbbThuoc.ValueMember = "MaDichVu";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải thuốc: " + ex.Message);
            }
        }

        private void btnThemThuoc_Click(object sender, EventArgs e)
        {
            var thuoc = cbbThuoc.SelectedItem as DichVu;
            if (thuoc == null)
            {
                MessageBox.Show("Vui lòng chọn thuốc!");
                return;
            }
            if (!int.TryParse(txtSoLuong.Text.Trim(), out int soLuong) || soLuong <= 0)
            {
                MessageBox.Show("Số lượng phải là số nguyên lớn hơn 0!");
                return;
            }

            var daCo = danhSachThuocTam.FirstOrDefault(t => t.MaThuoc == thuoc.MaDichVu);
            if (daCo != null)
            {
                daCo.SoLuong += soLuong;
                if (!string.IsNullOrWhiteSpace(txtCachDung.Text)) daCo.CachDung = txtCachDung.Text.Trim();
                dgvToaThuoc.Refresh();
            }
            else
            {
                danhSachThuocTam.Add(new ChiTietThuoc
                {
                    MaThuoc = thuoc.MaDichVu,
                    TenThuoc = thuoc.TenDichVu,
                    DonGia = thuoc.DonGia,
                    SoLuong = soLuong,
                    CachDung = txtCachDung.Text.Trim()
                });
            }

            txtSoLuong.Text = "";
            txtCachDung.Text = "";
        }

        private void btnLuuToa_Click(object sender, EventArgs e)
        {
            if (danhSachThuocTam.Count == 0)
            {
                MessageBox.Show("Toa thuốc đang trống!");
                return;
            }

            try
            {
                var filter = Builders<HoSoBenhAn>.Filter.Eq(hs => hs.Id, hoSoId);
                var hoSo = collectionHoSo.Find(filter).FirstOrDefault();
                if (hoSo == null)
                {
                    MessageBox.Show("Không tìm thấy hồ sơ khám. Vui lòng lưu hồ sơ ở màn hình Khám Bệnh trước.");
                    return;
                }
                if (hoSo.ThanhToan != null && hoSo.ThanhToan.DaThanhToan)
                {
                    MessageBox.Show("Hồ sơ này đã thanh toán, không thể kê thêm thuốc.");
                    return;
                }

                var thuocMoi = danhSachThuocTam.ToList();

                // Tính lại tổng tiền từ đầu: thuốc cũ + thuốc mới + dịch vụ chỉ định
                decimal tong = hoSo.DonThuoc.Sum(t => t.DonGia * t.SoLuong)
                             + thuocMoi.Sum(t => t.DonGia * t.SoLuong)
                             + hoSo.DichVuChiDinh.Sum(d => d.DonGia * d.SoLuong);

                var thanhToan = hoSo.ThanhToan ?? new ThongTinThanhToan();
                thanhToan.TongTien = tong;

                var update = Builders<HoSoBenhAn>.Update
                    .PushEach(hs => hs.DonThuoc, thuocMoi)
                    .Set(hs => hs.ThanhToan, thanhToan);

                collectionHoSo.UpdateOne(filter, update);

                MessageBox.Show("Kê toa thành công! Dữ liệu đã được nhúng vào Hồ Sơ Bệnh Án.", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi lưu DB: " + ex.Message);
            }
        }

        private void txtMaBN_TextChanged(object sender, EventArgs e) { }
        private void btnThoat_Click(object sender, EventArgs e)
        {
            Program.QuayVeMenu = true;
            this.Close();
        }
    }
}