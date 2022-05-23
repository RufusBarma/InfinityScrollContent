using Hangfire;

namespace Client.Telegram.Boilerplates; 

public class ContainerJobActivator : JobActivator
{
	private IServiceProvider _container;

	public ContainerJobActivator(IServiceProvider container)
	{
		_container = container;
	}

	public override object ActivateJob(Type type)
	{
		return _container.GetService(type);
	}
}