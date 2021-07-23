using System;
using ThunderRoad;

namespace RealisticBleeding
{
	[Serializable]
	public class LevelModule : ThunderRoad.LevelModule
	{
		public Configuration Configuration = new Configuration();
		
		private LevelModule()
		{
			EntryPoint.OnLoaded(Configuration);
		}

		public override void Update(Level level)
		{
			EntryPoint.OnUpdate();
		}
	}
}