namespace PopForums.Services;

public interface ICloseAgedTopicsWorker
{
	void Execute();
}

//Использовать функцию класса, с обработкой ошибок и логированием
public class CloseAgedTopicsWorker(ITopicService topicService, IErrorLog errorLog) : ICloseAgedTopicsWorker
{
	public async void Execute()
	{
		try
		{
			await topicService.CloseAgedTopics();
		}
		catch (Exception exc)
		{
			errorLog.Log(exc, ErrorSeverity.Error);
		}
	}
}