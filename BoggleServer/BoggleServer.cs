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


namespace BS
{
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
                //
                this.UnderlyingServer = new TcpListener(IPAddress.Any, 2000);
                this.GameLength = GameLength;
                this.DictionaryWords = new HashSet<string>(File.ReadAllLines(DictionaryPath));

                //Begin accepting clients
                UnderlyingServer.BeginAcceptSocket(ConnectionReceived, null);
                
            }
            catch (Exception)
            {

            }
        }


        /// <summary>
        /// The callback fired by the Underlying Server when a new connection is received. 
        /// </summary>
        private void ConnectionReceived(IAsyncResult ar)
        {
            Socket socket = UnderlyingServer.EndAcceptSocket(ar);
            StringSocket ss = new StringSocket(socket, UTF8Encoding.Default);
            
            ss.BeginReceive(PlayReceived, ss);

            UnderlyingServer.BeginAcceptSocket(ConnectionReceived, null);
        }

        private void PlayReceived(String PlayString, Exception e, Object PlayerStringSocket)
        {
            //If the message received is PLAY @ with @ being the name of the player then do the following
            if(PlayString.StartsWith("PLAY"))
            {
                //Get the name of the player from the incoming string. 
                String PlayerName = PlayString.Substring(5);
                //Create a new PlayerData object with O as the StringSocket, and the Name of the Player as the Name.
                PlayerData NewPlayer = new PlayerData(PlayerName, (StringSocket)PlayerStringSocket);

                //If there is nobody else queued up to play, then add the player to a queue to wait
                //for another connection.
                if(WaitingPlayers.Count == 0)
                {
                    WaitingPlayers.Enqueue(NewPlayer);
                }

                //If there is somebody waiting for a game then get both players and 
                //Build a game object with them. 
                else
                {
                    //Get the waiting player's data
                    PlayerData FirstPlayer = WaitingPlayers.Dequeue();
                    
                    //Build a new Game object with both players.
                    Game NewGame = new Game(FirstPlayer, SecondPlayer, 

                    //Start the game
                    Thread GameThread = new Thread(NewGame.StartGame());
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
        /// 
        /// </summary>
        private class Game
        {
            //Global Variables used for the Game
            int GameTime;
            bool GameFinished;
            PlayerData Player1;
            PlayerData Player2;
            BoggleBoard GameBoard;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="Player1"></param>
            /// <param name="Player2"></param>
            /// <param name="GameTime"></param>
            public Game(PlayerData Player1, PlayerData Player2, int GameTime)
            {
                //Create a BoggleBoard

                //Intialize all other instance variables.
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="Player1"></param>
            /// <param name="Player2"></param>
            /// <param name="GameTime"></param>
            /// <param name="OptionalString"></param>
            public Game(PlayerData Player1, PlayerData Player2, int GameTime, String OptionalString)
            {
                //Create a BoggleBoard with the OptionalString as the Board's String.

                //Initialize all other instance variables.
            }

            /// <summary>
            /// 
            /// </summary>
            public void StartGame()
            {
                //Send the START command to both Players

                //Start counting the time on a different thread

                //Start receiving words from both Players. 

                //When the game is finished call the EndGame method to return scores and cleanup resources.

            }

            /// <summary>
            /// 
            /// </summary>
            private void CalculateTime()
            {
                //While there is still time left in the game

                //Sleep for one second

                //Send the TIME command with the current time.

                //When time is out, set GameFinished variable to true. 
                
            }

            /// <summary>
            /// 
            /// </summary>
            private void EndGame()
            {
                //Return the final scores by using the STOP command

                //Clean up all resources

            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="Command"></param>
            /// <param name="Problem"></param>
            /// <param name="Payload"></param>
            private void CommandRecieved(String Command, Exception Problem, Object Payload)
            {
                //Check for which command was sent.

                //If the command was WORD process the word.
                    //Confirm whether or not we need to validate the word.
                    //Add the word to the user's list of words
                    //Calculate the score and send the score out to the user. 

                //If it was anything else, reply with IGNORING and the Command.
            }

        }

        /// <summary>
        /// 
        /// </summary>
        private class PlayerData
        {

        }
    }
}
