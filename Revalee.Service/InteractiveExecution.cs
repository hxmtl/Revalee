﻿using System;
using System.Diagnostics;
using System.Reflection;
using System.Security;
using System.Security.Principal;
using System.ServiceProcess;
using System.Threading;

namespace Revalee.Service
{
	internal sealed class InteractiveExecution
	{
		private static string _ConsoleStatusMessage = string.Empty;

		private InteractiveExecution()
		{
		}

		public static void Run()
		{
			Console.WriteLine("===] REVALEE [===  v{0}", GetVersionNumber());
			Console.WriteLine("                   {0}", GetCopyrightInformation());
			Console.WriteLine();

			if (!CheckInstallation())
			{
				return;
			}

			WriteStatusMessage("Initializing...");

			try
			{
				Supervisor.Start();
			}
			catch (Exception ex)
			{
				Console.WriteLine();
				Console.WriteLine("Revalee service cannot start due to the following critical error:");
				Console.WriteLine("*  {0}", ex.Message);
				Console.WriteLine();
				Console.WriteLine("Press any key to terminate.");
				Console.ReadKey(true);
				throw;
			}

			try
			{
				ClearStatusMessage();

				if (Supervisor.Configuration.ListenerPrefixes.Count == 0)
				{
					Console.WriteLine("Revalee service is running but is not listening for callback requests.");
				}
				else if (Supervisor.Configuration.ListenerPrefixes.Count == 1)
				{
					Console.WriteLine("Revalee service is running and listening on {0}.", Supervisor.Configuration.ListenerPrefixes[0]);
				}
				else
				{
					Console.WriteLine("Revalee service is running and listening on:");

					foreach (ListenerPrefix prefix in Supervisor.Configuration.ListenerPrefixes)
					{
						Console.WriteLine("    {0}", prefix);
					}
				}

				Console.WriteLine("Press any key to terminate.");
				Console.ReadKey(true);
				Console.WriteLine();
			}
			finally
			{
				WriteStatusMessage("Service stopping...");

				try
				{
					Supervisor.Stop();
				}
				catch (ThreadAbortException)
				{
					// Ignore thread abort exceptions on shutdown
				}
				catch (ObjectDisposedException)
				{
					// Ignore object disposed exceptions on shutdown
				}

				ClearStatusMessage();
				Console.WriteLine();
			}
		}

		public static void Help()
		{
			Console.WriteLine("===] REVALEE [===  v{0}", GetVersionNumber());
			Console.WriteLine("                   {0}", GetCopyrightInformation());
			Console.WriteLine();
			Console.WriteLine("Switches:");
			Console.WriteLine("    -install       Installs the service into the Windows Service Manager.");
			Console.WriteLine("    -uninstall     Uninstalls the service from the Windows Service Manager.");
			Console.WriteLine("    -help          Displays this information.");
		}

		private static string GetVersionNumber()
		{
			try
			{
				return FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;
			}
			catch
			{
				return string.Empty;
			}
		}

		private static string GetCopyrightInformation()
		{
			try
			{
				return ((AssemblyCopyrightAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false)[0]).Copyright.Replace("©", "(c)");
			}
			catch
			{
				return string.Empty;
			}
		}

		private static bool CheckInstallation()
		{
			if (IsServiceInstalled())
			{
				return true;
			}
			else if (HasRequiredInstallationPermission())
			{
				Console.WriteLine();
				Console.WriteLine("*  The Revalee service cannot run interactively until it is installed.");
				Console.Write("*  Would you like to install it now? [y/n] >");
				ConsoleKeyInfo keypress = Console.ReadKey(false);

				if (keypress.Key == ConsoleKey.Y)
				{
					var installer = new CommandLineInstaller();
					installer.Install();

					Console.WriteLine();
					Console.WriteLine();
					return true;
				}
				else
				{
					return false;
				}
			}
			else
			{
				Console.WriteLine();
				Console.WriteLine("*  The Revalee service must be installed before it can be run interactively.");
				Console.WriteLine("*  However, the current permission level is not high enough to install now.");
				Console.WriteLine("*  Please run this executable again with elevated privileges to install.");
				return false;
			}
		}

		private static bool IsServiceInstalled()
		{
			ServiceController serviceController = new ServiceController(Supervisor.Configuration.ServiceName);
			try
			{
				string ServiceName = serviceController.ServiceName;
				return true;
			}
			catch (InvalidOperationException)
			{
				return false;
			}
			finally
			{
				serviceController.Close();
			}
		}

		private static bool HasRequiredInstallationPermission()
		{
			if (!(new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator)))
			{
				return false;
			}
			try
			{
				EventLog.SourceExists(Assembly.GetExecutingAssembly().GetName().Name);
				return true;
			}
			catch (SecurityException)
			{
				return false;
			}
		}

		private static void WriteStatusMessage(string text)
		{
			ClearStatusMessage();

			if (!string.IsNullOrEmpty(text))
			{
				_ConsoleStatusMessage = text;
				Console.Write(text);
			}
		}

		private static void ClearStatusMessage()
		{
			if (!string.IsNullOrEmpty(_ConsoleStatusMessage))
			{
				Console.Write(new string('\b', _ConsoleStatusMessage.Length));
				Console.Write(new string(' ', _ConsoleStatusMessage.Length));
				Console.Write(new string('\b', _ConsoleStatusMessage.Length));
				_ConsoleStatusMessage = string.Empty;
			}
		}
	}
}