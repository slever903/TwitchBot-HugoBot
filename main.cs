/** Made for ShatteredWolf twitch chat to connect via IRC **/

using System;
using System.Xml;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Collections.Generic; 
/* 
* Connection work coded by Pasi Havia 17.11.2001 http://koti.mbnet.fi/~curupted
* Bulk of the command and saving coding done by FuzzyHunter
* Moral support by slever
*/ 
class IrcBot
{
	// Irc server to connect 
	public static string SERVER = "fuzzyhunter.jtvirc.com";
	// Irc server's port (6667 is default port)
	private static int PORT = 6667; 
	// User information defined in RFC 2812 (Internet Relay Chat: Client Protocol) is sent to irc server 
	private static string USER = "ShatteredBot"; 
	// Password for the server
	private static string PASS = "LolPassword";
	// Bot's nickname
	private static string NICK = "ShatteredBot"; 
	// Channel to join
	private static string CHANNEL = "#fuzzyhunter"; 
	// StreamWriter is declared here so that PingSender can access it
	public static StreamWriter writer; 
	public enum userLevels {Owner=1, Mod, Regular, User, Dicklist};

	public struct Command
	{
		public string trigger;
		public userLevels level;
		public string response;

		public Command(string trigger, userLevels level, string response)
		{
			this.trigger = trigger;
			this.level = level;
			this.response = response;
		}
	};
	private static List<Command> commands;

	private string readXml (string command)
	{
		//TODO add reader commands to check 1) if a command is real 2) check the needed userlevel 3) push back the result
		return "Not found";
	}

	private void loadCommands()
	{
		commands = new List<Command>();

		XmlTextReader reader = new XmlTextReader("commands.xml");
		reader.WhitespaceHandling = WhitespaceHandling.None;

		while (reader.Read())
		{

		}
	}

	private static void saveCommands()
	{
		XmlWriterSettings settings = new XmlWriterSettings();
		settings.Indent = true;
		settings.IndentChars = "\t";

		using (XmlWriter writer = XmlWriter.Create("commands.xml", settings))
		{
			writer.WriteStartDocument();
			writer.WriteStartElement("Commands");

			foreach (Command command in commands)
			{
				writer.WriteStartElement("Command");

				writer.WriteAttributeString("Trigger", command.trigger);
				writer.WriteAttributeString("Level", command.level.ToString());
				writer.WriteAttributeString("Response", command.response);

				writer.WriteEndElement();
			}

			writer.WriteEndElement();
			writer.WriteEndDocument();
		}

		Console.WriteLine("Commands saved");
	}

	static void Main (string[] args)
	{ 
		NetworkStream stream;
		TcpClient irc;
		string inputLine;
		StreamReader reader;
		string nickname;

		commands = new List<Command>();
		commands.Add(new Command("!test", userLevels.User, "Testing the XML"));
		commands.Add(new Command("!test3", userLevels.Mod, "Testing again"));
		saveCommands();

		try
		{
			irc = new TcpClient (SERVER, PORT);
			Console.WriteLine("Connected");
			stream = irc.GetStream ();
			reader = new StreamReader (stream);
			writer = new StreamWriter (stream); 
			// Start PingSender thread
			PingSender ping = new PingSender ();
			ping.Start (); 
			writer.WriteLine ("PASS " + PASS);
			writer.Flush ();
			writer.WriteLine (USER);
			writer.Flush ();
			writer.WriteLine ("NICK " + NICK);
			writer.Flush ();
			writer.WriteLine ("JOIN " + CHANNEL);
			writer.Flush (); 
			while (true)
			{ 
				while ( (inputLine = reader.ReadLine () ) != null )
				{
					if (inputLine.EndsWith ("JOIN " + CHANNEL) )
					{
						// Parse nickname of person who joined the channel
						nickname = inputLine.Substring(1, inputLine.IndexOf ("!") - 1);
						// Welcome the nickname to channel by sending a notice
						writer.WriteLine ("PRIVMSG " + CHANNEL + " :Hi " + nickname + " and welcome to " + CHANNEL + " channel!"); 
						writer.Flush ();
						Console.WriteLine("Hi " + nickname);
						// Sleep to prevent excess flood
						Thread.Sleep (10000);
					}
				}
				Console.WriteLine("End While");
				// Close all streams
				writer.Close ();
				reader.Close ();
				irc.Close ();
			}
		}
		catch (Exception e)
		{
		// Show the exception, sleep for a while and try to establish a new connection to irc server
		Console.WriteLine (e.ToString () );
		Thread.Sleep (5000);
		string[] argv = { };
		Main (argv);
		}
	}


} 

