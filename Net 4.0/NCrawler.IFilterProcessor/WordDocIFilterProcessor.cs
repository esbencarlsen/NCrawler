namespace NCrawler.IFilterProcessor
{
	public class WordDocIFilterProcessor : IFilterProcessor
	{
		#region Constructors

		public WordDocIFilterProcessor()
			: base("application/word", "doc")
		{
			m_MimeTypeExtensionMapping.Add("application/msword", "doc");
		}

		#endregion
	}
}