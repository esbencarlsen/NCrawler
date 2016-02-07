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

		private readonly string _databaseFileName;
		private readonly EsentInstance _esentInstance;

		#endregion

		#region Fields

		private JET_COLUMNDEF _dataColumn;
		private JET_COLUMNDEF _queueCountColumn;

		#endregion

		#region Constructors

		public EsentCrawlQueueService(Uri baseUri, bool resume)
		{
			_databaseFileName = Path.GetFullPath("NCrawlQueue{0}\\Queue.edb".FormatWith(baseUri.GetHashCode()));

			if (!resume && File.Exists(_databaseFileName))
			{
				ClearQueue();
			}

			_esentInstance = new EsentInstance(_databaseFileName, (session, dbid) =>
				{
					EsentTableDefinitions.CreateGlobalsTable(session, dbid);
					EsentTableDefinitions.CreateQueueTable(session, dbid);
				});

			// Get columns
			_esentInstance.Cursor((session, dbid) =>
				{
					Api.JetGetColumnInfo(session, dbid, EsentTableDefinitions.GlobalsTableName,
						EsentTableDefinitions.GlobalsCountColumnName,
						out _queueCountColumn);
					Api.JetGetColumnInfo(session, dbid, EsentTableDefinitions.QueueTableName,
						EsentTableDefinitions.QueueTableDataColumnName,
						out _dataColumn);
				});
		}

		#endregion

		#region Instance Methods

		protected override void Cleanup()
		{
			_esentInstance.Dispose();
			base.Cleanup();
		}

		protected override long GetCount()
		{
			return _esentInstance.Table(EsentTableDefinitions.GlobalsTableName,
				(session, dbid, table) =>
					{
						int? tmp = Api.RetrieveColumnAsInt32(session, table, _queueCountColumn.columnid);
						if (tmp.HasValue)
						{
							return (long) tmp.Value;
						}

						return 0;
					});
		}

		protected override CrawlerQueueEntry PopImpl()
		{
			return _esentInstance.Cursor((session, dbid) =>
				{
					using (Transaction transaction = new Transaction(session))
					{
						using (Table table = new Table(session, dbid, EsentTableDefinitions.QueueTableName, OpenTableGrbit.None))
						{
							if (Api.TryMoveFirst(session, table))
							{
								byte[] data = Api.RetrieveColumn(session, table, _dataColumn.columnid);
								Api.JetDelete(session, table);

								using (Table table2 = new Table(session, dbid, EsentTableDefinitions.GlobalsTableName, OpenTableGrbit.None))
								{
									Api.EscrowUpdate(session, table2, _queueCountColumn.columnid, -1);
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
			_esentInstance.Cursor((session, dbid) =>
				{
					using (Transaction transaction = new Transaction(session))
					{
						using (Table table = new Table(session, dbid, EsentTableDefinitions.QueueTableName, OpenTableGrbit.None))
						{
							using (Update update = new Update(session, table, JET_prep.Insert))
							{
								Api.SetColumn(session, table, _dataColumn.columnid, crawlerQueueEntry.ToBinary());
								update.Save();
							}
						}

						using (Table table = new Table(session, dbid, EsentTableDefinitions.GlobalsTableName, OpenTableGrbit.None))
						{
							Api.EscrowUpdate(session, table, _queueCountColumn.columnid, 1);
						}

						transaction.Commit(CommitTransactionGrbit.None);
					}
				});
		}

		private void ClearQueue()
		{
			File.Delete(_databaseFileName);
		}

		#endregion
	}
}