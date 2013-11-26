using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Net.Sockets;
using CustomNetworking;
using BB;
using System.Threading;
using System.Diagnostics;

namespace BS
{
    /// <summary>
    /// A BoggleServer is used to connect clients to play a Boggle game. It is asyncronous and 
    /// non-blocking. When this application is run, the Boggle Server will run independently 
    /// waiting for connection requests, processing them, and sending them to be handled in a 
    /// different thread. 
    /// </summary>
    public class BoggleServer
    {
        #region Main Method Used to Drive the BoggleServer
        /// <summary>
        /// Main method called upon when the user runs this program. Here we will initialize a 
        /// new BoggleServer with the command line args and run the server.
        /// </summary>
        public static void Main(string[] args)
        {
            // If the user supplied less than two args or more than three,
            // then print an error message and terminate.
            if (args.Length < 2 || args.Length > 3)
            {
                Console.WriteLine("Error: Invalid arguments.");
                Console.WriteLine("usage: BoggleServer time dictionary_path optional_string");
                return;
            }

            // Otherwise the user must have provided the correct number of
            // arguments. Continue forward..

            // Parse the arguments and ensure that they are valid. If they are not, then
            // print a descriptive error message and terminate the program.

            // If the first argument wasn't an integer, then print an error message
            // and terminate.
            int GameLength = 0;
            if (!int.TryParse(args[0], out GameLength) || GameLength <= 0)
            {
                Console.WriteLine("Error: Invalid arguments.");
                Console.WriteLine("usage: BoggleServer time dictionary_path optional_string");
                return;
            }

            // At this point, the first argument must be an integer value. Continue parsing the
            // remaining arguments..

            // If the second argument (the filepath) wasn't a valid filepath, then print an error
            // message and terminate.
            if (!File.Exists(args[1]))
            {
                Console.WriteLine("Error: You must provide a valid file path!");
                return;
            }


            // If there was a third argument, then it must be atleast 16 characters and it must only
            // contains letters.
            if (args.Length == 3 && (args[2].Length != 16 || !args[2].All(Char.IsLetter)))
            {
                args[2].All(Char.IsLetter);
                Console.WriteLine("Error: Invalid arguments.");
                Console.WriteLine("usage: BoggleServer time dictionary_path optional_string");
                return;
            }

            // After paramter validation, start the Boggle game server.
            BoggleServer GameServer;
            if (args.Length == 2)
                GameServer = new BoggleServer(GameLength, args[1], null);
            else
                GameServer = new BoggleServer(GameLength, args[1], args[2]);


            Console.Read();
        }
        #endregion

        // Member variables used to organize a BoggleServer:
        private TcpListener UnderlyingServer;
        private int GameLength;
        private HashSet<string> DictionaryWords;
        private PlayerData WaitingPlayer;
        private string OptionalString;
        private readonly Object CommandReceived;
        private readonly Object ConnectionReceivedLock;

