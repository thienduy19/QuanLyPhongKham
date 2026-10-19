using System;
using System.Drawing;
using System.Windows.Forms;


namespace Clinix
{
    public partial class CustomMessageBox : Form
    {
        public CustomMessageBox(string message, string title, MessageBoxIcon icon = MessageBoxIcon.None, System.Drawing.Font customFont = null)
        {
            InitializeComponent();
            this.Text = title;
            lblMessage.Text = message;

            if (customFont != null)
            {
                lblMessage.Font = customFont;
            }

            pbIcon.Image = GetIconImage(icon);
            pbIcon.Visible = pbIcon.Image != null;
        }

        private Image GetIconImage(MessageBoxIcon icon)
        {
            switch (icon)
            {
                case MessageBoxIcon.Warning:
                    return SystemIcons.Warning.ToBitmap(); // Icon cảnh báo
                case MessageBoxIcon.Error:
                    return SystemIcons.Error.ToBitmap(); // Icon lỗi
                case MessageBoxIcon.Information:
                    return SystemIcons.Information.ToBitmap(); // Icon thông tin
                case MessageBoxIcon.Question:
                    return SystemIcons.Question.ToBitmap(); // Icon câu hỏi
                default:
                    return null; // Không có icon
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        public static DialogResult Show(string message, string title = "Thông báo", MessageBoxIcon icon = MessageBoxIcon.None, System.Drawing.Font customFont = null)
        {
            using (var customBox = new CustomMessageBox(message, title, icon, customFont))
            {
                return customBox.ShowDialog();
            }
        }
    }
}
