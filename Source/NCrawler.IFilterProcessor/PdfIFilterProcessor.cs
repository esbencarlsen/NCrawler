namespace NCrawler.IFilterProcessor
{
	public class PdfIFilterProcessor : FilterProcessor
	{
		#region Constructors

		public PdfIFilterProcessor()
			: base("application/pdf", "pdf")
		{
		}

		#endregion
	}
}