        /// <summary>
        /// Constructor used to initialize a new BoggleServer. This will initialize the GameLength
        /// and it will build a dictionary of all of the valid words from the DictionaryPath. If an
        /// optional string was passed to this application, then it will build a BoggleBoard from
        /// the supplied string. Otherwise it will build a BoggleBoard randomly.
        /// </summary>
        /// <param name="GameLength">The length that the Boggle game should take.</param>
        /// <param name="DictionaryPath">The filepath to the dictionary that should be used to
        /// compare words against.</param>
        /// <param name="OptionalString">An optional string to construct a BoggleBoard with.</param>
        public BoggleServer(int GameLength, string DictionaryPath, string OptionalString)
        {
            try
            {
                this.UnderlyingServer = new TcpListener(IPAddress.Any, 2000);
                this.GameLength = GameLength;
                this.DictionaryWords = new HashSet<string>(File.ReadAllLines(DictionaryPath));
                this.WaitingPlayer = null;

                this.CommandReceived = new Object();
                this.ConnectionReceivedLock = new Object();

                if (OptionalString != null)
                    this.OptionalString = OptionalString;

                Console.WriteLine("The Server has Started on Port 2000");

                // Start the server and begin accepting clients.
                UnderlyingServer.Start();
                UnderlyingServer.BeginAcceptSocket(ConnectionReceived, null);

            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// The callback fired by the UnderlyingServer when a new connection is received.
        /// This will create a new socket from the connection request and wrap the socket
        /// in a StringSocket so that the server and client can communicate via strings.
        /// After creating the StringSocket the server begins accepting command from the 
        /// client and continues to accept more connections.
        /// </summary>
        private void ConnectionReceived(IAsyncResult ar)
        {
            lock (ConnectionReceivedLock)
            {
                Socket socket = UnderlyingServer.EndAcceptSocket(ar);
                StringSocket ss = new StringSocket(socket, UTF8Encoding.Default);

                // Begin receiving commands from the client. The first command should 
                // always be PLAY @, where @ is the name of the player. If it is not, 
                // then handle the command appropriately.
                ss.BeginReceive(ServerCommandReceived, ss);

                // Wait for more clients to connect.
                UnderlyingServer.BeginAcceptSocket(ConnectionReceived, null);
            }
        }

        /// <summary>
        /// The callback called by the StringSocket after a new player has been initialized. 
        /// The expected string is the command "PLAY @" where @ is the name of the Player. 
        /// If a player is waiting to play, this method takes both the waiting player and the 
        /// new player and creates a new Game Obect, then starts the Game on a new thread. If 
        /// there are no waiting players, then the waiting player is stored until a new client
        /// connects.
        /// </summary>
        private void ServerCommandReceived(String Command, Exception e, Object PlayerStringSocket)
        {
            lock (CommandReceived)
            {
                StringSocket PlayersSocket = (StringSocket)PlayerStringSocket;

                // If Command is null then the client disconnected. Close the socket
                // and return from this method call.
                if (object.ReferenceEquals(null, Command))
                {
                    PlayersSocket.Close();
                    return;
                }

                // If the message received is PLAY @ with @ being the name of the player, then do 
                // the following:
                else if ((Command = Command.Trim()).StartsWith("PLAY "))
                {
                    // Get the name of the player from the incoming string. If a carriage return
                    // exists from telnet etc.. then it will be removed because we have trimmed
                    // the command by this point.
                    String PlayerName = Command.Substring(5);

                    // Create a new PlayerData object with the PlayerName and the PlayersSocket.
                    PlayerData NewPlayer = new PlayerData(PlayerName, PlayersSocket);

                    // If there is nobody else waiting to play, then store the player and
                    // wait for another to join.
                    if (WaitingPlayer == null || !WaitingPlayer.Socket.Connected)
                    {
                        WaitingPlayer = NewPlayer;
                        Console.WriteLine(NewPlayer.Name + " Connected");
                    }

                    // If there is somebody waiting for a game, then get both players and 
                    // build a game object with them. 
                    else
                    {
                        Console.WriteLine(NewPlayer.Name + " Connected");

                        // Get the waiting player's data
                        PlayerData FirstPlayer = WaitingPlayer;

                        // There are no more players waiting. Set WaitingPlayer = null for
                        // future checks.
                        WaitingPlayer = null;

                        // Build a new Game object with both players.
                        Game NewGame;
                        if (OptionalString == null)
                            NewGame = new Game(FirstPlayer, NewPlayer, GameLength, DictionaryWords, null);
                        else
                            NewGame = new Game(FirstPlayer, NewPlayer, GameLength, DictionaryWords, OptionalString);

                        // Start the game between the two clients on a new Thread. This will
                        // reduce the workload that the server has to worry about.
                        ThreadStart Workload = new ThreadStart(NewGame.RunGame);
                        Thread RunGame = new Thread(Workload);
                        RunGame.Start();
                    }

                }

                // If the client didn't send the PLAY command, then print an IGNORING message
                // and continue waiting for the correct command.
                else
                {
                    StringSocket ss;
                    ss = (StringSocket)PlayerStringSocket;

                    ss.BeginSend("IGNORING " + Command + "\n", (ex, o) => { }, null);
                    ss.BeginReceive(ServerCommandReceived, ss);
                }
            }
        }

        #region Private Nested Game Class
        /// <summary>
        /// The Game class handles the bulk of the operations related to running a Boggle game. 
        /// It has methods to start games, manage game times, receive words from clients, and end 
        /// games. 
        /// </summary>
        private class Game
        {
            // Global Variables used for the Game:
            private PlayerData Player1;     // A PlayerData object to holds all data for Player1.
            private PlayerData Player2;     // A PlayerData object to holds all data for Player2.
            private int GameTime;           // The Time left in the game
            private bool GameFinished;      // Indicates if time has run out. 
            private BoggleBoard GameBoard;  // The boggle board to be played on. 
            private HashSet<string> DictionaryWords;
            private readonly object WordPlayedLock;

            /// <summary>
            /// Initializes a Game. Creates a BoggleBoard with the OptionalString as 
            /// the letters used in the board. It also initializes GameTime to the 
            /// given time, Player1 and Player2 to their respective global pointers,
            /// and GameFinished to false. No game operation happens here, only the 
            /// initialization of variables. 
            /// </summary>
            /// <param name="Player1">The data for the first player.</param>
            /// <param name="Player2">The data for the second player.</param>
            /// <param name="GameTime">The length of the game (in seconds).</param>
            /// <param name="OptionalString">The string given via the command line that 
            /// indicates which letters should be used for the game board.</param>
            public Game(PlayerData Player1, PlayerData Player2, int GameTime, HashSet<string> DictionaryWords, String OptionalString)
            {
                // Create a BoggleBoard with or without the OptionalString according to what was given.
                if(OptionalString == null)
                    this.GameBoard = new BoggleBoard();
                else
                    this.GameBoard = new BoggleBoard(OptionalString);

                // Intialize all other instance variables.
                this.Player1 = Player1;
                this.Player2 = Player2;
                this.GameTime = GameTime;
                this.DictionaryWords = DictionaryWords;
                this.GameFinished = false;
                this.WordPlayedLock = new object();
            }

            /// <summary>
            /// Does the bulk of the game operation. Sends the START command,
            /// starts and monitors the counting of GameTime., and receives
            /// and processes words from the Players. When GameFinished
            /// is set to true it calls the EndGame method
            /// </summary>
            public void RunGame()
            {
                // Send the START command to both Players
                Player1.Socket.BeginSend("START" + " " + GameBoard.ToString() + " " + GameTime + " " + Player2.Name + "\n", (e, o) => { }, null);
                Player2.Socket.BeginSend("START" + " " + GameBoard.ToString() + " " + GameTime + " " + Player1.Name + "\n", (e, o) => { }, null);

                // Start counting the time on a different thread.
                ThreadStart TimeCounter = new ThreadStart(CalculateTime);
                Thread CalcTime = new Thread(TimeCounter);
                CalcTime.Start();

                // Start receiving words from both Players. 
                Player1.Socket.BeginReceive(CommandRecieved, Player1);
                Player2.Socket.BeginReceive(CommandRecieved, Player2);

                // Wait for the Thread calculating the time to finish.
                CalcTime.Join();

                // When the game is finished call the EndGame method to return scores 
                // and cleanup resources.
                EndGame();
            }

            /// <summary>
            /// Calculates and sends the current game time to clients. The 
            /// time is sent out every second. When the time runs out the 
            /// GameFinished variable is set to true, then this method 
            /// returns. 
            /// </summary>
            private void CalculateTime()
            {
                // While there is still time left in the game
                for (; GameTime > 0; GameTime--)
                {
                    // Send the TIME command with the current time to the clients.
                    // Player1.Socket.BeginSend("TIME " + GameTime + "\n", TimeCallback, Player1);
                    // Player2.Socket.BeginSend("TIME " + GameTime + "\n", TimeCallback, Player2);

                    // Sleep for one second
                    Thread.Sleep(1000);
                }

                // When time is out, set GameFinished variable to true. 
                GameFinished = true;
            }

            private void TimeCallback(Exception e, object Payload)
            {

            }

            /// <summary>
            /// Called by RunGame when the this Game has finished.This will calculate and return 
            /// the final score of the game. It then builds and sends out the game summary. Finally, 
            /// it cleans up any resources used for the current Game.
            /// </summary>
            private void EndGame()
            {
                // Return the final scores.
                SendScore();

                // Build and send out the game summary.
                SendGameSummary();

                Player1.Socket.Close();
                Player2.Socket.Close();
            }

            /// <summary>
            /// Callback for the StringSockets. It expects the command WORD followed 
            /// by the word submitted by the client. If Command is null it checks the 
            /// exception. If Command does not start with 'WORD' then is replies
            /// with IGNORING followed by Command. 
            /// </summary>
            /// <param name="Command">The string sent from the client</param>
            /// <param name="Problem">The exception that caused the send to fail</param>
            /// <param name="Payload"></param>
            private void CommandRecieved(String Command, Exception Problem, Object Payload)
            {
                lock (WordPlayedLock)
                {
                    PlayerData Player = (PlayerData)Payload;

                    // If the game is finished, ignore any other incoming Commands. 
                    if (GameFinished == true)
                        return;

                    // If Command is null then the client disconnected. Close the opponents 
                    // client and print a message indicating the termination of the game.
                    if (object.ReferenceEquals(null, Command))
                    {
                        Console.WriteLine(Player.Name + " Disconnected");
                        if (Player == Player1)
                        {
                            Player2.Socket.BeginSend("TERMINATED" + "\n", (e, o) => { }, Player);
                            Player2.Socket.Close();
                            return;
                        }
                        else
                        {
                            Player1.Socket.BeginSend("TERMINATED" + "\n", (e, o) => { }, Player);
                            Player1.Socket.Close();
                            return;
                        }
                    }

                    // If the user sent the WORD command, then do the following:
                    else if ((Command = Command.ToUpper().Trim()).StartsWith("WORD "))
                    {
                        // Get the word that was played.
                        String WordPlayed = Command.Substring(5);

                        // If the user played a word containing a Q and the word that was
                        // played is illegal, then append a U and check if that is legal.
                        if (WordPlayed.Contains('Q') && !IsLegal(WordPlayed))
                            WordPlayed = WordPlayed.Replace("Q", "QU");


                        // Add the word to the user's list of words and calculate
                        // the score. Return the results to the user if any changes
                        // occured to the score.
                        CalculateScore(WordPlayed, Player);
                    }

                    // If the Command was anything else, reply with IGNORING and the Command.
                    else
                    {
                        Player.Socket.BeginSend("IGNORING " + Command + "\n", (ex, o) => { }, null);
                    }

                    // Begin receiving more commands from the player.
                    Player.Socket.BeginReceive(CommandRecieved, Player);
                }
            }

            /// <summary>
            /// Calculates the scores of the current scores for the two users involved
            /// in the current Boggle game.
            /// </summary>
            /// <param name="WordPlayed">The word played by the user.</param>
            /// <param name="Player">The player that played the word.</param>
            private void CalculateScore(string WordPlayed, PlayerData Player)
            {

                // Each word with fewer than three characters is removed (whether or not it is legal).For 
                // any word that appears more than once, ignore the additional appearance of the word.
                if (WordPlayed.Length > 2 && IsLegal(WordPlayed))
                {
                    if (!Player.WordsPlayed.Contains(WordPlayed))
                    {
                        // Calculate the weight of the word that was played by the
                        // user.
                        int WordWeight = 0;
                        switch (WordPlayed.Length)
                        {
                            case 3:
                                WordWeight = 1;
                                break;
                            case 4:
                                WordWeight = 1;
                                break;
                            case 5:
                                WordWeight = 2;
                                break;
                            case 6:
                                WordWeight = 3;
                                break;
                            case 7:
                                WordWeight = 5;
                                break;
                            default:
                                WordWeight = 11;
                                break;
                        }

                        if (Player == Player1)
                        {
                            // If player played a word that his opponent has already played,
                            // then decrement the opponents score based on the weight and add
                            // the word as a duplicate.
                            if (Player2.WordsPlayed.Contains(WordPlayed))
                            {
                                Player2.LegalWords.Remove(WordPlayed);
                                Player2.PlayerScore -= WordWeight;

                                SendScore();

                                Player1.DuplicateWords.Add(WordPlayed);
                                Player2.DuplicateWords.Add(WordPlayed);
                            }

                            // If the player played a word that his opponent has not already played
                            // then increment his score based on the weight and add he word to the
                            // list of legal words.
                            else
                            {
                                Player1.LegalWords.Add(WordPlayed);
                                Player1.PlayerScore += WordWeight;

                                SendScore();
                            }
                        }
                        else
                        {
                            // If player played a word that his opponent has already played,
                            // then decrement the opponents score based on the weight and add
                            // the word as a duplicate.
                            if (Player1.WordsPlayed.Contains(WordPlayed))
                            {
                                Player1.LegalWords.Remove(WordPlayed);
                                Player1.PlayerScore -= WordWeight;

                                SendScore();

                                Player1.DuplicateWords.Add(WordPlayed);
                                Player2.DuplicateWords.Add(WordPlayed);
                            }

                            // If the player played a word that his opponent has not already played
                            // then increment his score based on the weight and add he word to the
                            // list of legal words.
                            else
                            {
                                Player2.LegalWords.Add(WordPlayed);
                                Player2.PlayerScore += WordWeight;

                                SendScore();
                            }
                        }
                    }

                    Player.WordsPlayed.Add(WordPlayed);
                }

                // Otherwise if the word contains more than 2 characters and is illegal,
                // then do the following:
                else if (WordPlayed.Length > 2 && !IsLegal(WordPlayed))
                {
                    if (!Player.WordsPlayed.Contains(WordPlayed))
                    {
                        Player.IllegalWords.Add(WordPlayed);
                        Player.PlayerScore--;

                        SendScore();
                    }

                    Player.WordsPlayed.Add(WordPlayed);
                }
            }

            /// <summary>
            /// Determines whether a word that that was played is legal. A word
            /// is legal if it can be formed and if it is contained with the
            /// dictionary.
            /// </summary>
            private bool IsLegal(string WordPlayed)
            {
                if (DictionaryWords.Contains(WordPlayed) && GameBoard.CanBeFormed(WordPlayed))
                    return true;
                else
                    return false;
            }

            /// <summary>
            /// Used to send the current score to the two players in this Boggle game.
            /// </summary>
            private void SendScore()
            {
                Player1.Socket.BeginSend("SCORE " + Player1.PlayerScore + " " + Player2.PlayerScore + "\n", (e, o) => { }, null);
                Player2.Socket.BeginSend("SCORE " + Player2.PlayerScore + " " + Player1.PlayerScore + "\n", (e, o) => { }, null);
            }

            /// <summary>
            /// Sends out the game summary to the two players of this Boggle game.
            /// </summary>
            private void SendGameSummary()
            {
                // Get the count of legal words that Player1 played and the corresponding
                // whitespace seperated legal words.
                string Player1LegalWords = string.Join(" ", Player1.LegalWords);
                string Player1LegalCount = Player1.LegalWords.Count.ToString();

                // Get the count of legal words that Player2 played and the corresponding
                // whitespace seperated legal words.
                string Player2LegalWords = string.Join(" ", Player2.LegalWords);
                string Player2LegalCount = Player2.LegalWords.Count.ToString();

                // Get the count of illegal words that Player1 played and the corresponding
                // whitespace seperated illegal words.
                string Player1IllegalWords = string.Join(" ", Player1.IllegalWords);
                string Player1IllegalCount = Player1.IllegalWords.Count.ToString();

                // Get the count of illegal words that Player2 played and the corresponding
                // whitespace seperated illegal words.
                string Player2IllegalWords = string.Join(" ", Player2.IllegalWords);
                string Player2IllegalCount = Player2.IllegalWords.Count.ToString();

                // Note that Player1.DuplicateWords == Player2.DuplicateWords, assuming our
                // CalculateScore function words properly.
                string DuplicateWords = string.Join(" ", Player1.DuplicateWords);
                string DuplicateWordCount = Player1.DuplicateWords.Count.ToString();

                string[] FormatArgs = { Player1LegalCount, Player1LegalWords, Player2LegalCount, Player2LegalWords, DuplicateWordCount,                                                    DuplicateWords, Player1IllegalCount, Player1IllegalWords, Player2IllegalCount, Player2IllegalWords };
                
                // Generate the game summary for each player.
                string Player1Summary = string.Format("STOP {0} {1} {2} {3} {4} {5} {6} {7} {8} {9}\n", FormatArgs);
                string Player2Summary = string.Format("STOP {2} {3} {0} {1} {4} {5} {8} {9} {6} {7}\n", FormatArgs);

                // Send the game summary results to each player.
                Player1.Socket.BeginSend(Player1Summary, (e, o) => { }, null);
                Player2.Socket.BeginSend(Player2Summary, (e, o) => { }, null);
            }
        }
        #endregion

        # region Private Nested PlayerData Class
        /// <summary>
        /// Used to hold all data related to a single Boggle player.
        /// </summary>
        private class PlayerData
        {
            string PlayerName;                // The name of the player
            StringSocket PlayerSocket;        // The StringSocket associated with the PlayerName.                
            int Score;                        // The current score of the player.
            HashSet<string> Played_Words;      // All words the player has found in the current game. 
            HashSet<string> Legal_Words;
            HashSet<string> Illegal_Words;
            HashSet<string> Duplicate_Words;


            // Public Properties used to get the member variables of a given PlayerData instance:
            public string Name { get { return this.PlayerName; } }
            public StringSocket Socket { get { return this.PlayerSocket; } }
            public int PlayerScore { get { return this.Score; } set { this.Score = value; } }
            public HashSet<string> WordsPlayed { get { return this.Played_Words; } }
            public HashSet<string> LegalWords { get { return this.Legal_Words; } }
            public HashSet<string> IllegalWords { get { return this.Illegal_Words; } }
            public HashSet<string> DuplicateWords { get { return this.Duplicate_Words; } }

            /// <summary>
            /// Constructor used to initialize a new PlayerData instance. A PlayerData instance contains
            /// the name of the player, the StringSocket used for communication, the players score, and
            /// the words that the user has played.
            /// </summary>
            public PlayerData(string PlayerName, StringSocket PlayerSocket)
            {
                this.PlayerName = PlayerName;
                this.PlayerSocket = PlayerSocket;
                this.Score = 0;
                this.Played_Words = new HashSet<string>();
                this.Legal_Words = new HashSet<string>();
                this.Illegal_Words = new HashSet<string>();
                this.Duplicate_Words = new HashSet<string>();
            }
        }
        #endregion
    }
}
