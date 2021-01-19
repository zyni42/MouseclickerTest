using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading;

namespace Mouseclicker.Library
{
    public class Mouseclicker
    {
		#region Win32 API definitions
		[Flags]
		private enum DwMouseFlags
		{
			/// <summary>
			/// The dx and dy members contain normalized absolute coordinates. If the flag is not set, dxand dy contain relative data (the change in position since the last reported position). This flag can be set, or not set, regardless of what kind of mouse or other pointing device, if any, is connected to the system. For further information about relative mouse motion, see the following Remarks section.
			/// </summary>
			MOUSEEVENTF_ABSOLUTE = 0x8000,
			
			/// <summary>
			/// The wheel was moved horizontally, if the mouse has a wheel. The amount of movement is specified in mouseData.
			/// Windows XP/2000:  This value is not supported.
			/// </summary>
			MOUSEEVENTF_HWHEEL = 0x01000,

			/// <summary>
			/// Movement occurred.
			/// </summary>
			MOUSEEVENTF_MOVE = 0x0001,
			
			/// <summary>
			/// The WM_MOUSEMOVE messages will not be coalesced. The default behavior is to coalesce WM_MOUSEMOVE messages.
			/// Windows XP/2000:  This value is not supported.
			/// </summary>
			MOUSEEVENTF_MOVE_NOCOALESCE = 0x2000,

			/// <summary>
			/// The left button was pressed.
			/// </summary>
			MOUSEEVENTF_LEFTDOWN = 0x0002,
			
			/// <summary>
			/// The left button was released.
			/// </summary>
			MOUSEEVENTF_LEFTUP = 0x0004,
			
			/// <summary>
			/// The right button was pressed.
			/// </summary>
			MOUSEEVENTF_RIGHTDOWN = 0x0008,
			
			/// <summary>
			/// The right button was released.
			/// </summary>
			MOUSEEVENTF_RIGHTUP = 0x0010,
			
			/// <summary>
			/// The middle button was pressed.
			/// </summary>
			MOUSEEVENTF_MIDDLEDOWN = 0x0020,
			
			/// <summary>
			/// The middle button was released.
			/// </summary>
			MOUSEEVENTF_MIDDLEUP = 0x0040,
			
			/// <summary>
			/// Maps coordinates to the entire desktop. Must be used with MOUSEEVENTF_ABSOLUTE.
			/// </summary>
			MOUSEEVENTF_VIRTUALDESK = 0x4000,
			
			/// <summary>
			/// The wheel was moved, if the mouse has a wheel. The amount of movement is specified in mouseData.
			/// </summary>
			MOUSEEVENTF_WHEEL = 0x0800,
			
			/// <summary>
			/// An X button was pressed.
			/// </summary>
			MOUSEEVENTF_XDOWN = 0x0080,
			
			/// <summary>
			/// An X button was released.
			/// </summary>
			MOUSEEVENTF_XUP = 0x0100
		}

		private enum InputType
		{
			/// <summary>
			/// The event is a mouse event. Use the mi structure of the union.
			/// </summary>
			INPUT_MOUSE = 0,
			
			/// <summary>
			/// The event is a keyboard event. Use the ki structure of the union.
			/// </summary>
			INPUT_KEYBOARD = 1,
			
			/// <summary>
			/// The event is a hardware event. Use the hi structure of the union.
			/// </summary>
			INPUT_HARDWARE = 2
		}

		private struct MouseInput
		{
			public int X;
			public int Y;
			public uint MouseData;
			public uint Flags;
			public uint Time;
			public IntPtr ExtraInfo;
		}

		private struct Input
		{
			public int Type;
			public MouseInput MouseInput;
		}

		[DllImport("user32.dll", SetLastError = true)]
		private static extern uint SendInput(uint numInputs, Input[] inputs, int size);
		#endregion

		#region Properties
		public static bool ShowDebugLines { get; set; } = false;
		#endregion

		#region Methods
		public static void SendMouseInput (int posX, int posY, int maxX=0, int maxY=0, bool moveAbs=false, bool leftDown=false, bool rightDown=false)
		{

			Input[] i = new Input[2];

			// move the mouse to the position specified
			i[0] = new Input();
			i[0].Type = (int)InputType.INPUT_MOUSE;
			i[0].MouseInput.Flags = (uint)DwMouseFlags.MOUSEEVENTF_MOVE;
			if (moveAbs)
			{
				i[0].MouseInput.Flags |= (uint)DwMouseFlags.MOUSEEVENTF_ABSOLUTE;
				i[0].MouseInput.X = 65535 / maxX * posX;
				i[0].MouseInput.Y = 65535 / maxY * posY;
			}
			else
			{
				i[0].MouseInput.X = posX;
				i[0].MouseInput.Y = posY;
			}

			// determine if we need to send a mouse down or mouse up event
			if(leftDown || lastLeftDown || rightDown || lastRightDown)
			{
				i[1] = new Input();
				i[1].Type = (int)InputType.INPUT_MOUSE;
				i[1].MouseInput.Flags = 0;

				if (leftDown)			i[1].MouseInput.Flags |= (uint)DwMouseFlags.MOUSEEVENTF_LEFTDOWN;
				else if (lastLeftDown)	i[1].MouseInput.Flags |= (uint)DwMouseFlags.MOUSEEVENTF_LEFTUP;
				
				if (rightDown)			i[1].MouseInput.Flags |= (uint)DwMouseFlags.MOUSEEVENTF_RIGHTDOWN;
				else if (lastRightDown)	i[1].MouseInput.Flags |= (uint)DwMouseFlags.MOUSEEVENTF_RIGHTUP;
			}

			lastLeftDown = leftDown;
			lastRightDown = rightDown;

			// send it off
			uint result = SendInput((uint)i.Length, i, Marshal.SizeOf(i[0]));
			if(result == 0)
				throw new Win32Exception(Marshal.GetLastWin32Error());
		}

