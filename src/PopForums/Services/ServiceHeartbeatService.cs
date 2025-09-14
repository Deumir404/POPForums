namespace PopForums.Services;

public interface IServiceHeartbeatService
{
	Task RecordHeartbeat(string serviceName, string machineName);
	Task<List<ServiceHeartbeat>> GetAll();
	Task ClearAll();
}

public class ServiceHeartbeatService : IServiceHeartbeatService
{
	private readonly IServiceHeartbeatRepository _serviceHeartbeatRepository;

	public ServiceHeartbeatService(IServiceHeartbeatRepository serviceHeartbeatRepository)
	{
		_serviceHeartbeatRepository = serviceHeartbeatRepository;
	}
	//Запись логов от подсистем
	public async Task RecordHeartbeat(string serviceName, string machineName)
	{
		await _serviceHeartbeatRepository.RecordHeartbeat(serviceName, machineName, DateTime.UtcNow);
	}
	//Получить все логов от подсистем
	public async Task<List<ServiceHeartbeat>> GetAll()
	{
		return await _serviceHeartbeatRepository.GetAll();
	}
	//Удалить все логи
	public async Task ClearAll()
	{
		await _serviceHeartbeatRepository.ClearAll();
	}
}