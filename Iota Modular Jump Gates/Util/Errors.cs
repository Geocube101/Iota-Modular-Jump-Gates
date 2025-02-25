using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOTA.ModularJumpGates.Util
{
	public class InvalidGuuidException : KeyNotFoundException
	{
		public InvalidGuuidException() { }
		public InvalidGuuidException(string message) : base(message) { }
	}

	public class InvalidBlockTypeException : InvalidOperationException
	{
		public InvalidBlockTypeException() { }
		public InvalidBlockTypeException(string message) : base(message) { }
	}
}
