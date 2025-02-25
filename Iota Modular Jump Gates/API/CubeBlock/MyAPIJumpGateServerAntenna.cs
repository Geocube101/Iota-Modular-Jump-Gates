using IOTA.ModularJumpGates.CubeBlock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
