using ThunderRoad;

namespace RealisticBleeding
{
	public class LevelModule : ThunderRoad.LevelModule
	{
		static LevelModule()
		{
			EntryPoint.OnLoaded();
		}

		public override void Update(Level level)
		{
			EntryPoint.OnUpdate();
		}
	}
}