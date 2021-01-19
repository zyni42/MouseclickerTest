using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MC=Mouseclicker.Library.Mouseclicker;

namespace MouseclickerTest
{
	class Program
	{
		static void Main(string[] args)
		{
			try
			{
				string inputFileName = "input.txt";

				if (args.Length > 0)
				{
					for (int i = 0; i < args.Length; i++)
					{
						if (i == 0 && !args[0].StartsWith ("/"))
						{
							inputFileName=args[0];
							continue;
						}

						switch (args[i].ToLower ())
						{
							case "/debug":
								MC.ShowDebugLines = true;
								break;
							case "/?":
								PrintUsageInfo ();
								return;
							default:
								throw new ArgumentOutOfRangeException ("args[i]", "Specified argument is invalid.");
						}
					}
				}

				MC.ExecuteInputCommands (inputFileName);
			}
			catch (Exception ex)
			{
				Console.WriteLine ();
				Console.WriteLine ("ERROR:");
				Console.WriteLine (ex);
				Console.WriteLine ();
				Console.WriteLine ("(press any key to exit)");
				Console.ReadKey ();
			}
		}

		static void PrintUsageInfo ()
		{
			Console.WriteLine ();
			Console.WriteLine ("Usage: MouseclickerTest.ext [<inputfile>] [params]");
			Console.WriteLine ("<inputfile> ... File with input commands specified. Default = input.txt");
			Console.WriteLine ("                Format:");
			Console.WriteLine ("                  [coords=abs|rel] ............... Coordinate type (optional, line 1)");
			Console.WriteLine ("                  [res=<width>x<height>] ......... Resolution, only necessary if coords=abs (optional, line 2)");
			Console.WriteLine ("                  <x> <y> [ldown] [rdown] ........ Move mouse, optionally pressing left/right mouse button");
			Console.WriteLine ("                  wait <msec> .................... Wait for <msec> milliseconds");
			Console.WriteLine ("Params (optional):");
			Console.WriteLine ("/debug ........ Enable debug output in console");
			Console.WriteLine ("/? ............ Print this usage info");
			Console.WriteLine ();
			Console.WriteLine ("(press any key to exit)");
			Console.ReadKey ();
		}
	}
}