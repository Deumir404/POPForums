namespace PopForums.Services;

public interface IUserSessionWorker
{
	void Execute();
}

public class UserSessionWorker(IUserSessionService sessionService, IErrorLog errorLog) : IUserSessionWorker
{
	//Создать сервис для очистки старых сессий
	public async void Execute()
	{
		try
		{
			await sessionService.CleanUpExpiredSessions();
		}
		catch (Exception exc)
		{
			errorLog.Log(exc, ErrorSeverity.Error);
		}
	}
}