namespace NCrawler.Events
{
	public class AfterDownloadEventArgs : BeforeDownloadEventArgs
	{
		#region Constructors

		internal AfterDownloadEventArgs(bool cancel, PropertyBag response)
			: base(cancel, response.Step)
		{
			Response = response;
		}

		#endregion

		#region Instance Properties

		public PropertyBag Response { get; private set; }

		#endregion
	}
}