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
		private readonly bool m_Resume;

		#region Readonly & Static Fields

		private readonly string m_DatabaseFileName;
		private readonly EsentInstance m_EsentInstance;

		#endregion

		#region Fields

		private JET_COLUMNDEF historyCountColumn;
		private JET_COLUMNDEF historyUrlColumn;

		#endregion

		#region Constructors

		public EsentCrawlerHistoryService(Uri baseUri, bool resume)
		{
			m_Resume = resume;
			m_DatabaseFileName = Path.GetFullPath("NCrawlHist{0}\\Hist.edb".FormatWith(baseUri.GetHashCode()));

			if (!resume)
			{
				ClearHistory();
			}

			m_EsentInstance = new EsentInstance(m_DatabaseFileName, (session, dbid) =>
				{
					EsentTableDefinitions.CreateGlobalsTable(session, dbid);
					EsentTableDefinitions.CreateHistoryTable(session, dbid);
				});

			// Get columns
			m_EsentInstance.Cursor((session, dbid) =>
				{
					Api.JetGetColumnInfo(session, dbid, EsentTableDefinitions.GlobalsTableName,
						EsentTableDefinitions.GlobalsCountColumnName, out historyCountColumn);
					Api.JetGetColumnInfo(session, dbid, EsentTableDefinitions.HistoryTableName,
						EsentTableDefinitions.HistoryTableUrlColumnName, out historyUrlColumn);
				});
		}

		#endregion

		#region Instance Methods

		protected override void Add(string key)
		{
			m_EsentInstance.Cursor((session, dbid) =>
				{
					using (Transaction transaction = new Transaction(session))
					{
						using (Table table = new Table(session, dbid, EsentTableDefinitions.HistoryTableName, OpenTableGrbit.None))
						{
							using (Update update = new Update(session, table, JET_prep.Insert))
							{
								Api.SetColumn(session, table, historyUrlColumn.columnid, key, Encoding.Unicode);
								update.Save();
							}
						}

						using (Table table = new Table(session, dbid, EsentTableDefinitions.GlobalsTableName, OpenTableGrbit.None))
						{
							Api.EscrowUpdate(session, table, historyCountColumn.columnid, 1);
						}

						transaction.Commit(CommitTransactionGrbit.None);
					}
				});
		}

		protected override void Cleanup()
		{
			if (!m_Resume)
			{
				ClearHistory();
			}

			m_EsentInstance.Dispose();
			base.Cleanup();
		}

		protected override bool Exists(string key)
		{
			return m_EsentInstance.Table(EsentTableDefinitions.HistoryTableName,
				(session, dbid, table) =>
					{
						Api.JetSetCurrentIndex(session, table, "by_id");
						Api.MakeKey(session, table, key, Encoding.Unicode, MakeKeyGrbit.NewKey);
						return Api.TrySeek(session, table, SeekGrbit.SeekEQ);
					});
		}

		protected override long GetRegisteredCount()
		{
			return m_EsentInstance.Table(EsentTableDefinitions.GlobalsTableName,
				(session, dbid, table) =>
					{
						int? tmp = Api.RetrieveColumnAsInt32(session, table, historyCountColumn.columnid);
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
						if (File.Exists(m_DatabaseFileName))
						{
							File.Delete(m_DatabaseFileName);
						}
					});
		}

		#endregion
	}
}