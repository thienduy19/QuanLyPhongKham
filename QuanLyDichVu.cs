using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Clinix
{
    public partial class QuanLyDichVu : Form
    {
        private IMongoCollection<DichVu> collection;

        public QuanLyDichVu()
        {
            InitializeComponent();

            var client = new MongoClient("mongodb://localhost:27017");
            var database = client.GetDatabase("QuanLyPhongKhamDB");
            collection = database.GetCollection<DichVu>("dich_vu");

            dgvDichVu.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvDichVu.MultiSelect = false;
            dgvDichVu.ReadOnly = true;
            dgvDichVu.AllowUserToAddRows = false;
            dgvDichVu.AllowUserToDeleteRows = false;

            // Ô Loại nhập được để thêm thuốc / dịch vụ; giá trị được chuẩn hóa khi Thêm và Sửa
            textLoai.ReadOnly = false;
            toolTip1.SetToolTip(textLoai, "Nhập Thuoc hoặc DichVu (để trống = DichVu)");

            // Designer chưa gắn sự kiện cho 2 nút này nên gắn ở đây
            btnLamMoi.Click += btnLamMoi_Click;
            btnLoad.Click += btnLamMoi_Click;

            LoadData();
        }

        // Đưa giá trị nhập về đúng 2 giá trị hệ thống dùng: "Thuoc" (KeToa lọc theo giá trị này) và "DichVu".
        // Trả về null nếu nhập sai.
        private static string ChuanHoaLoai(string loai)
        {
            string s = (loai ?? "").Trim().Replace(" ", "").ToLowerInvariant();
            if (s == "") return "DichVu";
            if (s == "thuoc" || s == "thuốc") return "Thuoc";
            if (s == "dichvu" || s == "dịchvụ") return "DichVu";
            return null;
        }

        private void HienThi(List<DichVu> ds)
        {
            dgvDichVu.DataSource = ds;

            if (dgvDichVu.Columns["Id"] != null) dgvDichVu.Columns["Id"].Visible = false;
            if (dgvDichVu.Columns["Loai"] != null)
            {
                dgvDichVu.Columns["Loai"].Visible = true;
                dgvDichVu.Columns["Loai"].HeaderText = "Loại";
            }

            dgvDichVu.Columns["MaDichVu"].HeaderText = "Mã dịch vụ / thuốc";
            dgvDichVu.Columns["TenDichVu"].HeaderText = "Tên dịch vụ / thuốc";
            dgvDichVu.Columns["DonGia"].HeaderText = "Đơn giá";
            dgvDichVu.Columns["DonGia"].DefaultCellStyle.Format = "N0";
            dgvDichVu.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            lblSoLuongDichVu.Text = $"Tổng số dịch vụ và thuốc: {ds.Count}";
        }

        private void LoadData()
        {
            try
            {
                HienThi(collection.Find(FilterDefinition<DichVu>.Empty)
                                  .SortBy(dv => dv.MaDichVu)
                                  .ToList());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối MongoDB: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void XoaONhap()
        {
            txtMaDichVu.Clear();
            txtTenDichVu.Clear();
            txtDonGia.Clear();
            textLoai.Clear();
        }

        private void btnLamMoi_Click(object sender, EventArgs e)
        {
            XoaONhap();
            txtTimKiem.Clear();
            LoadData();
        }

        private void btnThem_Click(object sender, EventArgs e)
        {
            try
            {
                string maDichVu = txtMaDichVu.Text.Trim();
                string tenDichVu = txtTenDichVu.Text.Trim();
                decimal donGia;

                if (string.IsNullOrEmpty(maDichVu) || string.IsNullOrEmpty(tenDichVu)
                    || !decimal.TryParse(txtDonGia.Text.Trim(), out donGia) || donGia < 0)
                {
                    MessageBox.Show("Vui lòng kiểm tra lại giá trị nhập vào.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string loai = ChuanHoaLoai(textLoai.Text);
                if (loai == null)
                {
                    MessageBox.Show("Loại chỉ nhận \"Thuoc\" hoặc \"DichVu\".", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    textLoai.Focus();
                    return;
                }

                // Mã dùng chung giữa thuốc và dịch vụ trong cùng collection nên kiểm tra trùng trên toàn collection
                var existingDichVu = collection.Find(dv => dv.MaDichVu == maDichVu).FirstOrDefault();
                if (existingDichVu != null)
                {
                    MessageBox.Show("Mã đã tồn tại trong hệ thống.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                DichVu newDichVu = new DichVu
                {
                    MaDichVu = maDichVu,
                    TenDichVu = tenDichVu,
                    DonGia = donGia,
                    Loai = loai
                };

                collection.InsertOne(newDichVu);

                MessageBox.Show("Thêm thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                XoaONhap();
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Có lỗi xảy ra: {ex.Message}");
            }
        }

        // Nút Sửa
        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                string ma = txtMaDichVu.Text.Trim();
                string ten = txtTenDichVu.Text.Trim();
                decimal donGia;

                if (string.IsNullOrEmpty(ma))
                {
                    MessageBox.Show("Vui lòng click chọn một dòng ở dưới bảng trước khi bấm Sửa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (string.IsNullOrEmpty(ten) || !decimal.TryParse(txtDonGia.Text.Trim(), out donGia) || donGia < 0)
                {
                    MessageBox.Show("Tên hoặc đơn giá không hợp lệ!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string loai = ChuanHoaLoai(textLoai.Text);
                if (loai == null)
                {
                    MessageBox.Show("Loại chỉ nhận \"Thuoc\" hoặc \"DichVu\".", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    textLoai.Focus();
                    return;
                }

                // Chỉ cập nhật các trường cần sửa, không ghi đè cả document
                var filter = Builders<DichVu>.Filter.Eq(dv => dv.MaDichVu, ma);
                var update = Builders<DichVu>.Update
                    .Set(dv => dv.TenDichVu, ten)
                    .Set(dv => dv.DonGia, donGia)
                    .Set(dv => dv.Loai, loai);
                var kq = collection.UpdateOne(filter, update);

                if (kq.MatchedCount > 0)
                {
                    MessageBox.Show("Cập nhật thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadData();
                }
                else
                {
                    MessageBox.Show("Không tìm thấy dòng cần sửa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Có lỗi xảy ra: {ex.Message}");
            }
        }

        private void btnXoa_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvDichVu.CurrentRow == null || dgvDichVu.CurrentRow.Index < 0)
                {
                    MessageBox.Show("Vui lòng click chọn một dòng trên bảng trước khi xóa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string maDichVu = dgvDichVu.CurrentRow.Cells["MaDichVu"].Value?.ToString();
                if (string.IsNullOrEmpty(maDichVu))
                {
                    MessageBox.Show("Vui lòng click chọn một dòng trên bảng trước khi xóa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                DialogResult dialogResult = MessageBox.Show("Bạn có chắc chắn muốn xóa dòng này không?", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dialogResult != DialogResult.Yes) return;

                // Mã là duy nhất trên toàn collection (Thêm đã chặn trùng) nên xóa theo mã
                var filter = Builders<DichVu>.Filter.Eq(dv => dv.MaDichVu, maDichVu);
                var result = collection.DeleteOne(filter);

                if (result.DeletedCount > 0)
                {
                    MessageBox.Show("Xóa thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    XoaONhap();
                    LoadData();
                }
                else
                {
                    MessageBox.Show("Không tìm thấy dòng cần xóa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Có lỗi xảy ra: {ex.Message}");
            }
        }

        private void btnTimKiem_Click(object sender, EventArgs e)
        {
            try
            {
                string keyword = txtTimKiem.Text.Trim();
                if (string.IsNullOrEmpty(keyword))
                {
                    LoadData();
                    return;
                }

                var builder = Builders<DichVu>.Filter;
                var filter = builder.Regex(dv => dv.TenDichVu, MongoHelper.Like(keyword)) |
                             builder.Regex(dv => dv.MaDichVu, MongoHelper.Like(keyword));

                HienThi(collection.Find(filter).SortBy(dv => dv.MaDichVu).ToList());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Có lỗi xảy ra: {ex.Message}");
            }
        }

        private void dgvDichVu_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = dgvDichVu.Rows[e.RowIndex];

            txtMaDichVu.Text = row.Cells["MaDichVu"].Value?.ToString();
            txtTenDichVu.Text = row.Cells["TenDichVu"].Value?.ToString();
            textLoai.Text = row.Cells["Loai"].Value?.ToString() ?? "";

            object gia = row.Cells["DonGia"].Value;
            txtDonGia.Text = gia is decimal d ? d.ToString("0.##") : gia?.ToString();
        }
        private void btnThoat_Click(object sender, EventArgs e)
        {
            Program.QuayVeMenu = true;
            this.Close();
        }

        private void btnQuayLai_Click(object sender, EventArgs e)
        {
            Program.QuayVeMenu = true;
            this.Close();
        }
    }
}