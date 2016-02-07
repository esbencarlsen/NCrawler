namespace NCrawler.IFilterProcessor
{
	public class ExcelIFilterProcessor : IFilterProcessor
	{
		#region Constructors

		public ExcelIFilterProcessor()
			: base("application/excel", "xls")
		{
			m_MimeTypeExtensionMapping.Add("application/vnd.ms-excel", "xsl");
			m_MimeTypeExtensionMapping.Add("application/x-excel", "xsl");
			m_MimeTypeExtensionMapping.Add("application/x-msexcel", "xsl");
		}

		#endregion
	}
}