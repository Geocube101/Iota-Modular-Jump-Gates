using IOTA.ModularJumpGates.ModConfiguration;
using IOTA.ModularJumpGates.Terminal;
using IOTA.ModularJumpGates.Util;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VRageMath;

namespace IOTA.ModularJumpGates.Session
{
	internal partial class MyJumpGateModSession
	{
		#region Private Variables
		/// <summary>
		/// The variable name to access the mod log file list
		/// </summary>
		private string ModLogSettingsFile = "ModLogStorage.dat";

		/// <summary>
		/// The variable name to access the mod log file list
		/// </summary>
		private string ModDataFile = "ModData.dat";

		/// <summary>
		/// The error that occured loading the mod log file list
		/// </summary>
		private ModFileLoadingException ModDataLoadError = null;

		/// <summary>
		/// Mod data as loaded from file
		/// </summary>
		private MyModDataInfo ModData = null;
		#endregion

		#region Public Variables
		/// <summary>
		/// The maximum number of mod-specific log files that can be stored
		/// </summary>
		public uint MaxModSpecificLogFiles => this.Configuration?.ModSettings.MaxStoredModSpecificLogFiles ?? MyModConfigurationV1.MyModSettings.Defaults().MaxStoredModSpecificLogFiles;

		/// <summary>
		/// The current number of mod-specific log files
		/// </summary>
		public int ModSpecificLogFileCount => this.ModData?.ModLogFiles?.Count ?? 0;

		/// <summary>
		/// The currently active mod-specific log file name
		/// </summary>
		public string ActiveModLogFile { get; private set; }

		/// <summary>
		/// Configuration variables as loaded from file or recieved from server
		/// </summary>
		public MyModConfigurationV1 Configuration { get; private set; } = null;
		#endregion

		#region Private Methods
		/// <summary>
		/// Unloads the configuration and saves the mod data file
		/// </summary>
		private void UnloadConfiguration()
		{
			this.UpdateSaveModDataFile();
			MyModConfigurationV1.Dispose();
			this.Configuration = null;
		}

		/// <summary>
		/// Stores the mod-specific log file list to sandbox.sbc
		/// </summary>
		private void UpdateSaveModDataFile()
		{
			uint max_logfiles = this.MaxModSpecificLogFiles;
			BinaryWriter writer = null;

			try
			{
				writer = MyAPIGateway.Utilities.WriteBinaryFileInLocalStorage(this.ModDataFile, this.GetType());
				this.ModData.ModLogFiles.LastOrDefault()?.UpdateTime();
				this.ModData.ModLogFiles.RemoveAll((file) => file.Filename != this.ActiveModLogFile && !MyAPIGateway.Utilities.FileExistsInLocalStorage(file.Filename, this.GetType()));
				this.ModData.MaxModSpecificLogFiles = max_logfiles;
				writer.Write(MyAPIGateway.Utilities.SerializeToBinary(this.ModData));
				Logger.Log($"Mod data file updated; Version={this.ModData.ModVersion.X}.{this.ModData.ModVersion.Y}.{this.ModData.ModVersion.Z}, Log Capacity={this.ModData.ModLogFiles.Count}/{max_logfiles}");
			}
			catch (Exception e)
			{
				Logger.Error($"Failed to serialize mod data\n  ...\n[ {e.GetType().Name} ]: {e.Message}\n{e.StackTrace}\n{e.InnerException}");
				MyAPIGateway.Utilities.ShowMessage(this.DisplayName, "Failed to save mod data; See log for details");
			}
			finally
			{
				writer?.Close();
			}
		}

		/// <summary>
		/// Loads the mod-specific log file list from sandbox.sbc
		/// </summary>
		/// <returns>Whether data was successfully loaded</returns>
		private bool UpdateLoadLogFileList()
		{
			if (!MyAPIGateway.Utilities.FileExistsInLocalStorage(this.ModLogSettingsFile, this.GetType()))
			{
				this.ModData = new MyModDataInfo
				{
					ModVersion = new Vector3I(0, 0, 9),
					MaxModSpecificLogFiles = this.MaxModSpecificLogFiles,
					ModLogFiles = new List<MyModLogFileInfo>(),
				};
				return false;
			}

			BinaryReader reader = null;

			try
			{
				reader = MyAPIGateway.Utilities.ReadBinaryFileInLocalStorage(this.ModLogSettingsFile, this.GetType());
				reader.ReadUInt32();
				byte[] buffer = new byte[reader.BaseStream.Length - reader.BaseStream.Position];
				reader.Read(buffer, 0, buffer.Length);
				List<MyModLogFileInfo> log_files = MyAPIGateway.Utilities.SerializeFromBinary<List<MyModLogFileInfo>>(buffer) ?? new List<MyModLogFileInfo>();
				log_files.RemoveAll((file) => file.Filename != this.ActiveModLogFile && !MyAPIGateway.Utilities.FileExistsInLocalStorage(file.Filename, this.GetType()));
				this.ModData = new MyModDataInfo
				{
					ModVersion = new Vector3I(0, 0, 9),
					MaxModSpecificLogFiles = this.MaxModSpecificLogFiles,
					ModLogFiles = log_files,
				};
			}
			catch (Exception e)
			{
				this.ModDataLoadError = new ModFileLoadingException("Failed to deserialize mod-specific log file settings", e);
			}
			finally
			{
				reader?.Close();
			}

			return true;
		}

