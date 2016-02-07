namespace NCrawler.IFilterProcessor
{
	public class RtfIFilterProcessor : IFilterProcessor
	{
		#region Constructors

		public RtfIFilterProcessor()
			: base("application/rtf", "rtf")
		{
			m_MimeTypeExtensionMapping.Add("application/x-rtf", "rtf");
			m_MimeTypeExtensionMapping.Add("text/richtext", "rtf");
		}

		#endregion
	}
}