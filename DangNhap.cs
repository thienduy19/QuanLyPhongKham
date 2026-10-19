using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Clinix
{
    public partial class DangNhap : Form
    {
       
        public DangNhap()
        {
            InitializeComponent();
            this.AcceptButton = btnDangNhap;
        }

        private void DangNhap_Load(object sender, EventArgs e)
        {
            txtMatKhau.PasswordChar = '●';
        }

        DataClasses2DataContext db = new DataClasses2DataContext();
        // Tài khoản mặc định
        private const string TenDangNhapMacDinh= "admin";
        private const string MatKhauMatDinh = "123";
        private void btnDangNhap_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtTenDangNhap.Text) || string.IsNullOrEmpty(txtMatKhau.Text))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ Tên đăng nhập và Mật khẩu.", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Kiểm tra tài khoản mặc định
                if (txtTenDangNhap.Text == TenDangNhapMacDinh && txtMatKhau.Text == MatKhauMatDinh)
                {
                    MessageBox.Show("Đăng nhập thành công bằng tài khoản mặc định!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Chuyển đến Menu sau khi đăng nhập thành công
                    Menu mn = new Menu();
                    this.Hide();
                    mn.Show();
                    return;
                }

                // Kiểm tra tài khoản trong cơ sở dữ liệu
                var nguoiDung = db.TaiKhoans.SingleOrDefault(tk => tk.TenTaiKhoan == txtTenDangNhap.Text && tk.MatKhau == txtMatKhau.Text);

                if (nguoiDung != null)
                {
                    MessageBox.Show("Đăng nhập thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Chuyển đến Menu sau khi đăng nhập thành công
                    Menu mn = new Menu();
                    this.Hide();
                    mn.Show();
                }
                else
                {
                    MessageBox.Show("Tên đăng nhập hoặc mật khẩu không đúng.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Đã xảy ra lỗi: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void label2_Click(object sender, EventArgs e)
        {
            txtTenDangNhap.Text = string.Empty;
            txtMatKhau.Text = string.Empty;
            txtTenDangNhap.Focus();
        }

        private void btnThoat_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void lblNhapLai_MouseHover(object sender, EventArgs e)
        {
            lblXoaTruongNhap.Font = new Font(lblXoaTruongNhap.Font, FontStyle.Underline);
        }

        private void lblXoaTruongNhap_MouseLeave(object sender, EventArgs e)
        {
            lblXoaTruongNhap.Font = new Font(lblXoaTruongNhap.Font, FontStyle.Italic);
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            // Kiểm tra trạng thái của CheckBox
            if (chkHienMatKhau.Checked)
            {
                txtMatKhau.PasswordChar = '\0'; // Hiện mật khẩu
            }
            else
            {
                txtMatKhau.PasswordChar = '●'; // Ẩn mật khẩu
            }
        }
    }
}
