namespace HFT
{
    partial class Alpha
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.Label label10;
            this.button1 = new System.Windows.Forms.Button();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.checkBox2 = new System.Windows.Forms.CheckBox();
            this.button2 = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel16 = new System.Windows.Forms.TableLayoutPanel();
            this.checkBoxCiccSeed2 = new System.Windows.Forms.CheckBox();
            this.checkBoxCiccSeed1 = new System.Windows.Forms.CheckBox();
            this.checkBoxGuosenHedge1 = new System.Windows.Forms.CheckBox();
            this.checkBoxHY = new System.Windows.Forms.CheckBox();
            this.checkBoxMarketOrder = new System.Windows.Forms.CheckBox();
            label10 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.tableLayoutPanel16.SuspendLayout();
            this.SuspendLayout();
            // 
            // label10
            // 
            label10.Anchor = System.Windows.Forms.AnchorStyles.None;
            label10.AutoSize = true;
            label10.Location = new System.Drawing.Point(62, 8);
            label10.Name = "label10";
            label10.Size = new System.Drawing.Size(29, 12);
            label10.TabIndex = 0;
            label10.Text = "策略";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(421, 524);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(155, 52);
            this.button1.TabIndex = 1;
            this.button1.Text = "下单";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // dataGridView1
            // 
            this.dataGridView1.CausesValidation = false;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(260, 28);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowTemplate.Height = 23;
            this.dataGridView1.Size = new System.Drawing.Size(539, 413);
            this.dataGridView1.TabIndex = 2;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.tableLayoutPanel2);
            this.groupBox1.Location = new System.Drawing.Point(16, 28);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(227, 143);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "调仓策略";
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 1;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.Controls.Add(this.checkBox1, 0, 1);
            this.tableLayoutPanel2.Controls.Add(label10, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.checkBox2, 0, 2);
            this.tableLayoutPanel2.Location = new System.Drawing.Point(14, 18);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 4;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(153, 119);
            this.tableLayoutPanel2.TabIndex = 0;
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Checked = true;
            this.checkBox1.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBox1.Location = new System.Drawing.Point(3, 32);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(144, 16);
            this.checkBox1.TabIndex = 6;
            this.checkBox1.Text = "alpha_mamount_TotalA";
            this.checkBox1.UseVisualStyleBackColor = true;
            // 
            // checkBox2
            // 
            this.checkBox2.AutoSize = true;
            this.checkBox2.Checked = true;
            this.checkBox2.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBox2.Location = new System.Drawing.Point(3, 61);
            this.checkBox2.Name = "checkBox2";
            this.checkBox2.Size = new System.Drawing.Size(138, 16);
            this.checkBox2.TabIndex = 7;
            this.checkBox2.Text = "alpha_mamount_ZZ500";
            this.checkBox2.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(55, 524);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(150, 52);
            this.button2.TabIndex = 5;
            this.button2.Text = "计算";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.tableLayoutPanel16);
            this.groupBox2.Location = new System.Drawing.Point(16, 219);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(213, 177);
            this.groupBox2.TabIndex = 6;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "调仓账户";
            // 
            // tableLayoutPanel16
            // 
            this.tableLayoutPanel16.ColumnCount = 1;
            this.tableLayoutPanel16.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel16.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel16.Controls.Add(this.checkBoxCiccSeed2, 0, 2);
            this.tableLayoutPanel16.Controls.Add(this.checkBoxCiccSeed1, 0, 1);
            this.tableLayoutPanel16.Controls.Add(this.checkBoxGuosenHedge1, 0, 0);
            this.tableLayoutPanel16.Controls.Add(this.checkBoxHY, 0, 3);
            this.tableLayoutPanel16.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel16.Location = new System.Drawing.Point(3, 17);
            this.tableLayoutPanel16.Name = "tableLayoutPanel16";
            this.tableLayoutPanel16.RowCount = 4;
            this.tableLayoutPanel16.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel16.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel16.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel16.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel16.Size = new System.Drawing.Size(207, 157);
            this.tableLayoutPanel16.TabIndex = 1;
            // 
            // checkBoxCiccSeed2
            // 
            this.checkBoxCiccSeed2.AutoSize = true;
            this.checkBoxCiccSeed2.Checked = true;
            this.checkBoxCiccSeed2.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxCiccSeed2.Location = new System.Drawing.Point(3, 81);
            this.checkBoxCiccSeed2.Name = "checkBoxCiccSeed2";
            this.checkBoxCiccSeed2.Size = new System.Drawing.Size(66, 16);
            this.checkBoxCiccSeed2.TabIndex = 4;
            this.checkBoxCiccSeed2.Text = "种子2号";
            this.checkBoxCiccSeed2.UseVisualStyleBackColor = true;
            // 
            // checkBoxCiccSeed1
            // 
            this.checkBoxCiccSeed1.AutoSize = true;
            this.checkBoxCiccSeed1.Checked = true;
            this.checkBoxCiccSeed1.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxCiccSeed1.Location = new System.Drawing.Point(3, 42);
            this.checkBoxCiccSeed1.Name = "checkBoxCiccSeed1";
            this.checkBoxCiccSeed1.Size = new System.Drawing.Size(66, 16);
            this.checkBoxCiccSeed1.TabIndex = 3;
            this.checkBoxCiccSeed1.Text = "种子1号";
            this.checkBoxCiccSeed1.UseVisualStyleBackColor = true;
            // 
            // checkBoxGuosenHedge1
            // 
            this.checkBoxGuosenHedge1.AutoSize = true;
            this.checkBoxGuosenHedge1.Checked = true;
            this.checkBoxGuosenHedge1.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxGuosenHedge1.Location = new System.Drawing.Point(3, 3);
            this.checkBoxGuosenHedge1.Name = "checkBoxGuosenHedge1";
            this.checkBoxGuosenHedge1.Size = new System.Drawing.Size(114, 16);
            this.checkBoxGuosenHedge1.TabIndex = 0;
            this.checkBoxGuosenHedge1.Text = "国信量化对冲1号";
            this.checkBoxGuosenHedge1.UseVisualStyleBackColor = true;
            // 
            // checkBoxHY
            // 
            this.checkBoxHY.AutoSize = true;
            this.checkBoxHY.Checked = true;
            this.checkBoxHY.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxHY.Location = new System.Drawing.Point(3, 120);
            this.checkBoxHY.Name = "checkBoxHY";
            this.checkBoxHY.Size = new System.Drawing.Size(36, 16);
            this.checkBoxHY.TabIndex = 1;
            this.checkBoxHY.Text = "HY";
            this.checkBoxHY.UseVisualStyleBackColor = true;
            // 
            // checkBoxMarketOrder
            // 
            this.checkBoxMarketOrder.AutoSize = true;
            this.checkBoxMarketOrder.Checked = true;
            this.checkBoxMarketOrder.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxMarketOrder.Location = new System.Drawing.Point(55, 439);
            this.checkBoxMarketOrder.Name = "checkBoxMarketOrder";
            this.checkBoxMarketOrder.Size = new System.Drawing.Size(72, 16);
            this.checkBoxMarketOrder.TabIndex = 7;
            this.checkBoxMarketOrder.Text = "市价下单";
            this.checkBoxMarketOrder.UseVisualStyleBackColor = true;
            // 
            // Alpha
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(867, 613);
            this.Controls.Add(this.checkBoxMarketOrder);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.button1);
            this.Name = "Alpha";
            this.Text = "Form1";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Alpha_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.tableLayoutPanel16.ResumeLayout(false);
            this.tableLayoutPanel16.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.CheckBox checkBox2;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel16;
        private System.Windows.Forms.CheckBox checkBoxCiccSeed2;
        private System.Windows.Forms.CheckBox checkBoxCiccSeed1;
        private System.Windows.Forms.CheckBox checkBoxGuosenHedge1;
        private System.Windows.Forms.CheckBox checkBoxHY;
        private System.Windows.Forms.CheckBox checkBoxMarketOrder;
    }
}

