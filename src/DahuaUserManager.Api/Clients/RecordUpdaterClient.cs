namespace DahuaUserManager.Api.Clients;

public class RecordUpdaterClient
{
	private readonly DahuaClient _client = new();

	public async Task<string> DiagnoseInsertAsync(
		string ipAddress,
		string username,
		string password)
	{
		return await _client.ExecuteAuthenticatedGetDiagnosticAsync(
			ipAddress,
			username,
			password,
			"/cgi-bin/recordUpdater.cgi?action=insert&name=AccessControlCard");
	}

	public async Task<string> DiagnoseUpdateAsync(
		string ipAddress,
		string username,
		string password,
		int recNo)
	{
		return await _client.ExecuteAuthenticatedGetDiagnosticAsync(
			ipAddress,
			username,
			password,
			$"/cgi-bin/recordUpdater.cgi?action=update&name=AccessControlCard&recno={recNo}");
	}

	public async Task<string> DiagnoseRemoveAsync(
		string ipAddress,
		string username,
		string password,
		int recNo)
	{
		return await _client.ExecuteAuthenticatedGetDiagnosticAsync(
			ipAddress,
			username,
			password,
			$"/cgi-bin/recordUpdater.cgi?action=remove&name=AccessControlCard&recno={recNo}");
	}
}