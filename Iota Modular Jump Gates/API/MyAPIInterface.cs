namespace IOTA.ModularJumpGates.API
{
	public class MyAPIInterface
	{
		internal long ObjectInterfaceID = -1;

		internal ulong ReferenceCounter = 0;

		#region Public Methods
		/// <summary>
		/// Closes this API object<br />
		/// No code path should use this handle beyond this point
		/// </summary>
		public void Close()
		{
			MyAPISession.ReleaseInterface(this);
		}

		/// <summary>
		/// Releases this handle to the API object
		/// It will be released if the internal reference counter reaches 0<br />
		/// The calling code path should not use this handle beyond this point
		/// </summary>
		public void Release()
		{
			if (this.ReferenceCounter == 0 || --this.ReferenceCounter == 0) MyAPISession.ReleaseInterface(this);
		}
		#endregion
	}
}
