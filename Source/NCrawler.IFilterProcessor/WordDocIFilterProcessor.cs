namespace NCrawler.IFilterProcessor
{
	public class WordDocIFilterProcessor : FilterProcessor
	{
		#region Constructors

		public WordDocIFilterProcessor()
			: base("application/word", "doc")
		{
			MimeTypeExtensionMapping.Add("application/msword", "doc");
		}

		#endregion
	}
}