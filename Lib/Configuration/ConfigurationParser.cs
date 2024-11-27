using Microsoft.Extensions.Configuration;
namespace SearchPRBot.Lib.Configuration
{
	public class ConfigurationParser
	{
		private readonly IConfiguration _configuration;
		public ConfigurationParser(IConfiguration configuration)
		{
			_configuration = configuration;
		}
		public T Get<T>() where T: class
		{
			var secName = typeof(T).Name;
			var section = _configuration.GetSection(secName);
			var res = section?.Get<T>();
			return res;
		}
	}
}

