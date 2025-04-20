using System;

namespace IOTA.ModularJumpGates.ISC
{
	/* Not Functional
	 * Possible Inter-Server communication system for KEEN
	 * 
	 * Server Config:
	 * Servers can control and limit some aspects of ISC to maintain their security
	 * These settings cannot be altered by the modding API
	 * 
	 * Server Config - Whitelist:
	 * Any server not on this list cannot connect to this server whether inbound or outbound
	 * If empty, this setting is ignored
	 * An attempt to connect to this server from a non-whitelisted server's MyInterServerCommunication.connect will fail (ConnectionRefused)
	 * An attempt to connect to a non-whitelisted server from this server's MyInterServerCommunication.connect will fail (ConnectionRefused)
	 * This setting cannot be read by the modding API
	 * 
	 * Server Config - Blacklist:
	 * Any server on this list cannot connect to this server whether inbound or outbound
	 * An attempt to connect to this server from a blacklisted server's MyInterServerCommunication.connect will fail (ConnectionRefused)
	 * An attempt to connect to a blacklisted server from this server's MyInterServerCommunication.connect will fail (ConnectionRefused)
	 * This setting cannot be read by the modding API
	 * 
	 * Server Config - ISC Use Password
	 * A boolean value representing if ISC connections need authenticating against the server's password
	 * If true, any inbound ISC connection must specify the correct server password or the connection is refused
	 * If false, inbound ISC connections are not required to specify a password
	 * Will be false if server is not password protected
	 * This setting cannot be read by the modding API
	 * 
	 * Server Config - ISCAllowed:
	 * An integer value representing what ISC connections are allowed
	 * This setting may be read by the modding API
	 * Default is decidable but should probably be 1 or 2
	 * See below for possible values
	 * 
	 * - If this value is 0:
	 * - No ISC connections are allowed, whether internal or external
	 * - All attempts to open a connection to this server or from this server will fail
	 * 
	 * - If this value is 1:
	 * - Only internal connections are allowed
	 * - Connections can only be made from this server, back to this server
	 * - Any connection entering or leaving the local host are refused
	 * 
	 * - If this value is 2:
	 * - Only outbound and internal connections are allowed
	 * - Connections can only be made internally or from this server to an external server
	 * - Any connection entering the local host is refused
	 * 
	 * - If this value is 3:
	 * - Only inbound and internal connections are allowed
	 * - Connections can only be made internally or to this server from an external server
	 * - Any connection leaving the local host is refused
	 * 
	 * - If this value is 4:
	 * - All connections are allowed
	 * - Connections can be made both internally and externally
	 * 
	 * Server Config - Max Total Connections:
	 * The maximum total connections (whether inbound or outbound) this server will allow
	 * Any new connection that will exceed this count is refused
	 * This setting may be read by the modding API
	 * 
	 * Server Config - Max Inbound Connections:
	 * The maximum allowed inbound connections this server will allow
	 * Any new inbound connection that will exceed this count is refused
	 * This setting may be read by the modding API
	 * 
	 * Server Config - Max Outbound Connections:
	 * The maximum allowed outbound connections this server will allow
	 * Any new outbound connection that will exceed this count is refused
	 * This setting may be read by the modding API
	 * 
	 * Server Config - Max Mod Connections:
	 * The maximum total connections this server will allow for any one mod
	 * Any new connection from a mod that will exceed this count is refused
	 * This setting may be read by the modding API
	 * 
	 * Server Config - Max Mod Inbound Connections:
	 * The maximum allowed inbound connections this server will allow for any one mod
	 * Any new inbound connection from a mod that will exceed this count is refused
	 * This setting may be read by the modding API
	 * 
	 * Server Config - Max Mod Outbound Connections:
	 * The maximum allowed oubound connections this server will allow for any one mod
	 * Any new outbound connection from a mod that will exceed this count is refused
	 * This setting may be read by the modding API
	 */

	/* Class handling registered ISC handlers for this server's game session
	 * Any mod can register a callback for their id (the id should be unique to prevent conflicts) and a channel
	 * On receipt of connection event, password and other information can be authenticated and a new "CommSocket" class created
	 * ... The new "CommSocket" can then be passed to the specified handler given the id (passed in from connecting socket)
	 * ... If no handler is registered for the specified mod and channel id, the connection is refused
	 * ... The new "CommSocket" is stored in an internal list - used for closing all connections when the session ends
	 */
	public static class MyInterServerCommunication
	{
		/* Called at end of session
		 * Closes all open "CommSockets" and unregisters all handlers
		 * Any further connection is refused
		 */
		internal static void Close() { }

		/* Called at beginning of session
		 * Opens a listener to listen for incoming connections depending on server ISC config
		 */
		internal static void Init() { }

		/* Registers a handler for a given mod and channel
		 * If the mod has a handler on that channel, either no action will take place, the handler is overriten, an error is thrown, or the handler is added to a list if the implementation allows mulltiple handlers per mod per channel
		 * It may be possible to instead pass in a ModContext inplace of "mod_id", or have this value generated per mod per session to avoid one mod listening in on another's data
		 * 
		 * Parameters:
		 * ulong mod_id - The unique id of the mod
		 * ushort channel_id - The channel id to bind to
		 * Action<CommSocket> handler - The callback accepting a CommSocket called on the receiving server when an inbound connection is established
		 */
		public static void Register(ulong mod_id, ushort channel_id, Action<CommSocket> handler) { }

