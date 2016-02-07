using System;
using System.Drawing;
using System.Windows.Forms;

using mshtml;

namespace NCrawler.IEProcessor
{
	public partial class TridentBrowserForm : Form
	{
		#region Readonly & Static Fields

		private readonly string m_Url;

		#endregion

		#region Constructors

		static TridentBrowserForm()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
		}

		public TridentBrowserForm(string url)
		{
			m_Url = url;
			FormBorderStyle = FormBorderStyle.FixedToolWindow;
			ShowInTaskbar = false;
			StartPosition = FormStartPosition.Manual;
			Location = new Point(-20000, -20000);
			Size = new Size(1, 1);
			InitializeComponent();
		}

		#endregion

		#region Instance Properties

		public string DocumentDomHtml { get; private set; }

		#endregion

		#region Instance Methods

		protected override void OnLoad(EventArgs e)
		{
			IEWebBrowser.DocumentCompleted += (s, ee) =>
				{
					if (IEWebBrowser.Document == null)
					{
						return;
					}

					IHTMLDocument2 htmlDocument = IEWebBrowser.Document.DomDocument as IHTMLDocument2;
					if (htmlDocument == null)
					{
						return;
					}

					if (htmlDocument.body != null && htmlDocument.body.parentElement != null)
					{
						DocumentDomHtml = htmlDocument.body.parentElement.outerHTML;
						Close();
					}
				};

			IEWebBrowser.Navigate(m_Url);
		}

		#endregion
	}
}