		public static void ExecuteInputCommands (string fileName)
		{
			if (string.IsNullOrWhiteSpace (fileName)) throw new ArgumentNullException (fileName);
			if (!File.Exists (fileName)) throw new FileNotFoundException ("Specified input command file not found.", fileName);

			WriteDebugLine ($"Processing input file \"{fileName}\"...");
			using (var sr = new StreamReader (fileName))
			{
				string line;
				int lineNr=0;
				bool moveAbs=false;
				int maxX=0;
				int maxY=0;
				while (!sr.EndOfStream)
				{
					line = sr.ReadLine().ToLower().Trim();
					lineNr++;
					if (string.IsNullOrWhiteSpace (line)) continue;

					// Optionally Parse first and second line
					if (lineNr <= 2)
					{
						if (line.StartsWith ("coords"))			// optional: coords=abs|rel
						{
							var splitted = line.Split(new string [] {"="}, StringSplitOptions.RemoveEmptyEntries);
							if (splitted.Length == 2) moveAbs = (splitted[1] == "abs");
							WriteDebugLine ($"Using {((moveAbs) ? "absolute" : "relative")} coordinates.");
							continue;
						}
						else if (line.StartsWith ("res"))		// optional: res=<width>x<height>
						{
							string[] splitted = line.Split (new string [] {"="}, StringSplitOptions.RemoveEmptyEntries);
							if (splitted.Length == 2)
							{
								var resolution = splitted[1].Split (new string [] {"x"}, StringSplitOptions.RemoveEmptyEntries);
								if (resolution.Length == 2)
								{
									if (!int.TryParse (resolution[0], out maxX)) throw new ArgumentOutOfRangeException ("res", $"Specified width \"{resolution[0]}\" is not a valid integer.");
									if (!int.TryParse (resolution[1], out maxY)) throw new ArgumentOutOfRangeException ("res", $"Specified height \"{resolution[0]}\" is not a valid integer.");
								}
							}
							WriteDebugLine ($"Using resolution (maxX, maxY): {maxX}x{maxY}.");
							continue;
						}
					}
					if (lineNr > 2 && moveAbs && (maxX == 0 && maxY == 0)) throw new InvalidOperationException ($"Movement is abolute, but resolution is 0x0 (did you specify \"res=<width>x<height>\" correctly?).");

					// Process data lines
					// <x> <y> [ldown] [rdown]
					// wait <msecs>
					var commands = line.Split (new string [] {" "}, StringSplitOptions.RemoveEmptyEntries);
					if (commands.Length < 2 || commands.Length > 4) throw new InvalidDataException ($"Line {lineNr}, seems to be invalid: \"{line}\".");

					if (commands.Length == 2 && commands[0] == "wait")
					{
						int msecs;
						if (!int.TryParse (commands[1], out msecs)) throw new ArgumentOutOfRangeException ($"Line {lineNr}, invalid wait time specified: \"{commands[1]}\".");
						WriteDebugLine ($"Waiting {msecs} milliseconds...");
						Thread.Sleep (msecs);
						continue;
					}

					int posX, posY;
					if (!int.TryParse (commands[0], out posX)) throw new ArgumentOutOfRangeException ("x", $"Specified x coordinate \"{commands[0]}\" is invalid.");
					if (!int.TryParse (commands[1], out posY)) throw new ArgumentOutOfRangeException ("y", $"Specified y coordinate \"{commands[1]}\" is invalid.");
					bool ldown = (commands.Length > 2 && commands[2] == "ldown") || (commands.Length > 3 && commands[3] == "ldown");
					bool rdown = (commands.Length > 2 && commands[2] == "rdown") || (commands.Length > 3 && commands[3] == "rdown");

					WriteDebugLine ($"Sending mouse input: x={posX}, y={posY}, ldown={ldown}, rdown={rdown}");
					if (moveAbs)	SendMouseInput (posX, posY, maxX, maxY, true, ldown, rdown);
					else			SendMouseInput (posX, posY, leftDown:ldown, rightDown:rdown);
				}
			}
		}

		private static void WriteDebugLine (string debugLine)
		{
			if (ShowDebugLines) Console.WriteLine (debugLine);
		}
		#endregion

		#region Fields
		private static bool lastLeftDown;
		private static bool lastRightDown;
		#endregion
	}
}
