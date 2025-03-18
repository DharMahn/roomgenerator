namespace WinFormsApp4
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            TileCanvas = new PictureBox();
            buttonTop = new Button();
            buttonLeft = new Button();
            buttonRight = new Button();
            buttonBottom = new Button();
            button1 = new Button();
            RoomCanvas = new PictureBox();
            listBoxStatistics = new ListBox();
            DrawGridCheckBox = new CheckBox();
            DrawHistoryCheckBox = new CheckBox();
            ((System.ComponentModel.ISupportInitialize)TileCanvas).BeginInit();
            ((System.ComponentModel.ISupportInitialize)RoomCanvas).BeginInit();
            SuspendLayout();
            // 
            // TileCanvas
            // 
            TileCanvas.BackColor = SystemColors.ControlDark;
            TileCanvas.Location = new Point(86, 81);
            TileCanvas.Name = "TileCanvas";
            TileCanvas.Size = new Size(300, 300);
            TileCanvas.TabIndex = 0;
            TileCanvas.TabStop = false;
            TileCanvas.Paint += TileCanvas_Paint;
            // 
            // buttonTop
            // 
            buttonTop.Location = new Point(86, 12);
            buttonTop.Name = "buttonTop";
            buttonTop.Size = new Size(300, 63);
            buttonTop.TabIndex = 1;
            buttonTop.Text = "TOP";
            buttonTop.UseVisualStyleBackColor = true;
            // 
            // buttonLeft
            // 
            buttonLeft.Location = new Point(12, 81);
            buttonLeft.Name = "buttonLeft";
            buttonLeft.Size = new Size(68, 300);
            buttonLeft.TabIndex = 2;
            buttonLeft.Text = "LEFT";
            buttonLeft.UseVisualStyleBackColor = true;
            // 
            // buttonRight
            // 
            buttonRight.Location = new Point(392, 81);
            buttonRight.Name = "buttonRight";
            buttonRight.Size = new Size(68, 300);
            buttonRight.TabIndex = 3;
            buttonRight.Text = "RIGHT";
            buttonRight.UseVisualStyleBackColor = true;
            // 
            // buttonBottom
            // 
            buttonBottom.Location = new Point(86, 387);
            buttonBottom.Name = "buttonBottom";
            buttonBottom.Size = new Size(300, 63);
            buttonBottom.TabIndex = 4;
            buttonBottom.Text = "BOTTOM";
            buttonBottom.UseVisualStyleBackColor = true;
            // 
            // button1
            // 
            button1.Location = new Point(794, 81);
            button1.Name = "button1";
            button1.Size = new Size(173, 35);
            button1.TabIndex = 5;
            button1.Text = "Save tiles to file";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // RoomCanvas
            // 
            RoomCanvas.BackColor = SystemColors.ControlDark;
            RoomCanvas.Location = new Point(488, 81);
            RoomCanvas.Name = "RoomCanvas";
            RoomCanvas.Size = new Size(300, 300);
            RoomCanvas.TabIndex = 6;
            RoomCanvas.TabStop = false;
            RoomCanvas.Click += RoomCanvas_Click;
            RoomCanvas.Paint += RoomCanvas_Paint;
            RoomCanvas.MouseMove += RoomCanvas_MouseMove;
            // 
            // listBoxStatistics
            // 
            listBoxStatistics.Enabled = false;
            listBoxStatistics.FormattingEnabled = true;
            listBoxStatistics.ItemHeight = 15;
            listBoxStatistics.Location = new Point(794, 122);
            listBoxStatistics.Name = "listBoxStatistics";
            listBoxStatistics.Size = new Size(173, 259);
            listBoxStatistics.TabIndex = 7;
            // 
            // DrawGridCheckBox
            // 
            DrawGridCheckBox.AutoSize = true;
            DrawGridCheckBox.Location = new Point(488, 56);
            DrawGridCheckBox.Name = "DrawGridCheckBox";
            DrawGridCheckBox.Size = new Size(77, 19);
            DrawGridCheckBox.TabIndex = 8;
            DrawGridCheckBox.Text = "Draw grid";
            DrawGridCheckBox.UseVisualStyleBackColor = true;
            DrawGridCheckBox.CheckedChanged += Settings_Changed;
            // 
            // DrawHistoryCheckBox
            // 
            DrawHistoryCheckBox.AutoSize = true;
            DrawHistoryCheckBox.Location = new Point(571, 56);
            DrawHistoryCheckBox.Name = "DrawHistoryCheckBox";
            DrawHistoryCheckBox.Size = new Size(92, 19);
            DrawHistoryCheckBox.TabIndex = 9;
            DrawHistoryCheckBox.Text = "Draw history";
            DrawHistoryCheckBox.UseVisualStyleBackColor = true;
            DrawHistoryCheckBox.CheckedChanged += Settings_Changed;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1011, 463);
            Controls.Add(DrawHistoryCheckBox);
            Controls.Add(DrawGridCheckBox);
            Controls.Add(listBoxStatistics);
            Controls.Add(RoomCanvas);
            Controls.Add(button1);
            Controls.Add(buttonBottom);
            Controls.Add(buttonRight);
            Controls.Add(buttonLeft);
            Controls.Add(buttonTop);
            Controls.Add(TileCanvas);
            HelpButton = true;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Form1";
            Text = "Form1";
            HelpButtonClicked += Form1_HelpButtonClicked;
            KeyDown += Form1_KeyDown;
            Move += Form1_Move;
            ((System.ComponentModel.ISupportInitialize)TileCanvas).EndInit();
            ((System.ComponentModel.ISupportInitialize)RoomCanvas).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private PictureBox TileCanvas;
        private Button buttonTop;
        private Button buttonLeft;
        private Button buttonRight;
        private Button buttonBottom;
        private Button button1;
        private PictureBox RoomCanvas;
        private ListBox listBoxStatistics;
        private CheckBox DrawGridCheckBox;
        private CheckBox DrawHistoryCheckBox;
    }
}
