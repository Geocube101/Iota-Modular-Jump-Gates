using System;
using System.Collections.Generic;

namespace IOTA.ModularJumpGates.Util
{
	public class InvalidGuuidException : KeyNotFoundException
	{
		public InvalidGuuidException() { }
		public InvalidGuuidException(string message) : base(message) { }
		public InvalidGuuidException(string message, Exception e) : base(message, e) { }
	}

	public class InvalidBlockTypeException : InvalidOperationException
	{
		public InvalidBlockTypeException() { }
		public InvalidBlockTypeException(string message) : base(message) { }
		public InvalidBlockTypeException(string message, Exception e) : base(message, e) { }
	}

	public class ExplicitTickFailureException : InvalidOperationException
	{
		public ExplicitTickFailureException() { }
		public ExplicitTickFailureException(string message) : base(message) { }
		public ExplicitTickFailureException(string message, Exception e) : base(message, e) { }
	}

	public class ModFileLoadingException : Exception
	{
		public ModFileLoadingException() { }
		public ModFileLoadingException(string message) : base(message) { }
		public ModFileLoadingException(string message, Exception e) : base(message, e) { }
	}
}
