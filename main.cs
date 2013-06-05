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
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading; 
/* 
* This program establishes a connection to irc server, joins a channel and greets every nickname that
* joins the channel.
*
* Coded by Pasi Havia 17.11.2001 http://koti.mbnet.fi/~curupted
*/ 
class IrcBot
{
  // Irc server to connect 
  public static string SERVER = "fuzzyhunter.jtvirc.com";
  // Irc server's port (6667 is default port)
  private static int PORT = 6667; 
  // User information defined in RFC 2812 (Internet Relay Chat: Client Protocol) is sent to irc server 
  private static string USER = "HugoBot"; 
  // Bot's nickname
  private static string NICK = "HugoBot"; 
  // Channel to join
  private static string CHANNEL = "#fuzzyhunter"; 
  // StreamWriter is declared here so that PingSender can access it
  public static StreamWriter writer; 
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
      stream = irc.GetStream ();
      reader = new StreamReader (stream);
      writer = new StreamWriter (stream); 
      // Start PingSender thread
      PingSender ping = new PingSender ();
      ping.Start (); 
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
          if (inputLine.EndsWith ("JOIN :" + CHANNEL) )
          {
            // Parse nickname of person who joined the channel
            nickname = inputLine.Substring(1, inputLine.IndexOf ("!") - 1);
            // Welcome the nickname to channel by sending a notice
            writer.WriteLine ("NOTICE " + nickname + " :Hi " + nickname + " and welcome to " + CHANNEL + " channel!"); 
            writer.Flush ();
            // Sleep to prevent excess flood
            Thread.Sleep (2000);
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

