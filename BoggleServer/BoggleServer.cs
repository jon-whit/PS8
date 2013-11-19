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
        /// <summary>
        /// Main method called upon when the user runs this program. Here we 
        /// will initialize a new BoggleServer with the command line args
        /// and run the server.
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
                Console.WriteLine("Error: Invalid arguments.");
                Console.WriteLine("usage: BoggleServer time dictionary_path optional_string");
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
            BoggleServer GameServer = new BoggleServer(GameLength, args[1], args[2]);
        }

        // Member variables used to organize a BoggleServer:
        private TcpListener UnderlyingServer;
        private int GameLength;
        private HashSet<string> DictionaryWords;
        private Queue<PlayerData> WaitingPlayers;

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

                // Begin accepting clients
                UnderlyingServer.BeginAcceptSocket(ConnectionReceived, null);
                
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Additional constructor used to initalize a BoggleServer on the specified port number. 
        /// This will initialize the GameLength and it will build a dictionary of all of the valid 
        /// words from the DictionaryPath. If an optional string was passed to this application, 
        /// then it will build a BoggleBoard from the supplied string. Otherwise it will build a 
        /// BoggleBoard randomly.
        /// </summary>
        /// <param name="PortNum">The port that the BoggleServer should run on.</param>
        /// <param name="GameLength">The length that the Boggle game should take.</param>
        /// <param name="DictionaryPath">The filepath to the dictionary that should be used to
        /// compare words against.</param>
        /// <param name="OptionalString">An optional string to construct a BoggleBoard with.</param>
        public BoggleServer(int PortNum, int GameLength, string DictionaryPath, string OptionalString)
        {
            try
            {
                this.UnderlyingServer = new TcpListener(IPAddress.Any, PortNum);
                this.GameLength = GameLength;
                this.DictionaryWords = new HashSet<string>(File.ReadAllLines(DictionaryPath));
            }
            catch (Exception)
            {

            }
        }


        /// <summary>
        /// The callback called by the StringSocket after a new player has been initialized. 
        /// The expected string is the command "PLAY @" where @ is the name of the Player. 
        /// If a player is waiting to play, this method takes both the waiting player and the 
        /// new player and creates a new Game Obect, then starts the Game on a new thread. If 
        /// there are no waiting players, the player is put on a Queue to wait for another
        /// join. 
        /// </summary>
        /// <param name="IncomingCommand"></param>
        /// <param name="e"></param>
        /// <param name="PlayerStringSocket"></param>
        private void PlayReceived(String IncomingCommand, Exception e, Object PlayerStringSocket)
        {
            // If the message received is PLAY @ with @ being the name of the player then do the following
            if(IncomingCommand.StartsWith("PLAY"))
            {
                // Get the name of the player from the incoming string. 
                String PlayerName = IncomingCommand.Substring(5);
                
                // Create a new PlayerData object with O as the StringSocket, and the Name of the Player as the Name.
                PlayerData NewPlayer = new PlayerData(PlayerName, (StringSocket)PlayerStringSocket);

                // If there is nobody else queued up to play, then add the player to a queue to wait
                // for another connection.
                if(WaitingPlayers.Count == 0)
                {
                    WaitingPlayers.Enqueue(NewPlayer);
                }

                // If there is somebody waiting for a game then get both players and 
                // Build a game object with them. 
                else
                {
                    // Get the waiting player's data
                    PlayerData FirstPlayer = WaitingPlayers.Dequeue();
                    
                    // Build a new Game object with both players.
                    Game NewGame = new Game(FirstPlayer, SecondPlayer, 

                    // Start the game
                    Thread GameThread = new Thread(NewGame.RunGame());
                }
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
            Socket socket = UnderlyingServer.EndAcceptSocket(ar);
            StringSocket ss = new StringSocket(socket, UTF8Encoding.Default);

            ss.BeginReceive(PlayReceived, ss);

            UnderlyingServer.BeginAcceptSocket(ConnectionReceived, null);
        }

        /// <summary>
        /// The Game class handles the bulk of the operations related to running a Boggle game. 
        /// It has methods to start games, manage game times, receive words from clients, and end 
        /// games. 
        /// </summary>
        private class Game
        {
            // Global Variables used for the Game
            int GameTime;           // time left in the game
            bool GameFinished;      // Indicates if time has run out. 
            PlayerData Player1;     // A PlayerData object to holds all data for Player1.
            PlayerData Player2;     // A PlayerData object to holds all data for Player2.
            BoggleBoard GameBoard;  // The boggle board to be played on. 

            /// <summary>
            /// Initializes a Game. Creates a BoggleBoard and initializes GameTime to the 
            /// given time, Player1 and Player2 to their respective global pointers, and 
            /// GameFinished to false. No game operation happens here, only the 
            /// initialization of variables. 
            /// </summary>
            /// <param name="Player1">The first player</param>
            /// <param name="Player2">The second player</param>
            /// <param name="GameTime">The length of the game (in seconds)</param>
            public Game(PlayerData Player1, PlayerData Player2, int GameTime)
            {
                // Create a BoggleBoard

                // Intialize all other instance variables.
            }

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
            public Game(PlayerData Player1, PlayerData Player2, int GameTime, String OptionalString)
            {
                // Create a BoggleBoard with the OptionalString as the Board's String.

                // Initialize all other instance variables.
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

                // Start counting the time on a different thread

                // Start receiving words from both Players. 

                // When the game is finished call the EndGame method to return scores and cleanup resources.

            }

            /// <summary>
            /// Calculates and sends the current game time to clients. The 
            /// time is sent out every second. When the time runs out the 
            /// GameFinished variable is set to true, then this method 
            /// returns. 
            /// </summary>
            private void CalculateTime()
            {
                //While there is still time left in the game

                // Sleep for one second

                // Send the TIME command with the current time.

                // When time is out, set GameFinished variable to true. 
                
            }

            /// <summary>
            /// Called by RunGame when the this Game has finished (GameFinished = true).
            /// This will calculate and return the final score of the game. It then builds 
            /// and sends out the game summary. Finally, it closes the game and StringSockets. 
            /// </summary>
            private void EndGame()
            {
                //Return the final scores by using the STOP command

                // Build and send out the game summary.

                // Clean up all resources

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
                // Check for which command was sent.

                // If the command was WORD process the word.
                    // Confirm whether or not we need to validate the word.
                    // Add the word to the user's list of words
                    // Calculate the score and send the score out to the user. 

                // If it was anything else, reply with IGNORING and the Command.
            }

        }

        /// <summary>
        /// Used to hold all data related to a single Boggle player.
        /// </summary>
        private class PlayerData
        {
            StringSocket PlayerSocket;  // The StringSocket associated with the PlayerName
            String PlayerName;          // The name of the player
            int Score;                  // The current score of the player
            HashSet<String> words;      // All words the player has found in the current game. 

        }
    }
}
