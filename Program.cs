using System;
using System.Drawing;
using System.Windows.Forms;

namespace Clinix
{
    internal static class Program
    {
        // Được đặt true khi người dùng bấm nút Quay lại trong một giao diện.
        internal static bool QuayVeMenu { get; set; }

        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            do
            {
                QuayVeMenu = false;
                Form selectedForm;

                using (var chooser = new StartupForm())
                {
                    if (chooser.ShowDialog() != DialogResult.OK ||
                        chooser.SelectedForm == null)
                    {
                        return;
                    }

                    selectedForm = chooser.SelectedForm;
                }

                Application.Run(selectedForm);
            }
            while (QuayVeMenu);
        }
    }

    internal sealed class StartupForm : Form
    {
        // Số bệnh nhân mẫu mỗi lần bấm "Tạo dữ liệu mẫu"
        private const int SoBenhNhanMau = 10000;

        public Form SelectedForm { get; private set; }

        public StartupForm()
        {
            Text = "Chọn giao diện";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(520, 540);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 5,
                Padding = new Padding(24)
            };

            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 25));

            var heading = new Label
            {
                Text = "Bạn muốn mở giao diện nào?",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 15, FontStyle.Bold)
            };

            layout.Controls.Add(heading, 0, 0);
            layout.SetColumnSpan(heading, 2);

            AddChoiceButton(
                layout,
                "Quản lý dịch vụ",
                () => new QuanLyDichVu(),
                0,
                1);

            AddChoiceButton(
                layout,
                "Thêm lịch khám",
                () => new ThemLichKham(),
                1,
                1);

            AddChoiceButton(
                layout,
                "Quản lý bệnh nhân",
                () => new frmQLBenhNhan(),
                0,
                2);

            AddChoiceButton(
                layout,
                "Khám bệnh",
                () => new KhamBenh(),
                1,
                2);

            // Lịch sử KCB mở như form chính (không khóa theo bệnh nhân nào), nằm trọn một hàng
            AddChoiceButton(
                layout,
                "Lịch sử KCB",
                () => new frmLichSuKCB(null, true),
                0,
                3);
            layout.SetColumnSpan(layout.GetControlFromPosition(0, 3), 2);

            // Hàng cuối: công cụ dữ liệu mẫu, không đóng form chọn giao diện
            AddToolButton(
                layout,
                "Tạo dữ liệu mẫu",
                () =>
                {
                    DialogResult xacNhan = MessageBox.Show(
                        "Tạo " + SoBenhNhanMau.ToString("N0") + " bệnh nhân mẫu cùng hồ sơ bệnh án?",
                        "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    return xacNhan == DialogResult.Yes
                        ? SeedData.Tao(SoBenhNhanMau)
                        : null;
                },
                0,
                4);

            AddToolButton(
                layout,
                "Xóa dữ liệu mẫu",
                () =>
                {
                    DialogResult xacNhan = MessageBox.Show(
                        "Xóa toàn bộ bệnh nhân mẫu (mã bắt đầu bằng BNS) và hồ sơ của họ?",
                        "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    return xacNhan == DialogResult.Yes
                        ? SeedData.Xoa()
                        : null;
                },
                1,
                4);

            Controls.Add(layout);
        }

        private void AddChoiceButton(
            TableLayoutPanel layout,
            string text,
            Func<Form> createForm,
            int column,
            int row)
        {
            var button = new Button
            {
                Text = text,
                Dock = DockStyle.Fill,
                Margin = new Padding(8),
                Font = new Font("Segoe UI", 11, FontStyle.Regular)
            };

            button.Click += (sender, e) =>
            {
                SelectedForm = createForm();
                DialogResult = DialogResult.OK;
                Close();
            };

            layout.Controls.Add(button, column, row);
        }

        // Nút chạy một tác vụ trả về thông báo kết quả; trả về null nghĩa là người dùng đã hủy
        private void AddToolButton(
            TableLayoutPanel layout,
            string text,
            Func<string> task,
            int column,
            int row)
        {
            var button = new Button
            {
                Text = text,
                Dock = DockStyle.Fill,
                Margin = new Padding(8),
                Font = new Font("Segoe UI", 11, FontStyle.Regular)
            };

            button.Click += (sender, e) =>
            {
                try
                {
                    UseWaitCursor = true;
                    string ketQua = task();
                    UseWaitCursor = false;

                    if (ketQua != null)
                    {
                        MessageBox.Show(ketQua, "Thông báo",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    UseWaitCursor = false;
                    MessageBox.Show("Lỗi: " + ex.Message, "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            layout.Controls.Add(button, column, row);
        }
    }
}