using System;
using System.Drawing;
using System.Windows.Forms;

using Skybound.Gecko;

namespace NCrawler.GeckoProcessor
{
	public partial class GeckoBrowserForm : Form
	{
		#region Readonly & Static Fields

		private readonly GeckoWebBrowser m_GeckoWebBrowser = new GeckoWebBrowser();
		private readonly string m_Url;
		private static bool s_IsXulrunnerInitialized;

		#endregion

		#region Constructors

		static GeckoBrowserForm()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
		}

		public GeckoBrowserForm(string xulRunnerPath, string url)
		{
			InitializeXulRunner(xulRunnerPath);
			m_Url = url;
			FormBorderStyle = FormBorderStyle.FixedToolWindow;
			ShowInTaskbar = false;
			StartPosition = FormStartPosition.Manual;
			Location = new Point(-20000, -20000);
			Size = new Size(1, 1);
			Done = false;
			InitializeComponent();
		}

		#endregion

		#region Instance Properties

		public string DocumentDomHtml { get; private set; }

		public Boolean Done { get; set; }

		#endregion

		#region Instance Methods

		protected override void OnLoad(EventArgs e)
		{
			m_GeckoWebBrowser.Parent = this;
			m_GeckoWebBrowser.Dock = DockStyle.Fill;
			m_GeckoWebBrowser.DocumentCompleted += (s, ee) =>
				{
					DocumentDomHtml = m_GeckoWebBrowser.Document.DocumentElement.InnerHtml;
					if (m_Url.Equals(m_GeckoWebBrowser.Document.Url.ToString(), StringComparison.OrdinalIgnoreCase))
					{
						Done = true;
					}
				};

			m_GeckoWebBrowser.Navigate(m_Url);
		}

		#endregion

		#region Class Methods

		private static void InitializeXulRunner(string path)
		{
			if (s_IsXulrunnerInitialized)
			{
				return;
			}

			s_IsXulrunnerInitialized = true;
			Xpcom.Initialize(path);
		}

		#endregion
	}
}