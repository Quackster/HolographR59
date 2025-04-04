using System;
using System.Security.Permissions;
using Zero.Core;

namespace Zero;

public class Program
{
	// [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlAppDomain)]
	public static void Main(string[] args)
	{
		AppDomain currentDomain = AppDomain.CurrentDomain;
		currentDomain.UnhandledException += MyHandler;
		try
		{
			HolographEnvironment.Initialize();
			while (true)
			{
				CommandParser.Parse(Console.ReadLine());
			}
		}
		catch (Exception ex)
		{
			Console.Write("Erro: " + ex.Message);
			Console.ReadKey(intercept: true);
		}
	}

	private static void MyHandler(object sender, UnhandledExceptionEventArgs args)
	{
		Exception e = (Exception)args.ExceptionObject;
		Console.WriteLine("Erro Ocorrido! Especificações: " + e.ToString());
		HolographEnvironment.Destroy();
		Console.ReadKey(intercept: true);
		Environment.Exit(2);
		HolographEnvironment.Initialize();
	}
}
