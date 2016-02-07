using Microsoft.Isam.Esent.Interop;

using NCrawler.Utils;

namespace NCrawler.EsentServices.Utils
{
	public class Cursor : DisposableBase
	{
		#region Readonly & Static Fields

		public readonly JET_DBID Dbid;
		public readonly Session Session;
		private readonly string m_DatabaseFileName;

		#endregion

		#region Constructors

		public Cursor(Instance instance, string databaseFileName)
		{
			m_DatabaseFileName = databaseFileName;
			Session = new Session(instance);
			Api.JetAttachDatabase(Session, databaseFileName, AttachDatabaseGrbit.None);
			Api.JetOpenDatabase(Session, databaseFileName, null, out Dbid, OpenDatabaseGrbit.None);
		}

		#endregion

		#region Instance Methods

		protected override void Cleanup()
		{
			Api.JetCloseDatabase(Session, Dbid, CloseDatabaseGrbit.None);
			Api.JetDetachDatabase(Session, m_DatabaseFileName);
			Session.Dispose();
		}

		#endregion
	}
}