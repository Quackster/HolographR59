using System;
using System.Data;
using MySql.Data.MySqlClient;

namespace Zero.Storage;

internal class DatabaseClient : IDisposable
{
	public MySqlCommand Command;

	public MySqlConnection Connection;

	public uint Handle;

	public DateTime LastActivity;

	public DatabaseManager Manager;

	public int InactiveTime => (int)(DateTime.Now - LastActivity).TotalSeconds;

	public bool IsAnonymous => Handle == 0;

	public ConnectionState State => (Connection != null) ? Connection.State : ConnectionState.Broken;

	public DatabaseClient(uint _Handle, DatabaseManager _Manager)
	{
		if (_Manager == null)
		{
			throw new ArgumentNullException("[DBClient.Connect]: Invalid database handle");
		}
		Handle = _Handle;
		Manager = _Manager;
		Connection = new MySqlConnection(Manager.ConnectionString);
		Command = Connection.CreateCommand();
		UpdateLastActivity();
	}

	public void AddParamWithValue(string sParam, object val)
	{
		Command.Parameters.AddWithValue(sParam, val);
	}

	public void Connect()
	{
		try
		{
			Connection.Open();
		}
		catch (MySqlException ex)
		{
			throw new DatabaseException("[DBClient.Connect]: Could not open MySQL Connection - " + ex.Message);
		}
	}

	public void Destroy()
	{
		Disconnect();
		Connection.Dispose();
		Connection = null;
		Command.Dispose();
		Command = null;
		Manager = null;
	}

	public void Disconnect()
	{
		try
		{
			Connection.Close();
		}
		catch
		{
		}
	}

	public void Dispose()
	{
		if (IsAnonymous)
		{
			Destroy();
			return;
		}
		Command.CommandText = null;
		Command.Parameters.Clear();
		Manager.ReleaseClient(Handle);
	}

	public void ExecuteQuery(string sQuery)
	{
		Command.CommandText = sQuery;
		Command.ExecuteScalar();
		Command.CommandText = null;
	}

	public DataRow ReadDataRow(string Query)
	{
		DataTable DataTable = ReadDataTable(Query);
		if (DataTable != null && DataTable.Rows.Count > 0)
		{
			return DataTable.Rows[0];
		}
		return null;
	}

	public DataSet ReadDataSet(string Query)
	{
		DataSet DataSet = new DataSet();
		Command.CommandText = Query;
		using (MySqlDataAdapter Adapter = new MySqlDataAdapter(Command))
		{
			Adapter.Fill(DataSet);
		}
		Command.CommandText = null;
		return DataSet;
	}

	public bool findsResult(string sQuery)
	{
		bool Found = false;
		try
		{
			Command.CommandText = sQuery;
			MySqlDataReader dReader = Command.ExecuteReader();
			Found = dReader.HasRows;
			dReader.Close();
		}
		catch (Exception ex)
		{
			HolographEnvironment.GetLogging().WriteLine(ex.Message + "\n(^^" + sQuery + "^^)");
		}
		return Found;
	}

	public DataTable ReadDataTable(string Query)
	{
		DataTable DataTable = new DataTable();
		Command.CommandText = Query;
		using (MySqlDataAdapter Adapter = new MySqlDataAdapter(Command))
		{
			Adapter.Fill(DataTable);
		}
		Command.CommandText = null;
		return DataTable;
	}

	public int ReadInt32(string Query)
	{
		Command.CommandText = Query;
		int result = int.Parse(Command.ExecuteScalar().ToString());
		Command.CommandText = null;
		return result;
	}

	public string ReadString(string Query)
	{
		Command.CommandText = Query;
		string result = Command.ExecuteScalar().ToString();
		Command.CommandText = null;
		return result;
	}

	public void UpdateLastActivity()
	{
		LastActivity = DateTime.Now;
	}
}
