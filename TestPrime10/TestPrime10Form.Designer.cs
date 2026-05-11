namespace TestPrime10
{
    partial class TestPrime10Form
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
            btnDig = new Button();
            btnTwinSemi = new Button();
            chkEven = new CheckBox();
            chkDig = new CheckBox();
            rtbOutput = new RichTextBox();
            btnStage = new Button();
            lblStage = new Label();
            btnGo = new Button();
            lblLen = new Label();
            txtLen = new TextBox();
            lblStart = new Label();
            txtStart = new TextBox();
            SuspendLayout();
            // 
            // btnDig
            // 
            btnDig.Location = new Point(13, 296);
            btnDig.Name = "btnDig";
            btnDig.Size = new Size(75, 23);
            btnDig.TabIndex = 25;
            btnDig.Text = "Dig";
            btnDig.UseVisualStyleBackColor = true;
            btnDig.Click += btnDig_Click;
            // 
            // btnTwinSemi
            // 
            btnTwinSemi.Location = new Point(13, 267);
            btnTwinSemi.Name = "btnTwinSemi";
            btnTwinSemi.Size = new Size(75, 23);
            btnTwinSemi.TabIndex = 24;
            btnTwinSemi.Text = "TwinSemi";
            btnTwinSemi.UseVisualStyleBackColor = true;
            btnTwinSemi.Click += btnTwinSemi_Click;
            // 
            // chkEven
            // 
            chkEven.AutoSize = true;
            chkEven.Location = new Point(12, 187);
            chkEven.Name = "chkEven";
            chkEven.Size = new Size(56, 19);
            chkEven.TabIndex = 23;
            chkEven.Text = "Evens";
            chkEven.UseVisualStyleBackColor = true;
            // 
            // chkDig
            // 
            chkDig.AutoSize = true;
            chkDig.Location = new Point(13, 162);
            chkDig.Name = "chkDig";
            chkDig.Size = new Size(44, 19);
            chkDig.TabIndex = 22;
            chkDig.Text = "Dig";
            chkDig.UseVisualStyleBackColor = true;
            // 
            // rtbOutput
            // 
            rtbOutput.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            rtbOutput.Location = new Point(101, 96);
            rtbOutput.Name = "rtbOutput";
            rtbOutput.ReadOnly = true;
            rtbOutput.ScrollBars = RichTextBoxScrollBars.Vertical;
            rtbOutput.Size = new Size(688, 344);
            rtbOutput.TabIndex = 21;
            rtbOutput.Text = "";
            // 
            // btnStage
            // 
            btnStage.Location = new Point(13, 212);
            btnStage.Name = "btnStage";
            btnStage.Size = new Size(82, 23);
            btnStage.TabIndex = 20;
            btnStage.Text = "Stage";
            btnStage.UseVisualStyleBackColor = true;
            btnStage.Click += btnStage_Click;
            // 
            // lblStage
            // 
            lblStage.AutoSize = true;
            lblStage.Location = new Point(13, 238);
            lblStage.Name = "lblStage";
            lblStage.Size = new Size(36, 15);
            lblStage.TabIndex = 19;
            lblStage.Text = "Stage";
            // 
            // btnGo
            // 
            btnGo.Location = new Point(13, 113);
            btnGo.Name = "btnGo";
            btnGo.Size = new Size(82, 43);
            btnGo.TabIndex = 18;
            btnGo.Text = "Go!";
            btnGo.UseVisualStyleBackColor = true;
            btnGo.Click += btnGo_Click;
            // 
            // lblLen
            // 
            lblLen.AutoSize = true;
            lblLen.Font = new Font("Segoe UI", 18F);
            lblLen.Location = new Point(13, 56);
            lblLen.Name = "lblLen";
            lblLen.Size = new Size(52, 32);
            lblLen.TabIndex = 17;
            lblLen.Text = "Len";
            // 
            // txtLen
            // 
            txtLen.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtLen.Location = new Point(101, 67);
            txtLen.Name = "txtLen";
            txtLen.Size = new Size(688, 23);
            txtLen.TabIndex = 16;
            txtLen.Text = "1550";
            // 
            // lblStart
            // 
            lblStart.AutoSize = true;
            lblStart.Font = new Font("Segoe UI", 18F);
            lblStart.Location = new Point(13, 11);
            lblStart.Name = "lblStart";
            lblStart.Size = new Size(62, 32);
            lblStart.TabIndex = 15;
            lblStart.Text = "Start";
            // 
            // txtStart
            // 
            txtStart.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtStart.Location = new Point(101, 22);
            txtStart.Name = "txtStart";
            txtStart.Size = new Size(688, 23);
            txtStart.TabIndex = 14;
            txtStart.Text = "18361375334787046697";
            txtStart.DoubleClick += txtStart_DoubleClick;
            // 
            // TestPrime10Form
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(btnDig);
            Controls.Add(btnTwinSemi);
            Controls.Add(chkEven);
            Controls.Add(chkDig);
            Controls.Add(rtbOutput);
            Controls.Add(btnStage);
            Controls.Add(lblStage);
            Controls.Add(btnGo);
            Controls.Add(lblLen);
            Controls.Add(txtLen);
            Controls.Add(lblStart);
            Controls.Add(txtStart);
            Name = "TestPrime10Form";
            Text = "TestPrime10Form";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btnDig;
        private Button btnTwinSemi;
        private CheckBox chkEven;
        private CheckBox chkDig;
        private RichTextBox rtbOutput;
        private Button btnStage;
        private Label lblStage;
        private Button btnGo;
        private Label lblLen;
        private TextBox txtLen;
        private Label lblStart;
        private TextBox txtStart;
    }
}
