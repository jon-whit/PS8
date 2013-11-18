using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Net.Sockets;


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
    }
}
