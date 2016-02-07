using System;
using System.IO;
using System.Text;

using Microsoft.Isam.Esent.Interop;

using NCrawler.EsentServices.Utils;
using NCrawler.Extensions;
using NCrawler.Utils;

namespace NCrawler.EsentServices
{
	public class EsentCrawlerHistoryService : HistoryServiceBase
	{
		private readonly bool _resume;

		#region Readonly & Static Fields

		private readonly string _databaseFileName;
		private readonly EsentInstance _esentInstance;

		#endregion

		#region Fields

		private JET_COLUMNDEF _historyCountColumn;
		private JET_COLUMNDEF _historyUrlColumn;

		#endregion

		#region Constructors

		public EsentCrawlerHistoryService(Uri baseUri, bool resume)
		{
			_resume = resume;
			_databaseFileName = Path.GetFullPath("NCrawlHist{0}\\Hist.edb".FormatWith(baseUri.GetHashCode()));

			if (!resume)
			{
				ClearHistory();
			}

			_esentInstance = new EsentInstance(_databaseFileName, (session, dbid) =>
				{
					EsentTableDefinitions.CreateGlobalsTable(session, dbid);
					EsentTableDefinitions.CreateHistoryTable(session, dbid);
				});

			// Get columns
			_esentInstance.Cursor((session, dbid) =>
				{
					Api.JetGetColumnInfo(session, dbid, EsentTableDefinitions.GlobalsTableName,
						EsentTableDefinitions.GlobalsCountColumnName, out _historyCountColumn);
					Api.JetGetColumnInfo(session, dbid, EsentTableDefinitions.HistoryTableName,
						EsentTableDefinitions.HistoryTableUrlColumnName, out _historyUrlColumn);
				});
		}

		#endregion

		#region Instance Methods

		protected override void Add(string key)
		{
			_esentInstance.Cursor((session, dbid) =>
				{
					using (Transaction transaction = new Transaction(session))
					{
						using (Table table = new Table(session, dbid, EsentTableDefinitions.HistoryTableName, OpenTableGrbit.None))
						{
							using (Update update = new Update(session, table, JET_prep.Insert))
							{
								Api.SetColumn(session, table, _historyUrlColumn.columnid, key, Encoding.Unicode);
								update.Save();
							}
						}

						using (Table table = new Table(session, dbid, EsentTableDefinitions.GlobalsTableName, OpenTableGrbit.None))
						{
							Api.EscrowUpdate(session, table, _historyCountColumn.columnid, 1);
						}

						transaction.Commit(CommitTransactionGrbit.None);
					}
				});
		}

		protected override void Cleanup()
		{
			if (!_resume)
			{
				ClearHistory();
			}

			_esentInstance.Dispose();
			base.Cleanup();
		}

		protected override bool Exists(string key)
		{
			return _esentInstance.Table(EsentTableDefinitions.HistoryTableName,
				(session, dbid, table) =>
					{
						Api.JetSetCurrentIndex(session, table, "by_id");
						Api.MakeKey(session, table, key, Encoding.Unicode, MakeKeyGrbit.NewKey);
						return Api.TrySeek(session, table, SeekGrbit.SeekEQ);
					});
		}

		protected override long GetRegisteredCount()
		{
			return _esentInstance.Table(EsentTableDefinitions.GlobalsTableName,
				(session, dbid, table) =>
					{
						int? tmp = Api.RetrieveColumnAsInt32(session, table, _historyCountColumn.columnid);
						if (tmp.HasValue)
						{
							return (long) tmp.Value;
						}

						return 0;
					});
		}

		private void ClearHistory()
		{
			AspectF.Define.
				IgnoreExceptions().
				Do(() =>
					{
						if (File.Exists(_databaseFileName))
						{
							File.Delete(_databaseFileName);
						}
					});
		}

		#endregion
	}
}