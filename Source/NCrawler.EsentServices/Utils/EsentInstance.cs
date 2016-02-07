using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.Isam.Esent.Interop;
using Microsoft.Isam.Esent.Interop.Windows7;

using NCrawler.Extensions;
using NCrawler.Utils;

namespace NCrawler.EsentServices.Utils
{
	public class EsentInstance : DisposableBase
	{
		#region Readonly & Static Fields

		private readonly Action<Session, JET_DBID> _createTable;
		private readonly Stack<Cursor> _cursors = new Stack<Cursor>();
		private readonly string _databaseFileName;

		#endregion

		#region Constructors

		public EsentInstance(string databaseFileName, Action<Session, JET_DBID> createTable)
		{
			_databaseFileName = databaseFileName;
			_createTable = createTable;

			AspectF.Define.
				Retry(TimeSpan.Zero, 1, null).
				Do(InitInstance);

			try
			{
				if (!File.Exists(_databaseFileName))
				{
					CreateDatabase();
				}
			}
			catch (Exception)
			{
				// We have failed to initialize for some reason. Terminate
				// the instance.
				Instance.Term();
				throw;
			}
		}

		private void InitInstance()
		{
			string directory = Path.GetDirectoryName(_databaseFileName);
			Instance = new Instance(Guid.NewGuid().ToString());
			Instance.Parameters.TempDirectory = Path.Combine(directory, "temp");
			Instance.Parameters.SystemDirectory = Path.Combine(directory, "system");
			Instance.Parameters.LogFileDirectory = Path.Combine(directory, "logs");
			Instance.Parameters.AlternateDatabaseRecoveryDirectory = directory;
			Instance.Parameters.CreatePathIfNotExist = true;
			Instance.Parameters.EnableIndexChecking = false;
			Instance.Parameters.CircularLog = true;
			Instance.Parameters.CheckpointDepthMax = 64 * 1024 * 1024;
			Instance.Parameters.LogFileSize = 1024; // 1MB logs
			Instance.Parameters.LogBuffers = 1024; // buffers = 1/2 of logfile
			Instance.Parameters.MaxTemporaryTables = 0;
			Instance.Parameters.MaxVerPages = 1024;
			Instance.Parameters.NoInformationEvent = true;
			Instance.Parameters.WaypointLatency = 1;
			Instance.Parameters.MaxSessions = 256;
			Instance.Parameters.MaxOpenTables = 256;
			Instance.Parameters.EventSource = "NCrawler";

			InitGrbit grbit = EsentVersion.SupportsWindows7Features
				? Windows7Grbits.ReplayIgnoreLostLogs
				: InitGrbit.None;
			try
			{
				Instance.Init(grbit);
			}
			catch
			{
				Directory.Delete(directory, true);
				throw;
			}
		}

		#endregion

		#region Instance Properties

		public Instance Instance { get; set; }

		#endregion

		#region Instance Methods

		public T Cursor<T>(Func<Session, JET_DBID, T> action)
		{
			Cursor cursor;
			lock (_cursors)
			{
				cursor = _cursors.Count > 0 ? _cursors.Pop() : new Cursor(Instance, _databaseFileName);
			}

			try
			{
				return action(cursor.Session, cursor.Dbid);
			}
			finally
			{
				lock (_cursors)
				{
					_cursors.Push(cursor);
				}
			}
		}

		public void Cursor(Action<Session, JET_DBID> action)
		{
			Cursor((session, dbid) =>
				{
					action(session, dbid);
					return (object) null;
				});
		}

		public T Table<T>(string tableName, Func<Session, JET_DBID, Table, T> action)
		{
			return Cursor((session, dbid) =>
				{
					using (Table table = new Table(session, dbid, tableName, OpenTableGrbit.None))
					{
						return action(session, dbid, table);
					}
				});
		}

		protected override void Cleanup()
		{
			_cursors.ForEach(cursor => cursor.Dispose());
			//Instance.Dispose();
			Instance.Term();
		}

		private void CreateDatabase()
		{
			using (Session session = new Session(Instance))
			{
				JET_DBID dbid;
				Api.JetCreateDatabase(session, _databaseFileName, string.Empty, out dbid, CreateDatabaseGrbit.None);
				try
				{
					using (Transaction transaction = new Transaction(session))
					{
						_createTable(session, dbid);
						transaction.Commit(CommitTransactionGrbit.None);
						Api.JetCloseDatabase(session, dbid, CloseDatabaseGrbit.None);
						Api.JetDetachDatabase(session, _databaseFileName);
					}
				}
				catch (Exception)
				{
					// Delete the partially constructed database
					Api.JetCloseDatabase(session, dbid, CloseDatabaseGrbit.None);
					Api.JetDetachDatabase(session, _databaseFileName);
					File.Delete(_databaseFileName);
					throw;
				}
			}
		}

		#endregion
	}
}