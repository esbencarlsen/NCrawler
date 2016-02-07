using System;
using System.Linq;

using Db4objects.Db4o;

using NCrawler.Extensions;
using NCrawler.Utils;

namespace NCrawler.Db4oServices
{
	public class Db4OHistoryService : HistoryServiceBase
	{
		#region Readonly & Static Fields

		private readonly IObjectContainer _db;
		private readonly bool _resume;

		#endregion

		#region Constructors

		public Db4OHistoryService(Uri baseUri, bool resume)
		{
			_resume = resume;
			_db = Db4oEmbedded.OpenFile(Db4oEmbedded.NewConfiguration(),
				"NCrawlerHist_{0}.Yap".FormatWith(baseUri.GetHashCode()));
			ClearHistory();
		}

		#endregion

		#region Instance Methods

		protected override void Add(string key)
		{
			_db.Store(new StringWrapper {Key = key});
		}

		protected override void Cleanup()
		{
			ClearHistory();
			_db.Dispose();
			base.Cleanup();
		}

		protected override bool Exists(string key)
		{
			return _db.Query<StringWrapper>(entry => entry.Key == key).Any();
		}

		protected override long GetRegisteredCount()
		{
			return _db.Query<StringWrapper>().Count;
		}

		private void ClearHistory()
		{
			if (!_resume)
			{
				_db.Query<StringWrapper>().ForEach(entry => _db.Delete(entry));
			}
		}

		#endregion
	}

	internal class StringWrapper
	{
		#region Instance Properties

		public string Key { get; set; }

		#endregion
	}
}