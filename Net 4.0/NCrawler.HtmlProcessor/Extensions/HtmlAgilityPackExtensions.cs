using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using HtmlAgilityPack;

using NCrawler.Extensions;
using NCrawler.Utils;

namespace NCrawler.HtmlProcessor.Extensions
{
	public static class HtmlToText
	{
		#region Class Methods

		public static string ExtractText(this HtmlDocument htmlDocument)
		{
			using (StringWriter sw = new StringWriter(CultureInfo.InvariantCulture))
			{
				ConvertTo(htmlDocument.DocumentNode, sw);
				sw.Flush();
				return sw.ToString();
			}
		}

		public static DocumentWithLinks GetLinks(this HtmlDocument htmlDocument)
		{
			return new DocumentWithLinks(htmlDocument);
		}

		private static void ConvertContentTo(HtmlNode node, TextWriter outText)
		{
			foreach (HtmlNode subnode in node.ChildNodes)
			{
				ConvertTo(subnode, outText);
			}
		}

		private static void ConvertTo(HtmlNode node, TextWriter outText)
		{
			string html;
			switch (node.NodeType)
			{
				case HtmlNodeType.Comment:
					// don't output comments
					break;

				case HtmlNodeType.Document:
					ConvertContentTo(node, outText);
					break;

				case HtmlNodeType.Text:
					// script and style must not be output
					string parentName = node.ParentNode.Name;
					if ((parentName == "script") || (parentName == "style"))
						break;

					// get text
					html = ((HtmlTextNode) node).Text;

					// is it in fact a special closing node output as text?
					if (HtmlNode.IsOverlappedClosingElement(html))
						break;

					// check the text is meaningful and not a bunch of whitespaces
					if (html.Trim().Length > 0)
					{
						outText.Write(HtmlEntity.DeEntitize(html));
						outText.Write(" ");
					}
					break;

				case HtmlNodeType.Element:
					switch (node.Name)
					{
						case "p":
							// treat paragraphs as crlf
							outText.Write("\r\n");
							break;
					}

					if (node.HasChildNodes)
					{
						ConvertContentTo(node, outText);
					}
					break;
			}
		}

		#endregion
	}

	public class DocumentWithLinks
	{
		#region Readonly & Static Fields

		private readonly HtmlDocument m_Doc;

		#endregion

		#region Constructors

		/// <summary>
		/// Creates an instance of a DocumentWithLinkedFiles.
		/// </summary>
		/// <param name="doc">The input HTML document. May not be null.</param>
		public DocumentWithLinks(HtmlDocument doc)
		{
			AspectF.Define.
				NotNull(doc, "doc");

			m_Doc = doc;
			GetLinks();
			GetReferences();
		}

		#endregion

		#region Instance Properties

		/// <summary>
		/// Gets a list of links as they are declared in the HTML document.
		/// </summary>
		public IEnumerable<string> Links { get; private set; }

		/// <summary>
		/// Gets a list of reference links to other HTML documents, as they are declared in the HTML document.
		/// </summary>
		public IEnumerable<string> References { get; private set; }

		#endregion

		#region Instance Methods

		private void GetLinks()
		{
			HtmlNodeCollection atts = m_Doc.DocumentNode.SelectNodes("//*[@background or @lowsrc or @src or @href or @action]");
			if (atts.IsNull())
			{
				Links = new string[0];
				return;
			}

			Links = atts.
				SelectMany(n => new[]
					{
						ParseLink(n, "background"),
						ParseLink(n, "href"),
						ParseLink(n, "src"),
						ParseLink(n, "lowsrc"),
						ParseLink(n, "action"),
					}).
				Distinct().
				ToArray();
		}

		private void GetReferences()
		{
			HtmlNodeCollection hrefs = m_Doc.DocumentNode.SelectNodes("//a[@href]");
			if (hrefs.IsNull())
			{
				References = new string[0];
				return;
			}

			References = hrefs.
				Select(href => href.Attributes["href"].Value).
				Distinct().
				ToArray();
		}

		#endregion

		#region Class Methods

		private static string ParseLink(HtmlNode node, string name)
		{
			HtmlAttribute att = node.Attributes[name];
			if (att.IsNull())
			{
				return null;
			}

			// if name = href, we are only interested by <link> tags
			if ((name == "href") && (node.Name != "link"))
			{
				return null;
			}

			return att.Value;
		}

		#endregion
	}
}