namespace NCrawler.IFilterProcessor
{
	public class PdfIFilterProcessor : IFilterProcessor
	{
		#region Constructors

		public PdfIFilterProcessor()
			: base("application/pdf", "pdf")
		{
		}

		#endregion
	}
}