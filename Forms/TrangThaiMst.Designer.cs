namespace Thue.Forms
{
	partial class TrangThaiMst
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
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
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			toolStrip1 = new ToolStrip();
			btnStartStop = new ToolStripButton();
			toolStripSeparator1 = new ToolStripSeparator();
			btnImport = new ToolStripButton();
			groupBox1 = new GroupBox();
			dataGrid = new DataGridView();
			label1 = new Label();
			txtRetryOnError = new TextBox();
			label2 = new Label();
			txtThreadCount = new TextBox();
			statusStrip1 = new StatusStrip();
			lblStatus = new ToolStripStatusLabel();
			toolStrip1.SuspendLayout();
			groupBox1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)dataGrid).BeginInit();
			statusStrip1.SuspendLayout();
			SuspendLayout();
			// 
			// toolStrip1
			// 
			toolStrip1.Items.AddRange(new ToolStripItem[] { btnStartStop, toolStripSeparator1, btnImport });
			toolStrip1.Location = new Point(0, 0);
			toolStrip1.Name = "toolStrip1";
			toolStrip1.Size = new Size(1264, 39);
			toolStrip1.TabIndex = 0;
			toolStrip1.Text = "toolStrip1";
			// 
			// btnStartStop
			// 
			btnStartStop.Image = Properties.Resources.start;
			btnStartStop.ImageScaling = ToolStripItemImageScaling.None;
			btnStartStop.ImageTransparentColor = Color.Magenta;
			btnStartStop.Name = "btnStartStop";
			btnStartStop.Size = new Size(95, 36);
			btnStartStop.Text = "  Bắt đầu  ";
			btnStartStop.Click += BtnStartStop_Click;
			// 
			// toolStripSeparator1
			// 
			toolStripSeparator1.Name = "toolStripSeparator1";
			toolStripSeparator1.Size = new Size(6, 39);
			// 
			// btnImport
			// 
			btnImport.Image = Properties.Resources.sheets;
			btnImport.ImageScaling = ToolStripItemImageScaling.None;
			btnImport.ImageTransparentColor = Color.Magenta;
			btnImport.Name = "btnImport";
			btnImport.Size = new Size(120, 36);
			btnImport.Text = "  Import excel  ";
			btnImport.Click += BtnImport_Click;
			// 
			// groupBox1
			// 
			groupBox1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			groupBox1.Controls.Add(dataGrid);
			groupBox1.Location = new Point(12, 73);
			groupBox1.Name = "groupBox1";
			groupBox1.Size = new Size(1240, 463);
			groupBox1.TabIndex = 1;
			groupBox1.TabStop = false;
			groupBox1.Text = "Danh sách";
			// 
			// dataGrid
			// 
			dataGrid.AllowUserToAddRows = false;
			dataGrid.AllowUserToDeleteRows = false;
			dataGrid.AllowUserToResizeRows = false;
			dataGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			dataGrid.Dock = DockStyle.Fill;
			dataGrid.Location = new Point(3, 19);
			dataGrid.Name = "dataGrid";
			dataGrid.RowHeadersVisible = false;
			dataGrid.Size = new Size(1234, 441);
			dataGrid.TabIndex = 0;
			// 
			// label1
			// 
			label1.AutoSize = true;
			label1.Location = new Point(15, 48);
			label1.Name = "label1";
			label1.Size = new Size(78, 15);
			label1.TabIndex = 2;
			label1.Text = "Thử lại khi lỗi";
			// 
			// txtRetryOnError
			// 
			txtRetryOnError.Location = new Point(110, 44);
			txtRetryOnError.Name = "txtRetryOnError";
			txtRetryOnError.Size = new Size(45, 23);
			txtRetryOnError.TabIndex = 3;
			txtRetryOnError.Text = "10";
			// 
			// label2
			// 
			label2.AutoSize = true;
			label2.Location = new Point(180, 48);
			label2.Name = "label2";
			label2.Size = new Size(89, 15);
			label2.TabIndex = 2;
			label2.Text = "Chạy đồng thời";
			// 
			// txtThreadCount
			// 
			txtThreadCount.Location = new Point(275, 44);
			txtThreadCount.Name = "txtThreadCount";
			txtThreadCount.Size = new Size(45, 23);
			txtThreadCount.TabIndex = 3;
			txtThreadCount.Text = "1";
			// 
			// statusStrip1
			// 
			statusStrip1.Items.AddRange(new ToolStripItem[] { lblStatus });
			statusStrip1.Location = new Point(0, 539);
			statusStrip1.Name = "statusStrip1";
			statusStrip1.Size = new Size(1264, 22);
			statusStrip1.TabIndex = 4;
			statusStrip1.Text = "statusStrip1";
			// 
			// lblStatus
			// 
			lblStatus.Name = "lblStatus";
			lblStatus.Size = new Size(39, 17);
			lblStatus.Text = "Status";
			// 
			// TrangThaiMst
			// 
			AutoScaleDimensions = new SizeF(7F, 15F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(1264, 561);
			Controls.Add(statusStrip1);
			Controls.Add(txtThreadCount);
			Controls.Add(label2);
			Controls.Add(txtRetryOnError);
			Controls.Add(label1);
			Controls.Add(groupBox1);
			Controls.Add(toolStrip1);
			Name = "TrangThaiMst";
			Text = "Trạng thái Mã số thuế";
			toolStrip1.ResumeLayout(false);
			toolStrip1.PerformLayout();
			groupBox1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)dataGrid).EndInit();
			statusStrip1.ResumeLayout(false);
			statusStrip1.PerformLayout();
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion

		private ToolStrip toolStrip1;
		private ToolStripButton btnStartStop;
		private ToolStripSeparator toolStripSeparator1;
		private ToolStripButton btnImport;
		private GroupBox groupBox1;
		private DataGridView dataGrid;
		private Label label1;
		private TextBox txtRetryOnError;
		private Label label2;
		private TextBox txtThreadCount;
		private StatusStrip statusStrip1;
		private ToolStripStatusLabel lblStatus;
	}
}