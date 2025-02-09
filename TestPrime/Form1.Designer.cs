namespace TestPrime
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
            textBox1 = new TextBox();
            label1 = new Label();
            label2 = new Label();
            textBox2 = new TextBox();
            button1 = new Button();
            label3 = new Label();
            button2 = new Button();
            richTextBox1 = new RichTextBox();
            chkDig = new CheckBox();
            chkEven = new CheckBox();
            button3 = new Button();
            btnDig = new Button();
            SuspendLayout();
            // 
            // textBox1
            // 
            textBox1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textBox1.Location = new Point(100, 20);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(688, 23);
            textBox1.TabIndex = 0;
            textBox1.Text = "18361375334787046697";
            textBox1.TextChanged += textBox1_TextChanged;
            textBox1.DoubleClick += textBox1_DoubleClick;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 18F);
            label1.Location = new Point(12, 9);
            label1.Name = "label1";
            label1.Size = new Size(62, 32);
            label1.TabIndex = 1;
            label1.Text = "Start";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 18F);
            label2.Location = new Point(12, 54);
            label2.Name = "label2";
            label2.Size = new Size(52, 32);
            label2.TabIndex = 3;
            label2.Text = "Len";
            // 
            // textBox2
            // 
            textBox2.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textBox2.Location = new Point(100, 65);
            textBox2.Name = "textBox2";
            textBox2.Size = new Size(688, 23);
            textBox2.TabIndex = 2;
            textBox2.Text = "1550";
            // 
            // button1
            // 
            button1.Location = new Point(12, 111);
            button1.Name = "button1";
            button1.Size = new Size(82, 43);
            button1.TabIndex = 5;
            button1.Text = "Go!";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(12, 236);
            label3.Name = "label3";
            label3.Size = new Size(36, 15);
            label3.TabIndex = 7;
            label3.Text = "Stage";
            // 
            // button2
            // 
            button2.Location = new Point(12, 210);
            button2.Name = "button2";
            button2.Size = new Size(82, 23);
            button2.TabIndex = 8;
            button2.Text = "Stage";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // richTextBox1
            // 
            richTextBox1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            richTextBox1.Location = new Point(100, 94);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.ReadOnly = true;
            richTextBox1.ScrollBars = RichTextBoxScrollBars.Vertical;
            richTextBox1.Size = new Size(688, 344);
            richTextBox1.TabIndex = 9;
            richTextBox1.Text = "";
            // 
            // chkDig
            // 
            chkDig.AutoSize = true;
            chkDig.Location = new Point(12, 160);
            chkDig.Name = "chkDig";
            chkDig.Size = new Size(44, 19);
            chkDig.TabIndex = 10;
            chkDig.Text = "Dig";
            chkDig.UseVisualStyleBackColor = true;
            // 
            // chkEven
            // 
            chkEven.AutoSize = true;
            chkEven.Location = new Point(11, 185);
            chkEven.Name = "chkEven";
            chkEven.Size = new Size(56, 19);
            chkEven.TabIndex = 11;
            chkEven.Text = "Evens";
            chkEven.UseVisualStyleBackColor = true;
            // 
            // button3
            // 
            button3.Location = new Point(12, 265);
            button3.Name = "button3";
            button3.Size = new Size(75, 23);
            button3.TabIndex = 12;
            button3.Text = "TwinSemi";
            button3.UseVisualStyleBackColor = true;
            button3.Click += button3_Click;
            // 
            // btnDig
            // 
            btnDig.Location = new Point(12, 294);
            btnDig.Name = "btnDig";
            btnDig.Size = new Size(75, 23);
            btnDig.TabIndex = 13;
            btnDig.Text = "Dig";
            btnDig.UseVisualStyleBackColor = true;
            btnDig.Click += btnDig_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(btnDig);
            Controls.Add(button3);
            Controls.Add(chkEven);
            Controls.Add(chkDig);
            Controls.Add(richTextBox1);
            Controls.Add(button2);
            Controls.Add(label3);
            Controls.Add(button1);
            Controls.Add(label2);
            Controls.Add(textBox2);
            Controls.Add(label1);
            Controls.Add(textBox1);
            Name = "Form1";
            Text = "Form1";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox textBox1;
        private Label label1;
        private Label label2;
        private TextBox textBox2;
        private Button button1;
        private Label label3;
        private Button button2;
        private RichTextBox richTextBox1;
        private CheckBox chkDig;
        private CheckBox chkEven;
        private Button button3;
        private Button btnDig;
    }
}