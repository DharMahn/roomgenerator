using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timer = System.Windows.Forms.Timer;
using System.Runtime.InteropServices;

namespace WinFormsApp4
{
    public partial class ToastForm : Form
    {
        private Timer fadeTimer = new Timer();
        private Timer closeTimer = new Timer();
        private double fadeStep = 0.10; // Fade step for each timer tick
        protected override bool ShowWithoutActivation => true;

        public ToastForm(string message, int duration = 3000)
        {
            SetStyle(ControlStyles.Selectable, false);
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            BackColor = Color.Gray;
            Opacity = 0.5;
            Padding = new Padding(10);
            Width = 100;
            Height = 50;
            Label messageLabel = new Label
            {
                Text = message,
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            Controls.Add(messageLabel);

            // Close timer setup
            closeTimer.Interval = duration;
            closeTimer.Tick += (sender, e) => { fadeTimer.Start(); }; // Start fading out
            closeTimer.Start();

            // Fade timer setup
            fadeTimer.Interval = 16; // Adjust for smoother or quicker fade
            fadeTimer.Tick += (sender, e) =>
            {
                if (Opacity > 0)
                    Opacity -= fadeStep;
                else
                    Close(); // Close the form once fully faded
            };
        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // Define the size of the rounded corners
            float cornerRadius = 40.0f; // Adjust the radius size as needed

            // Create a rounded rectangle path for the entire form
            var path = GetRoundedRectPath(0, 0, Width, Height, cornerRadius);

            // Set the form's region to the rounded rectangle
            this.Region = new Region(path);
        }

        private static System.Drawing.Drawing2D.GraphicsPath GetRoundedRectPath(float x, float y, float width, float height, float radius)
        {
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            // Top-left corner
            path.AddArc(x, y, radius, radius, 180, 90);
            // Top edge
            path.AddLine(x + radius, y, x + width - radius, y);
            // Top-right corner
            path.AddArc(x + width - radius, y, radius, radius, 270, 90);
            // Right edge
            path.AddLine(x + width, y + radius, x + width, y + height - radius);
            // Bottom-right corner
            path.AddArc(x + width - radius, y + height - radius, radius, radius, 0, 90);
            // Bottom edge
            path.AddLine(x + width - radius, y + height, x + radius, y + height);
            // Bottom-left corner
            path.AddArc(x, y + height - radius, radius, radius, 90, 90);
            // Left edge
            path.AddLine(x, y + height - radius, x, y + radius);
            path.CloseFigure();
            return path;
        }

        // Method to position the toast relative to the main form
        public void PositionRelativeToForm(Form mainForm)
        {
            int x = mainForm.Location.X + mainForm.Width - this.Width - 20;
            int y = mainForm.Location.Y + mainForm.Height - this.Height - 20;
            this.Location = new Point(x, y);
        }
    }
}
