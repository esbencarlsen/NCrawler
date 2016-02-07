namespace NCrawler.IEProcessor
{
	partial class TridentBrowserForm
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
			this.IEWebBrowser = new System.Windows.Forms.WebBrowser();
			this.SuspendLayout();
			// 
			// IEWebBrowser
			// 
			this.IEWebBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
			this.IEWebBrowser.Location = new System.Drawing.Point(0, 0);
			this.IEWebBrowser.MinimumSize = new System.Drawing.Size(20, 20);
			this.IEWebBrowser.Name = "IEWebBrowser";
			this.IEWebBrowser.Size = new System.Drawing.Size(284, 262);
			this.IEWebBrowser.TabIndex = 0;
			// 
			// TridentBrowserForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(284, 262);
			this.Controls.Add(this.IEWebBrowser);
			this.Name = "TridentBrowserForm";
			this.Text = "TridentBrowserForm";
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.WebBrowser IEWebBrowser;
	}
}