		/* Unbinds a handler for a given mod and channel
		 * If the mod has no handler on that channel no action is taken
		 * If the implementation allows for multiple handlers per mod per channel:
		 * ... A third parameter (the handler callback) may be passed to remove only that handler
		 * ... If the third parameter is null, all handlers for the specified channel and mod are removed
		 * 
		 * Parameters:
		 * ulong mod_id - The unique id of the mod
		 * ushort channel_id - The channel id to unbind from
		 */
		public static void Unregister(ulong mod_id, ushort channel_id) { }

		/* Unbinds all handlers for a given mod on all channels
		 * If the mod has no handlers no action is taken
		 * 
		 * Parameters:
		 * ulong mod_id - The unique id of the mod
		 */
		public static void Unregister(ulong mod_id) { }

		/* Attempts to connect to the specified remote SE server
		 * If the connection fails, it can either throw an error or return null
		 * The connection will fail if:
		 * ... The server address / port is unreachable
		 * ... The specified server is not an active or not a valid Space Engineers server
		 * ... The password is incorrect when connecting to a password protected server
		 * ... The connection is not allowed by either local or remote server settings
		 * ... The specified SE server failed to validate the connection
		 * ... The specified SE server has no listener on the specified mod id and channel
		 * Depending on implementation, the callback parameter may be removed and the registered handler called instead
		 * 
		 * Parameters:
		 * ulong mod_id - The unique id of the mod handler to connect to
		 * ushort channel_id - The channel id to connect to
		 * string server_address - A non-null string representing the SE server address and port
		 * Action<CommSocket> callback - The callback called when the connection is established
		 * string server_password - The server's password if applicable, or null
		 */
		public static void Connect(ulong mod_id, ushort channel_id, string server_address, Action<CommSocket> callback, string server_password = null) { }
	}

	/* Class handling physical connection between servers
	 * Connection is a pipe for transmitting bytes
	 * Instance is created with underlying connection already established by the "MyInterServerCommunication" class
	 */
	public class CommSocket
	{
		/* ~ Destructor ~
		 * Close the connection when instance is garbage collected and if connection is still open
		 * Theoretically, connection should already be closed from "MyInterServerCommunication"
		 */
		~CommSocket() { }

		/* bool IsSender - True if this server initiated the connection or can write to the pipe
		 * ... If the implentation is a duplex pipe, this can be skipped or always true
		 */
		public readonly bool IsSender;

		/* bool Closed - True if the socket is closed
		 */
		public bool Closed { get; private set; }

		/* * uint InWaiting - The number of bytes waiting to be read (For inbound or duplex connections only, will be 0 if outbound and not duplex)
		 * ... If the implementation is not buffered, this property can be removed
		 */
		public uint InWaiting { get; private set; }

		/* uint OutWaiting - The number of bytes waiting to be written (For outbound or duplex connections only, will be 0 if inbound and not duplex)
		 * ... If the implementation is not buffered, this property can be removed
		 */
		public uint OutWaiting { get; private set; }

		/* Closes the connection
		 * Any data in output buffer is written and the connection closed
		 * Could also close the connection without writing data
		 * Any further IO on this connection raises an error
		 */
		public void Close() { }

		/* Writes bytes to the pipe connection
		 * If inbound and not duplex: either raises an error or does nothing
		 */
		public void Write(byte[] buffer) { }

		/* Reads bytes from the pipe connection
		 * If outbound and not duplex: either raises an error or returns 0
		 * 
		 * Parameters:
		 * byte[] buffer - The byte buffer to read into
		 * int length - The number of bytes to read or the buffer's length
		 * int offset - The start position in the buffer to begin writing
		 * bool blocking - If true, blocks until the specified number of bytes have been read, otherwise, reads at most "length" bytes
		 * 
		 * Returns:
		 * uint - The number of bytes read
		 */
		public uint Read(byte[] buffer, int length, int offset = 0, bool blocking = false) { return 0; }

		/* Reads bytes from the pipe connection
		 * If outbound and not duplex: either raises an error or returns 0
		 * This method is allocating
		 * 
		 * Parameters:
		 * int n - The number of bytes to read
		 * bool blocking - If true, blocks until the specified number of bytes have been read, otherwise, reads at most "n" bytes
		 * 
		 * Returns:
		 * byte[] - The read bytes
		 */
		public byte[] Read(int n, bool blocking = false) { return new byte[] { }; }

		/* If the implementation is buffered:
		 * Flushes the buffer
		 * Blocks until flush is complete
		 * 
		 * For outbound or duplex connection - Writes output buffer over network
		 * For inbound or duplex connection - Clears the input buffer
		 */
		public void Flush() { }

		/* If the implementation is buffered:
		 * Clears the buffer, data is not written to network
		 * 
		 * For outbound or duplex connection - Clears all data from the output buffer
		 * For inbound or duplex connection - Clears all data from the input buffer
		 */
		public void ClearBuffer() { }
	}
}
