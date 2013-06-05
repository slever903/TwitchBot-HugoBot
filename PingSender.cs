using System;
using System.Threading; 
/*
* Class that sends PING to irc server every 15 seconds
*/
class PingSender
{
  static string PING = "PING :";
  private Thread pingSender;
  // Empty constructor makes instance of Thread
  public PingSender () 
  {
    pingSender = new Thread (new ThreadStart (this.Run) ); 
  } 
  // Starts the thread
  public void Start () 
  { 
    pingSender.Start (); 
  } 
  // Send PING to irc server every 15 seconds
  public void Run ()
  {
    while (true)
    {
      IrcBot.writer.WriteLine (PING + IrcBot.SERVER);
      IrcBot.writer.Flush ();
      Thread.Sleep (15000);
    }
  }
}
