namespace NCrawler.IFilterProcessor
{
	public class RtfIFilterProcessor : FilterProcessor
	{
		#region Constructors

		public RtfIFilterProcessor()
			: base("application/rtf", "rtf")
		{
			MimeTypeExtensionMapping.Add("application/x-rtf", "rtf");
			MimeTypeExtensionMapping.Add("text/richtext", "rtf");
		}

		#endregion
	}
}