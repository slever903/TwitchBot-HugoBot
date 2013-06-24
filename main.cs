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
	private static string PASS = "LolPassword123";
	// Bot's nickname
	private static string NICK = "ShatteredBot"; 
	// Channel to join
	private static string CHANNEL = "#fuzzyhunter";
	// Broadcaster
	private static string BROADCASTER = CHANNEL.Trim('#');

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

	private static bool isMod(string nickname, StreamReader reader)
	{
		if (nickname == BROADCASTER)
			return true;

		else
		{
			writer.WriteLine("PRIVMSG " + CHANNEL + " :.mods");
			//writer.Flush();
			string inputLine = reader.ReadLine();
			string modList = inputLine.Substring(inputLine.IndexOf("are:") + 5);
			string[] mods = modList.Split(' ');

			for (int i = 0; i < mods.Length; i++)
			{
				mods[i] = mods[i].Trim(',');
			}

			foreach (string mod in mods)
			{
				if (nickname == mod)
					return true;
			}
		}

		return false;
	}

	static void Main (string[] args)
	{ 
		NetworkStream stream;
		TcpClient irc;
		string inputLine;
		StreamReader reader;
		string nickname;
		bool shouldWelcome = true;

		loadCommands();

		try
		{
			irc = new TcpClient (SERVER, PORT);
			Console.WriteLine("Connected");
			stream = irc.GetStream ();
			reader = new StreamReader (stream);
			writer = new StreamWriter (stream);

			writer.AutoFlush = true;

			// Start PingSender thread
			PingSender ping = new PingSender ();
			ping.Start (); 
			
			writer.WriteLine ("PASS " + PASS);
			//writer.Flush ();
			writer.WriteLine (USER);
			//writer.Flush ();
			writer.WriteLine ("NICK " + NICK);
			//writer.Flush ();
			writer.WriteLine ("JOIN " + CHANNEL);
			//writer.Flush ();

			while (true)
			{ 
				while ( (inputLine = reader.ReadLine () ) != null )
				{
					//Write out all IRC input
					//Console.WriteLine(inputLine);

					if (inputLine.EndsWith("JOIN " + CHANNEL) && shouldWelcome)
					{
						nickname = inputLine.Substring(1, inputLine.IndexOf("!") - 1);

						if (nickname != USER.ToLower())
						{
							// Welcome the nickname to channel by sending a notice
							writer.WriteLine("PRIVMSG " + CHANNEL + " :/me Hi " + nickname + ". Welcome to " + BROADCASTER + "'s channel!");
							//writer.Flush();
							Console.WriteLine("Welcomed " + nickname);
						}

						// Sleep to prevent excess flood
						Thread.Sleep(500);
					}

                    if (inputLine.Contains(":!"))
                    {
						nickname = inputLine.Substring(1, inputLine.IndexOf("!") - 1);

						if (inputLine.Contains(":!welcome") && isMod(nickname, reader))
						{
							shouldWelcome = !shouldWelcome;
							Console.WriteLine(nickname + " - Welcome toggled - " + shouldWelcome);

							if (shouldWelcome)
								writer.WriteLine("PRIVMSG " + CHANNEL + " :" + nickname + "-> Welcoming enabled");
							else
								writer.WriteLine("PRIVMSG " + CHANNEL + " :" + nickname + "-> Welcoming disabled");

							//writer.Flush();
						}

						else if (inputLine.Contains(":!addcom") && isMod(nickname, reader))
						{
							string commandInput = inputLine.Substring(inputLine.IndexOf(":!") + 1);

							string[] words = commandInput.Split(' ');
							string trigger = words[1];
							List<string> responseWords = new List<string>();
							int responseIndex = 2;
							userLevels level = userLevels.User;

							if (commandInput.Contains("ul="))
							{
								responseIndex = 3;
								trigger = words[2];

								string levelInput = words[1];
								levelInput = levelInput.Replace("ul=", "");
								//Console.WriteLine(levelInput);

								if (levelInput == "owner")
									level = userLevels.Owner;
								else if (levelInput == "mod")
									level = userLevels.Mod;
								else if (levelInput == "reg")
									level = userLevels.Regular;
							}

							for (int i = responseIndex; i < words.Length; i++)
							{
								responseWords.Add(words[i]);
							}

							string response = String.Join(" ", responseWords);

							commands.Add(new Command(trigger, level, response));
							saveCommands();

							Console.WriteLine(nickname + " - Adding command - " + trigger + ", " + response);

							writer.WriteLine("PRIVMSG " + CHANNEL + " :" + nickname + "-> Added command " + trigger);
							//writer.Flush();
						}
						else if (inputLine.Contains("!delcom") && isMod(nickname, reader))
						{
							string commandInput = inputLine.Substring(inputLine.IndexOf(":!") + 1);

							string[] words = commandInput.Split(' ');
							string trigger = words[1];

							for (int i = commands.Count - 1; i >= 0; i--)
							{
								if (commands[i].trigger == trigger)
								{
									Console.WriteLine(nickname + " - Deleting command - " + trigger);
									commands.RemoveAt(i);
									saveCommands();

									writer.WriteLine("PRIVMSG " + CHANNEL + " :" + nickname + "-> Deleted command " + trigger);
									//writer.Flush();

									break;
								}
							}
						}
						else if (inputLine.Contains("!editcom") && isMod(nickname, reader))
						{
							string commandInput = inputLine.Substring(inputLine.IndexOf(":!") + 1);

							string[] words = commandInput.Split(' ');
							string trigger = words[1];
							List<string> responseWords = new List<string>();
							int responseIndex = 2;
							userLevels level = userLevels.User;

							if (commandInput.Contains("ul="))
							{
								responseIndex = 3;
								trigger = words[2];

								string levelInput = words[1];
								levelInput = levelInput.Replace("ul=", "");
								//Console.WriteLine(levelInput);

								if (levelInput == "owner")
									level = userLevels.Owner;
								else if (levelInput == "mod")
									level = userLevels.Mod;
								else if (levelInput == "reg")
									level = userLevels.Regular;
							}

							for (int i = responseIndex; i < words.Length; i++)
							{
								responseWords.Add(words[i]);
							}

							string response = String.Join(" ", responseWords);

							for (int i = 0; i < commands.Count; i++)
							{
								if (commands[i].trigger == trigger)
								{
									commands.Add(new Command(trigger, level, response));
									commands.Remove(commands[i]);
									break;
								}
							}
							saveCommands();

							Console.WriteLine(nickname + " - Editing command - " + trigger + ", " + response);

							writer.WriteLine("PRIVMSG " + CHANNEL + " :" + nickname + "-> Edited command " + trigger);
						}
						else
						{
							string commandInput = inputLine.Substring(inputLine.IndexOf(":!") + 1);

							foreach (Command command in commands)
							{
								if (command.trigger == commandInput)
								{
									if ((command.level < userLevels.Regular && isMod(nickname, reader)) || command.level >= userLevels.Regular)
									{
										Console.WriteLine(nickname + " - " + commandInput + " - " + command.response);
										writer.WriteLine("PRIVMSG " + CHANNEL + " :" + command.response);
										//writer.Flush();
										break;
									}
								}
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

