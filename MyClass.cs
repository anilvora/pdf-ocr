using Microsoft.Extensions.Options;
using Practice.WebApp.Model;
using System.Runtime;

namespace Practice.WebApp
{
	public class MyClass
	{
		private readonly MyAppSetting _mySettings;

		public MyAppSetting MyAppSetting { get { return _mySettings; } }

		public MyClass(IOptions<MyAppSetting> mySettings)
		{
			_mySettings = mySettings.Value;
		}

	}
}
