using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoggleServer
{
    class BoggleServer
    {
        // Main method called upon when the user runs this program. Here we 
        // will initialize a new BoggleServer with the command line args
        // and run the server.
        static void Main(string[] args)
        {
            // If the user supplied less than two args or more than three,
            // then print an error message and terminate.
            if (args.Length < 2 || args.Length > 3)
            {
                Console.WriteLine("Error: Invalid arguments.");
                Console.WriteLine("usage: BoggleServer time dictionary_path optional_string");
                Environment.Exit(0);
            }

            // Otherwise the user must have provided the correct number of
            // arguments. Continue forward..

            // Parse the arguments and ensure that they are valid. If they are not, then
            // print a descriptive error message and terminate the program.

            // After paramter validation, start the Boggle game server.
            //BoggleServer GameServer = new BoggleServer(time, dictionary_path, optional_string);
            //GameServer.StartServer();
        }

        // Member variables used to organize a BoggleServer:
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

        }
    }
}