		/// <summary>
		/// Loads the mod data file from local storage
		/// </summary>
		/// <returns>Whether file loaded succesfully</returns>
		private bool UpdateLoadModDataFile()
		{
			if (!MyAPIGateway.Utilities.FileExistsInLocalStorage(this.ModDataFile, this.GetType()))
			{
				this.ModData = new MyModDataInfo
				{
					ModVersion = this.ModVersion,
					MaxModSpecificLogFiles = this.MaxModSpecificLogFiles,
					ModLogFiles = new List<MyModLogFileInfo>(),
				};
				return false;
			}

			BinaryReader reader = null;

			try
			{
				reader = MyAPIGateway.Utilities.ReadBinaryFileInLocalStorage(this.ModDataFile, this.GetType());
				byte[] buffer = reader.ReadBytes((int) reader.BaseStream.Length);
				this.ModData = MyAPIGateway.Utilities.SerializeFromBinary<MyModDataInfo>(buffer);
				this.ModData.ModLogFiles.RemoveAll((file) => file.Filename != this.ActiveModLogFile && !MyAPIGateway.Utilities.FileExistsInLocalStorage(file.Filename, this.GetType()));
			}
			catch (Exception e)
			{
				this.ModDataLoadError = new ModFileLoadingException("Failed to deserialize mod data", e);
				this.ModData = new MyModDataInfo
				{
					ModVersion = this.ModVersion,
					MaxModSpecificLogFiles = this.MaxModSpecificLogFiles,
					ModLogFiles = new List<MyModLogFileInfo>(),
				};
			}
			finally
			{
				reader?.Close();
			}

			return true;
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Updates the configuration
		/// </summary>
		/// <param name="configuration">The new mod configuration</param>
		public void UpdateConfiguration(MyModConfigurationV1 configuration)
		{
			if (configuration == null || MyNetworkInterface.IsStandaloneMultiplayerClient) return;
			this.Configuration.Update(new MyModConfigurationV1.MyLocalModConfiguration
			{
				CapacitorConfiguration = configuration.CapacitorConfiguration,
				DriveConfiguration = configuration.DriveConfiguration,
				JumpGateConfiguration = configuration.JumpGateConfiguration,
				ConstructConfiguration = configuration.ConstructConfiguration,
				GeneralConfiguration = configuration.GeneralConfiguration,
			}, configuration.ModSettings);

			if (!this.Configuration.JumpGateConfiguration.JumpGateExplosionConfiguration.EnableGateExplosions.Value && MyJumpGateControllerTerminal.TerminalSection == MyJumpGateControllerTerminal.MyTerminalSection.DETONATION) MyJumpGateControllerTerminal.TerminalSection = MyJumpGateControllerTerminal.MyTerminalSection.JUMP_GATE;
			if (!this.Configuration.JumpGateConfiguration.JumpGateExplosionConfiguration.EnableGateExplosions.Value && MyJumpGateRemoteAntennaTerminal.TerminalSection == MyJumpGateRemoteAntennaTerminal.MyTerminalSection.DETONATION) MyJumpGateRemoteAntennaTerminal.TerminalSection = MyJumpGateRemoteAntennaTerminal.MyTerminalSection.JUMP_GATE;
			this.RedrawAllTerminalControls();

			if (this.Network.Registered)
			{
				MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
				{
					PacketType = MyPacketTypeEnum.UPDATE_CONFIG,
					TargetID = 0,
					Broadcast = true,
				};
				packet.Payload(new MyModConfigurationV1.MyGlobalModConfiguration
				{
					LocalModConfiguration = new MyModConfigurationV1.MyLocalModConfiguration
					{
						CapacitorConfiguration = this.Configuration.CapacitorConfiguration,
						DriveConfiguration = this.Configuration.DriveConfiguration,
						JumpGateConfiguration = this.Configuration.JumpGateConfiguration,
						ConstructConfiguration = this.Configuration.ConstructConfiguration,
						GeneralConfiguration = this.Configuration.GeneralConfiguration,
					},
					ModSettings = this.Configuration.ModSettings,
				});
				packet.Send();
			}
		}

		/// <summary>
		/// Purges all but the current active log file
		/// </summary>
		public void PurgeAllStoredLogFiles()
		{
			for (int i = 0; i < this.ModData.ModLogFiles.Count; ++i)
			{
				MyModLogFileInfo info = this.ModData.ModLogFiles[i];
				if (info.Filename == this.ActiveModLogFile) continue;
				MyAPIGateway.Utilities.DeleteFileInLocalStorage(info.Filename, this.GetType());
				this.ModData.ModLogFiles.RemoveAt(i--);
			}
		}

		/// <summary>
		/// Purges the oldenst log files keeping the newest 'n' files<br />
		/// Current file will not be purged
		/// </summary>
		/// <param name="count">The number of log files to keep or 0 to purge all</param>
		public void PurgeStoredLogFiles(uint count)
		{
			if (count == 0)
			{
				this.PurgeAllStoredLogFiles();
				return;
			}

			List<MyModLogFileInfo> closed = this.ModData.ModLogFiles.Where((info) => info.Filename != this.ActiveModLogFile).OrderBy((info) => info.ModificationTime).Skip((int) count).ToList();
			foreach (MyModLogFileInfo file in closed) MyAPIGateway.Utilities.DeleteFileInLocalStorage(file.Filename, this.GetType());
			this.ModData.ModLogFiles.RemoveAll((info) => closed.Contains(info));
		}

		/// <summary>
		/// Creates a new mod-specific log file<br />
		/// Extra log files will be deleted if max stored log files is exceeded
		/// </summary>
		/// <param name="filename">The new filename</param>
		/// <returns>A file writer</returns>
		public TextWriter CreateNewModSpecificLogFile(string filename)
		{
			uint max_count = this.MaxModSpecificLogFiles;
			this.ActiveModLogFile = filename;
			this.ModData.ModLogFiles.Add(new MyModLogFileInfo(filename));
			this.PurgeStoredLogFiles(max_count);
			return MyAPIGateway.Utilities.WriteFileInLocalStorage(filename, this.GetType());
		}
		#endregion
	}
}
