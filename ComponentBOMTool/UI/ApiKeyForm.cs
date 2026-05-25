using System;
using System.Windows.Forms;

namespace ComponentBOMTool.UI
{
    public class ApiKeyForm : Form
    {
        private TextBox txtKey;
        private Button btnOk;

        public string ApiKey => txtKey.Text;

        public ApiKeyForm()
        {
            this.Text = "Enter Mouser API Key";
            this.Width = 420;
            this.Height = 180; // ✅ increase height (fix cut issue)
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // ✅ Label
            Label lbl = new Label()
            {
                Text = "Enter Mouser API Key:",
                Left = 20,
                Top = 20,
                AutoSize = true
            };

            // ✅ TextBox
            txtKey = new TextBox()
            {
                Left = 20,
                Top = 50,
                Width = 360
            };

            // ✅ OK Button (SAFE POSITION)
            btnOk = new Button()
            {
                Text = "OK",
                Width = 90,
                Height = 30,
                Left = 290,
                Top = 95 // ✅ FIXED (not too low)
            };

            btnOk.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtKey.Text))
                {
                    MessageBox.Show("API Key cannot be empty");
                    return;
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            this.Controls.Add(lbl);
            this.Controls.Add(txtKey);
            this.Controls.Add(btnOk);

            // ✅ Enter key support
            this.AcceptButton = btnOk;
        }

    }
}