/** Made for ShatteredWolf twitch chat to connect via IRC **/

//TODO: Everything man

//Connect to server is priority

/**
#DEFINE UserName HugoBot
//#DEFINE Password LolPassword
#DEFINE Owner ShatteredWolf

private enum userLevel {
Owner=1,
Mod,
User,
}; **/
//For now I'll leave this here

using System;
using System.Xml;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading; 
/* 
* Connection work coded by Pasi Havia 17.11.2001 http://koti.mbnet.fi/~curupted
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
	private static enum userLevels {Owner=1, Mod, Regular, User, Dicklist};
	
	private string ReadXml (string command)
	{
		//TODO add reader commands to check 1) if a command is real 2) check the needed userlevel 3) push back the result
		return "Not found";
	}

	private void saveCommands()
	{
		using (XmlWriter writer = XmlWriter.Create("commands.xml"))
		{
			writer.WriteStartDocument();
			writer.WriteStartElement("Commands");

			foreach (var command in commands)
			{
				writer.WriteStartElement("Command");

				writer.WriteElementString("Trigger", command.Key);
				writer.WriteElementString("Response", command.Value);

				writer.WriteEndElement();
			}

			writer.WriteEndElement();
			writer.WriteEndDocument();
		}
	}

	static void Main (string[] args)
	{ 
		NetworkStream stream;
		TcpClient irc;
		string inputLine;
		StreamReader reader;
		string nickname; 
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

