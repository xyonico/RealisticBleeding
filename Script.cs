using ThunderRoad;

namespace RealisticBleeding
{
	public class Script : ThunderScript
	{
		public override void ScriptLoaded(ModManager.ModData modData)
		{
			base.ScriptLoaded(modData);
			
			EntryPoint.OnLoaded();
		}

		public override void ScriptUpdate()
		{
			EntryPoint.OnUpdate();
		}

		public override void ScriptFixedUpdate()
		{
			EntryPoint.OnFixedUpdate();
		}
	}
}