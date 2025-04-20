using IOTA.ModularJumpGates.CubeBlock;

namespace IOTA.ModularJumpGates.API.CubeBlock
{
	public class MyAPIJumpGateServerAntenna : MyAPICubeBlockBase
	{
		new internal MyJumpGateServerAntenna CubeBlock;

		internal MyAPIJumpGateServerAntenna(MyJumpGateServerAntenna block) : base(block)
		{
			this.CubeBlock = block;
		}
	}
}
