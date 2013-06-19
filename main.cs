/** Made for ShatteredWolf twitch chat to connect via IRC 
* Connection work coded by Pasi Havia 17.11.2001 http://koti.mbnet.fi/~curupted
* Bulk of the command and saving coding done by FuzzyHunter
* Moral support by slever
**/ 

using System;
using System.Xml;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Text.RegularExpressions;

class IrcBot
{
	// Irc server to connect 
    public static string SERVER = "199.9.250.229";
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
    
	private static void loadCommands()
	{
		commands = new List<Command>();

		XmlTextReader reader = new XmlTextReader("commands.xml");
		reader.WhitespaceHandling = WhitespaceHandling.None;

		while (reader.Read())
		{
			if (reader.LocalName == "Command")
			{
				Console.WriteLine("Loading " + reader.GetAttribute(0) + "...");
				commands.Add(new Command(reader.GetAttribute(0), (userLevels)Convert.ToInt32(reader.GetAttribute(1)), reader.GetAttribute(2)));
			}
		}

		reader.Close();
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
				writer.WriteAttributeString("Level", ((int)command.level).ToString());
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

		loadCommands();

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
                    //strip out the nickname of the person who used the command
					//Console.WriteLine(inputLine);

					//if (inputLine.EndsWith ("JOIN " + CHANNEL) )
					//{
					//    nickname = inputLine.Substring(1, inputLine.IndexOf("!") - 1);

					//    // Welcome the nickname to channel by sending a notice
					//    writer.WriteLine ("PRIVMSG " + CHANNEL + " :Hi " + nickname + " and welcome to " + CHANNEL + " channel!"); 
					//    writer.Flush ();
					//    Console.WriteLine("Hi " + nickname);
					//    // Sleep to prevent excess flood
					//    Thread.Sleep (10000);
					//}

					if (inputLine.Contains(":!addcom"))
					{
						string commandInput = inputLine.Substring(inputLine.IndexOf(":!") + 1);

						string[] words = commandInput.Split(' ');
						string trigger = words[1];
						List<string> responseWords = new List<string>();

						for(int i = 2; i < words.Length; i++)
						{
							responseWords.Add(words[i]);
						}

						string response = String.Join(" ", responseWords);

						commands.Add(new Command(trigger, userLevels.User, response));
						saveCommands();

						Console.WriteLine("Add command: " + trigger + ", " + response);
					}

                    else if (inputLine.Contains(":!"))
                    {
						nickname = inputLine.Substring(1, inputLine.IndexOf("!") - 1);
						string commandInput = inputLine.Substring(inputLine.IndexOf(":!") + 1);

						foreach (Command command in commands)
						{
							if (command.trigger == commandInput)
							{
								Console.WriteLine(nickname + " - " + commandInput + " - " + command.response);
								writer.WriteLine("PRIVMSG " + CHANNEL + " :" + command.response);
								writer.Flush();
								break;
							}
						}
                    }
				}

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

