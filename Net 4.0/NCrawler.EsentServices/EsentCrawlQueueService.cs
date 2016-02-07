using System;
using System.IO;

using Microsoft.Isam.Esent.Interop;

using NCrawler.EsentServices.Utils;
using NCrawler.Extensions;
using NCrawler.Utils;

namespace NCrawler.EsentServices
{
	public class EsentCrawlQueueService : CrawlerQueueServiceBase
	{
		#region Readonly & Static Fields

		private readonly string m_DatabaseFileName;
		private readonly EsentInstance m_EsentInstance;

		#endregion

		#region Fields

		private JET_COLUMNDEF dataColumn;
		private JET_COLUMNDEF queueCountColumn;

		#endregion

		#region Constructors

		public EsentCrawlQueueService(Uri baseUri, bool resume)
		{
			m_DatabaseFileName = Path.GetFullPath("NCrawlQueue{0}\\Queue.edb".FormatWith(baseUri.GetHashCode()));

			if (!resume && File.Exists(m_DatabaseFileName))
			{
				ClearQueue();
			}

			m_EsentInstance = new EsentInstance(m_DatabaseFileName, (session, dbid) =>
				{
					EsentTableDefinitions.CreateGlobalsTable(session, dbid);
					EsentTableDefinitions.CreateQueueTable(session, dbid);
				});

			// Get columns
			m_EsentInstance.Cursor((session, dbid) =>
				{
					Api.JetGetColumnInfo(session, dbid, EsentTableDefinitions.GlobalsTableName,
						EsentTableDefinitions.GlobalsCountColumnName,
						out queueCountColumn);
					Api.JetGetColumnInfo(session, dbid, EsentTableDefinitions.QueueTableName,
						EsentTableDefinitions.QueueTableDataColumnName,
						out dataColumn);
				});
		}

		#endregion

		#region Instance Methods

		protected override void Cleanup()
		{
			m_EsentInstance.Dispose();
			base.Cleanup();
		}

		protected override long GetCount()
		{
			return m_EsentInstance.Table(EsentTableDefinitions.GlobalsTableName,
				(session, dbid, table) =>
					{
						int? tmp = Api.RetrieveColumnAsInt32(session, table, queueCountColumn.columnid);
						if (tmp.HasValue)
						{
							return (long) tmp.Value;
						}

						return 0;
					});
		}

		protected override CrawlerQueueEntry PopImpl()
		{
			return m_EsentInstance.Cursor((session, dbid) =>
				{
					using (Transaction transaction = new Transaction(session))
					{
						using (Table table = new Table(session, dbid, EsentTableDefinitions.QueueTableName, OpenTableGrbit.None))
						{
							if (Api.TryMoveFirst(session, table))
							{
								byte[] data = Api.RetrieveColumn(session, table, dataColumn.columnid);
								Api.JetDelete(session, table);

								using (Table table2 = new Table(session, dbid, EsentTableDefinitions.GlobalsTableName, OpenTableGrbit.None))
								{
									Api.EscrowUpdate(session, table2, queueCountColumn.columnid, -1);
								}

								transaction.Commit(CommitTransactionGrbit.None);
								return data.FromBinary<CrawlerQueueEntry>();
							}
						}

						transaction.Rollback();
						return null;
					}
				});
		}

		protected override void PushImpl(CrawlerQueueEntry crawlerQueueEntry)
		{
			m_EsentInstance.Cursor((session, dbid) =>
				{
					using (Transaction transaction = new Transaction(session))
					{
						using (Table table = new Table(session, dbid, EsentTableDefinitions.QueueTableName, OpenTableGrbit.None))
						{
							using (Update update = new Update(session, table, JET_prep.Insert))
							{
								Api.SetColumn(session, table, dataColumn.columnid, crawlerQueueEntry.ToBinary());
								update.Save();
							}
						}

						using (Table table = new Table(session, dbid, EsentTableDefinitions.GlobalsTableName, OpenTableGrbit.None))
						{
							Api.EscrowUpdate(session, table, queueCountColumn.columnid, 1);
						}

						transaction.Commit(CommitTransactionGrbit.None);
					}
				});
		}

		private void ClearQueue()
		{
			File.Delete(m_DatabaseFileName);
		}

		#endregion
	}
}