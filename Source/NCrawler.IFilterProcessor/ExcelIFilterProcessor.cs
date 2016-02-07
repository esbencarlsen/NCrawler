namespace NCrawler.IFilterProcessor
{
	public class ExcelIFilterProcessor : FilterProcessor
	{
		#region Constructors

		public ExcelIFilterProcessor()
			: base("application/excel", "xls")
		{
			MimeTypeExtensionMapping.Add("application/vnd.ms-excel", "xsl");
			MimeTypeExtensionMapping.Add("application/x-excel", "xsl");
			MimeTypeExtensionMapping.Add("application/x-msexcel", "xsl");
		}

		#endregion
	}
}