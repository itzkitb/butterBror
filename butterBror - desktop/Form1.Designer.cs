namespace butterBror___desktop
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
            panel1 = new Panel();
            panel3 = new Panel();
            groupBox1 = new GroupBox();
            filesGrid = new TableLayoutPanel();
            cache_reads = new GroupBox();
            cache_writes = new GroupBox();
            files_read = new GroupBox();
            files_writes = new GroupBox();
            files_checks = new GroupBox();
            files_operations = new GroupBox();
            groupBox2 = new GroupBox();
            tableLayoutPanel1 = new TableLayoutPanel();
            groupBox3 = new GroupBox();
            panel4 = new Panel();
            kernel_console = new Label();
            groupBox4 = new GroupBox();
            panel5 = new Panel();
            main_console = new Label();
            groupBox5 = new GroupBox();
            panel6 = new Panel();
            errors_console = new Label();
            groupBox6 = new GroupBox();
            panel7 = new Panel();
            nbw_console = new Label();
            groupBox7 = new GroupBox();
            panel8 = new Panel();
            info_console = new Label();
            groupBox8 = new GroupBox();
            panel9 = new Panel();
            chat_console = new Label();
            groupBox9 = new GroupBox();
            panel10 = new Panel();
            cafus_console = new Label();
            tableLayoutPanel2 = new TableLayoutPanel();
            ram_status = new GroupBox();
            cpu_status = new GroupBox();
            messages_per_second = new GroupBox();
            operations_per_second = new GroupBox();
            panel2 = new Panel();
            version = new Label();
            label1 = new Label();
            pictureBox1 = new PictureBox();
            panel1.SuspendLayout();
            panel3.SuspendLayout();
            groupBox1.SuspendLayout();
            filesGrid.SuspendLayout();
            groupBox2.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            groupBox3.SuspendLayout();
            panel4.SuspendLayout();
            groupBox4.SuspendLayout();
            panel5.SuspendLayout();
            groupBox5.SuspendLayout();
            panel6.SuspendLayout();
            groupBox6.SuspendLayout();
            panel7.SuspendLayout();
            groupBox7.SuspendLayout();
            panel8.SuspendLayout();
            groupBox8.SuspendLayout();
            panel9.SuspendLayout();
            groupBox9.SuspendLayout();
            panel10.SuspendLayout();
            tableLayoutPanel2.SuspendLayout();
            panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.BackColor = Color.FromArgb(32, 33, 37);
            panel1.Controls.Add(panel3);
            panel1.Controls.Add(panel2);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(1126, 450);
            panel1.TabIndex = 0;
            // 
            // panel3
            // 
            panel3.AutoScroll = true;
            panel3.Controls.Add(groupBox1);
            panel3.Controls.Add(groupBox2);
            panel3.Controls.Add(tableLayoutPanel2);
            panel3.Dock = DockStyle.Fill;
            panel3.Location = new Point(0, 56);
            panel3.Name = "panel3";
            panel3.Padding = new Padding(8);
            panel3.Size = new Size(1126, 394);
            panel3.TabIndex = 1;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(filesGrid);
            groupBox1.Dock = DockStyle.Top;
            groupBox1.ForeColor = SystemColors.Control;
            groupBox1.Location = new Point(8, 1614);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(1093, 300);
            groupBox1.TabIndex = 1;
            groupBox1.TabStop = false;
            groupBox1.Text = "Файлы";
            // 
            // filesGrid
            // 
            filesGrid.ColumnCount = 3;
            filesGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3349876F));
            filesGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3316879F));
            filesGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333321F));
            filesGrid.Controls.Add(cache_reads, 0, 0);
            filesGrid.Controls.Add(cache_writes, 1, 0);
            filesGrid.Controls.Add(files_read, 2, 0);
            filesGrid.Controls.Add(files_writes, 0, 1);
            filesGrid.Controls.Add(files_checks, 1, 1);
            filesGrid.Controls.Add(files_operations, 2, 1);
            filesGrid.Dock = DockStyle.Fill;
            filesGrid.Location = new Point(3, 19);
            filesGrid.Name = "filesGrid";
            filesGrid.RowCount = 2;
            filesGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            filesGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            filesGrid.Size = new Size(1087, 278);
            filesGrid.TabIndex = 0;
            // 
            // cache_reads
            // 
            cache_reads.Dock = DockStyle.Fill;
            cache_reads.ForeColor = SystemColors.Control;
            cache_reads.Location = new Point(3, 3);
            cache_reads.Name = "cache_reads";
            cache_reads.Size = new Size(356, 133);
            cache_reads.TabIndex = 0;
            cache_reads.TabStop = false;
            cache_reads.Text = "Cache чтение";
            // 
            // cache_writes
            // 
            cache_writes.Dock = DockStyle.Fill;
            cache_writes.ForeColor = SystemColors.Control;
            cache_writes.Location = new Point(365, 3);
            cache_writes.Name = "cache_writes";
            cache_writes.Size = new Size(356, 133);
            cache_writes.TabIndex = 1;
            cache_writes.TabStop = false;
            cache_writes.Text = "Cache запись";
            // 
            // files_read
            // 
            files_read.Dock = DockStyle.Fill;
            files_read.ForeColor = SystemColors.Control;
            files_read.Location = new Point(727, 3);
            files_read.Name = "files_read";
            files_read.Size = new Size(357, 133);
            files_read.TabIndex = 2;
            files_read.TabStop = false;
            files_read.Text = "Чтение файлов";
            // 
            // files_writes
            // 
            files_writes.Dock = DockStyle.Fill;
            files_writes.ForeColor = SystemColors.Control;
            files_writes.Location = new Point(3, 142);
            files_writes.Name = "files_writes";
            files_writes.Size = new Size(356, 133);
            files_writes.TabIndex = 3;
            files_writes.TabStop = false;
            files_writes.Text = "Запись файлов";
            // 
            // files_checks
            // 
            files_checks.Dock = DockStyle.Fill;
            files_checks.ForeColor = SystemColors.Control;
            files_checks.Location = new Point(365, 142);
            files_checks.Name = "files_checks";
            files_checks.Size = new Size(356, 133);
            files_checks.TabIndex = 4;
            files_checks.TabStop = false;
            files_checks.Text = "Проверок файлов";
            // 
            // files_operations
            // 
            files_operations.Dock = DockStyle.Fill;
            files_operations.ForeColor = SystemColors.Control;
            files_operations.Location = new Point(727, 142);
            files_operations.Name = "files_operations";
            files_operations.Size = new Size(357, 133);
            files_operations.TabIndex = 5;
            files_operations.TabStop = false;
            files_operations.Text = "Всего операций";
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(tableLayoutPanel1);
            groupBox2.Dock = DockStyle.Top;
            groupBox2.ForeColor = SystemColors.Control;
            groupBox2.Location = new Point(8, 334);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(1093, 1280);
            groupBox2.TabIndex = 2;
            groupBox2.TabStop = false;
            groupBox2.Text = "Консоли";
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 2;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.Controls.Add(groupBox3, 0, 0);
            tableLayoutPanel1.Controls.Add(groupBox4, 1, 0);
            tableLayoutPanel1.Controls.Add(groupBox5, 0, 1);
            tableLayoutPanel1.Controls.Add(groupBox6, 1, 1);
            tableLayoutPanel1.Controls.Add(groupBox7, 0, 2);
            tableLayoutPanel1.Controls.Add(groupBox8, 1, 2);
            tableLayoutPanel1.Controls.Add(groupBox9, 0, 3);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(3, 19);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 4;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            tableLayoutPanel1.Size = new Size(1087, 1258);
            tableLayoutPanel1.TabIndex = 1;
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(panel4);
            groupBox3.Dock = DockStyle.Fill;
            groupBox3.ForeColor = SystemColors.Control;
            groupBox3.Location = new Point(3, 3);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new Size(537, 308);
            groupBox3.TabIndex = 1;
            groupBox3.TabStop = false;
            groupBox3.Text = "KERNEL";
            // 
            // panel4
            // 
            panel4.AutoScroll = true;
            panel4.Controls.Add(kernel_console);
            panel4.Dock = DockStyle.Fill;
            panel4.Location = new Point(3, 19);
            panel4.Name = "panel4";
            panel4.Size = new Size(531, 286);
            panel4.TabIndex = 1;
            // 
            // kernel_console
            // 
            kernel_console.AutoSize = true;
            kernel_console.Font = new Font("Consolas", 9F);
            kernel_console.Location = new Point(0, 0);
            kernel_console.Name = "kernel_console";
            kernel_console.Size = new Size(0, 14);
            kernel_console.TabIndex = 0;
            kernel_console.Click += kernel_console_Click;
            // 
            // groupBox4
            // 
            groupBox4.Controls.Add(panel5);
            groupBox4.Dock = DockStyle.Fill;
            groupBox4.ForeColor = SystemColors.Control;
            groupBox4.Location = new Point(546, 3);
            groupBox4.Name = "groupBox4";
            groupBox4.Size = new Size(538, 308);
            groupBox4.TabIndex = 2;
            groupBox4.TabStop = false;
            groupBox4.Text = "Главная";
            // 
            // panel5
            // 
            panel5.AutoScroll = true;
            panel5.Controls.Add(main_console);
            panel5.Dock = DockStyle.Fill;
            panel5.Location = new Point(3, 19);
            panel5.Name = "panel5";
            panel5.Size = new Size(532, 286);
            panel5.TabIndex = 2;
            // 
            // main_console
            // 
            main_console.AutoSize = true;
            main_console.Font = new Font("Consolas", 9F);
            main_console.Location = new Point(0, 0);
            main_console.Name = "main_console";
            main_console.Size = new Size(0, 14);
            main_console.TabIndex = 0;
            main_console.Click += main_console_Click;
            // 
            // groupBox5
            // 
            groupBox5.Controls.Add(panel6);
            groupBox5.Dock = DockStyle.Fill;
            groupBox5.ForeColor = SystemColors.Control;
            groupBox5.Location = new Point(3, 317);
            groupBox5.Name = "groupBox5";
            groupBox5.Size = new Size(537, 308);
            groupBox5.TabIndex = 3;
            groupBox5.TabStop = false;
            groupBox5.Text = "Ошибки";
            // 
            // panel6
            // 
            panel6.AutoScroll = true;
            panel6.Controls.Add(errors_console);
            panel6.Dock = DockStyle.Fill;
            panel6.Location = new Point(3, 19);
            panel6.Name = "panel6";
            panel6.Size = new Size(531, 286);
            panel6.TabIndex = 3;
            // 
            // errors_console
            // 
            errors_console.AutoSize = true;
            errors_console.Font = new Font("Consolas", 9F);
            errors_console.Location = new Point(0, 0);
            errors_console.Name = "errors_console";
            errors_console.Size = new Size(0, 14);
            errors_console.TabIndex = 0;
            errors_console.Click += errors_console_Click;
            // 
            // groupBox6
            // 
            groupBox6.Controls.Add(panel7);
            groupBox6.Dock = DockStyle.Fill;
            groupBox6.ForeColor = SystemColors.Control;
            groupBox6.Location = new Point(546, 317);
            groupBox6.Name = "groupBox6";
            groupBox6.Size = new Size(538, 308);
            groupBox6.TabIndex = 4;
            groupBox6.TabStop = false;
            groupBox6.Text = "NoBanWords";
            // 
            // panel7
            // 
            panel7.AutoScroll = true;
            panel7.Controls.Add(nbw_console);
            panel7.Dock = DockStyle.Fill;
            panel7.Location = new Point(3, 19);
            panel7.Name = "panel7";
            panel7.Size = new Size(532, 286);
            panel7.TabIndex = 3;
            // 
            // nbw_console
            // 
            nbw_console.AutoSize = true;
            nbw_console.Font = new Font("Consolas", 9F);
            nbw_console.Location = new Point(0, 0);
            nbw_console.Name = "nbw_console";
            nbw_console.Size = new Size(0, 14);
            nbw_console.TabIndex = 0;
            nbw_console.Click += nbw_console_Click;
            // 
            // groupBox7
            // 
            groupBox7.Controls.Add(panel8);
            groupBox7.Dock = DockStyle.Fill;
            groupBox7.ForeColor = SystemColors.Control;
            groupBox7.Location = new Point(3, 631);
            groupBox7.Name = "groupBox7";
            groupBox7.Size = new Size(537, 308);
            groupBox7.TabIndex = 5;
            groupBox7.TabStop = false;
            groupBox7.Text = "Информация";
            // 
            // panel8
            // 
            panel8.AutoScroll = true;
            panel8.Controls.Add(info_console);
            panel8.Dock = DockStyle.Fill;
            panel8.Location = new Point(3, 19);
            panel8.Name = "panel8";
            panel8.Size = new Size(531, 286);
            panel8.TabIndex = 3;
            // 
            // info_console
            // 
            info_console.AutoSize = true;
            info_console.Font = new Font("Consolas", 9F);
            info_console.Location = new Point(0, 0);
            info_console.Name = "info_console";
            info_console.Size = new Size(0, 14);
            info_console.TabIndex = 0;
            info_console.Click += info_console_Click;
            // 
            // groupBox8
            // 
            groupBox8.Controls.Add(panel9);
            groupBox8.Dock = DockStyle.Fill;
            groupBox8.ForeColor = SystemColors.Control;
            groupBox8.Location = new Point(546, 631);
            groupBox8.Name = "groupBox8";
            groupBox8.Size = new Size(538, 308);
            groupBox8.TabIndex = 6;
            groupBox8.TabStop = false;
            groupBox8.Text = "Чат";
            // 
            // panel9
            // 
            panel9.AutoScroll = true;
            panel9.Controls.Add(chat_console);
            panel9.Dock = DockStyle.Fill;
            panel9.Location = new Point(3, 19);
            panel9.Name = "panel9";
            panel9.Size = new Size(532, 286);
            panel9.TabIndex = 3;
            // 
            // chat_console
            // 
            chat_console.AutoSize = true;
            chat_console.Font = new Font("Consolas", 9F);
            chat_console.Location = new Point(0, 0);
            chat_console.Name = "chat_console";
            chat_console.Size = new Size(0, 14);
            chat_console.TabIndex = 0;
            chat_console.Click += chat_console_Click;
            // 
            // groupBox9
            // 
            groupBox9.Controls.Add(panel10);
            groupBox9.Dock = DockStyle.Fill;
            groupBox9.ForeColor = SystemColors.Control;
            groupBox9.Location = new Point(3, 945);
            groupBox9.Name = "groupBox9";
            groupBox9.Size = new Size(537, 310);
            groupBox9.TabIndex = 7;
            groupBox9.TabStop = false;
            groupBox9.Text = "CAFUS";
            // 
            // panel10
            // 
            panel10.AutoScroll = true;
            panel10.Controls.Add(cafus_console);
            panel10.Dock = DockStyle.Fill;
            panel10.Location = new Point(3, 19);
            panel10.Name = "panel10";
            panel10.Size = new Size(531, 288);
            panel10.TabIndex = 3;
            // 
            // cafus_console
            // 
            cafus_console.AutoSize = true;
            cafus_console.Font = new Font("Consolas", 9F);
            cafus_console.Location = new Point(0, 0);
            cafus_console.Name = "cafus_console";
            cafus_console.Size = new Size(0, 14);
            cafus_console.TabIndex = 0;
            cafus_console.Click += cafus_console_Click;
            // 
            // tableLayoutPanel2
            // 
            tableLayoutPanel2.ColumnCount = 2;
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel2.Controls.Add(ram_status, 1, 1);
            tableLayoutPanel2.Controls.Add(cpu_status, 0, 1);
            tableLayoutPanel2.Controls.Add(messages_per_second, 1, 0);
            tableLayoutPanel2.Controls.Add(operations_per_second, 0, 0);
            tableLayoutPanel2.Dock = DockStyle.Top;
            tableLayoutPanel2.Location = new Point(8, 8);
            tableLayoutPanel2.Name = "tableLayoutPanel2";
            tableLayoutPanel2.RowCount = 2;
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel2.Size = new Size(1093, 326);
            tableLayoutPanel2.TabIndex = 4;
            // 
            // ram_status
            // 
            ram_status.Dock = DockStyle.Top;
            ram_status.ForeColor = SystemColors.Control;
            ram_status.Location = new Point(549, 166);
            ram_status.Name = "ram_status";
            ram_status.Size = new Size(541, 157);
            ram_status.TabIndex = 2;
            ram_status.TabStop = false;
            ram_status.Text = "Использование RAM (МБ)";
            // 
            // cpu_status
            // 
            cpu_status.Dock = DockStyle.Top;
            cpu_status.ForeColor = SystemColors.Control;
            cpu_status.Location = new Point(3, 166);
            cpu_status.Name = "cpu_status";
            cpu_status.Size = new Size(540, 157);
            cpu_status.TabIndex = 1;
            cpu_status.TabStop = false;
            cpu_status.Text = "CPU (%)";
            // 
            // messages_per_second
            // 
            messages_per_second.Dock = DockStyle.Top;
            messages_per_second.ForeColor = SystemColors.Control;
            messages_per_second.Location = new Point(549, 3);
            messages_per_second.Name = "messages_per_second";
            messages_per_second.Size = new Size(541, 157);
            messages_per_second.TabIndex = 0;
            messages_per_second.TabStop = false;
            messages_per_second.Text = "Сообщения";
            // 
            // operations_per_second
            // 
            operations_per_second.Dock = DockStyle.Top;
            operations_per_second.ForeColor = SystemColors.Control;
            operations_per_second.Location = new Point(3, 3);
            operations_per_second.Name = "operations_per_second";
            operations_per_second.Size = new Size(540, 157);
            operations_per_second.TabIndex = 0;
            operations_per_second.TabStop = false;
            operations_per_second.Text = "Операции";
            // 
            // panel2
            // 
            panel2.BackColor = Color.FromArgb(43, 45, 51);
            panel2.Controls.Add(version);
            panel2.Controls.Add(label1);
            panel2.Controls.Add(pictureBox1);
            panel2.Dock = DockStyle.Top;
            panel2.Location = new Point(0, 0);
            panel2.Name = "panel2";
            panel2.Size = new Size(1126, 56);
            panel2.TabIndex = 0;
            // 
            // version
            // 
            version.AutoSize = true;
            version.ForeColor = SystemColors.Control;
            version.Location = new Point(146, 23);
            version.Name = "version";
            version.Size = new Size(29, 15);
            version.TabIndex = 2;
            version.Text = "v. --";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.BackColor = Color.Transparent;
            label1.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            label1.ForeColor = SystemColors.Control;
            label1.Location = new Point(51, 17);
            label1.Name = "label1";
            label1.Size = new Size(89, 21);
            label1.TabIndex = 1;
            label1.Text = "butterBror";
            // 
            // pictureBox1
            // 
            pictureBox1.BackgroundImage = Properties.Resources.logo;
            pictureBox1.BackgroundImageLayout = ImageLayout.Stretch;
            pictureBox1.Location = new Point(9, 9);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(37, 37);
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1126, 450);
            Controls.Add(panel1);
            Name = "Form1";
            Text = "Form1";
            panel1.ResumeLayout(false);
            panel3.ResumeLayout(false);
            groupBox1.ResumeLayout(false);
            filesGrid.ResumeLayout(false);
            groupBox2.ResumeLayout(false);
            tableLayoutPanel1.ResumeLayout(false);
            groupBox3.ResumeLayout(false);
            panel4.ResumeLayout(false);
            panel4.PerformLayout();
            groupBox4.ResumeLayout(false);
            panel5.ResumeLayout(false);
            panel5.PerformLayout();
            groupBox5.ResumeLayout(false);
            panel6.ResumeLayout(false);
            panel6.PerformLayout();
            groupBox6.ResumeLayout(false);
            panel7.ResumeLayout(false);
            panel7.PerformLayout();
            groupBox7.ResumeLayout(false);
            panel8.ResumeLayout(false);
            panel8.PerformLayout();
            groupBox8.ResumeLayout(false);
            panel9.ResumeLayout(false);
            panel9.PerformLayout();
            groupBox9.ResumeLayout(false);
            panel10.ResumeLayout(false);
            panel10.PerformLayout();
            tableLayoutPanel2.ResumeLayout(false);
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Panel panel1;
        private Panel panel2;
        private PictureBox pictureBox1;
        private Label label1;
        private Panel panel3;
        private GroupBox operations_per_second;
        private GroupBox groupBox1;
        private TableLayoutPanel filesGrid;
        private GroupBox cache_reads;
        private GroupBox cache_writes;
        private GroupBox files_read;
        private GroupBox files_writes;
        private GroupBox files_checks;
        private GroupBox files_operations;
        private GroupBox groupBox2;
        private TableLayoutPanel tableLayoutPanel1;
        private GroupBox groupBox3;
        private GroupBox groupBox4;
        private GroupBox groupBox5;
        private GroupBox groupBox6;
        private GroupBox groupBox7;
        private GroupBox groupBox8;
        private GroupBox groupBox9;
        private Label kernel_console;
        private Panel panel4;
        private Panel panel5;
        private Label main_console;
        private Panel panel6;
        private Label errors_console;
        private Panel panel7;
        private Label nbw_console;
        private Panel panel8;
        private Label info_console;
        private Panel panel9;
        private Label chat_console;
        private Panel panel10;
        private Label cafus_console;
        private Label version;
        private GroupBox messages_per_second;
        private TableLayoutPanel tableLayoutPanel2;
        private GroupBox ram_status;
        private GroupBox cpu_status;
    }
}
