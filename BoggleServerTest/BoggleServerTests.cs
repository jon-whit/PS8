using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Sockets;
using CustomNetworking;
using BS;
using BB;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace BoggleServerTest
{
    [TestClass]
    public class BoggleServerTests
    {
        // Create a HashSet containing all of the unique words in the
        // supplied dictionary.
        public static string DictionaryPath = "..//..//..//Solution Items//dictionary.txt";
        private HashSet<string> DictionaryWords = new HashSet<string>(File.ReadAllLines(DictionaryPath));

        // Data that is shared across threads
        private ManualResetEvent mre1;
        private ManualResetEvent mre2;
        private ManualResetEvent mre3;
        private ManualResetEvent mre4;
        private bool ExceptionThrown;
        private String s1;
        private object p1;
        private String s2;
        private object p2;
        private String s3;
        private object p3;
        private String s4;
        private object p4;

        // Timeout used in test case
        private static int timeout = 2000;
        private static int GameTime = 10;
        
        [TestMethod]
        public void TestIllegalCMDArgs()
        {
            /*
             When running the program the following values should be used:

             * The number of seconds that each Boggle game should last. This should be a positive integer.
             * The pathname of a file that contains all the legal words. The file should contain one word 
               per line.
             * An optional string consisting of exactly 16 letters. If provided, this will be used to initialize
               each Boggle board.
             */

            // Assert errors are being returned correctly when the user passes illegal parameters
            // as command line arguments.

            StringWriter sw1 = new StringWriter();

            Console.SetOut(sw1);

            // Illegally pass more than three parameters.
            BoggleServer.Main(new string[] { "arg1", "arg2", "arg3", "arg4" });

            string expected = string.Format("Error: Invalid arguments.\r\nusage: BoggleServer time dictionary_path optional_string\r\n");
            string actual = sw1.ToString();

            Assert.AreEqual(expected, actual);
            sw1.Close();

            // Create a new StringWriter for the next test.
            sw1 = new StringWriter();
            Console.SetOut(sw1);

            // Illegally pass less than two parameters.
            BoggleServer.Main(new string[] { });

            actual = sw1.ToString();

            Assert.AreEqual(expected, actual);
            sw1.Close();
            
            // The first parameter must be a number. If it is not, then the user has supplied illegal
            // parameters.
            
            // Create a new StringWriter for the next test.
            sw1 = new StringWriter();
            Console.SetOut(sw1);

            // Illegally pass less than two parameters.
            BoggleServer.Main(new string[] { BoggleServerTests.DictionaryPath, "200" });

            actual = sw1.ToString();

            Assert.AreEqual(expected, actual);
            sw1.Close();

            // The second paramter must be a valid filepath. If it is not, then the user has supplied 
            // illegal paramters.
            
            // Create a new StringWriter for the next test.
            sw1 = new StringWriter();
            Console.SetOut(sw1);

            // Illegally pass less than two parameters.
            BoggleServer.Main(new string[] { "200", "illegal.txt" });

            actual = sw1.ToString();

            Assert.AreEqual("Error: You must provide a valid file path!\r\n", actual);
            sw1.Close();

            // Test a non-positive time input.

            // Create a new StringWriter for the next test.
            sw1 = new StringWriter();
            Console.SetOut(sw1);

            // Illegally pass less than two parameters.
            BoggleServer.Main(new string[] { "-1", "illegal.txt" });

            actual = sw1.ToString();

            Assert.AreEqual(expected, actual);
            sw1.Close();

            // Create a new StringWriter for the next test.
            sw1 = new StringWriter();
            Console.SetOut(sw1);

            // Illegally pass less than two parameters.
            BoggleServer.Main(new string[] { "0", "illegal.txt" });

            actual = sw1.ToString();

            Assert.AreEqual(expected, actual);
            sw1.Close();

            // The optional third parameter must be 16 characters. If it is not, then the user has
            // supplied illegal parameters.

            // Create a new StringWriter for the next test.
            sw1 = new StringWriter();
            Console.SetOut(sw1);

            // Illegally pass less than two parameters.
            BoggleServer.Main(new string[] { "200", BoggleServerTests.DictionaryPath, "arg" });

            actual = sw1.ToString();

            Assert.AreEqual(expected, actual);
            sw1.Close();

            // The optional third parameter must be 16 characters and all letters. If it is not, then 
            // the user has supplied illegal parameters.

            // Create a new StringWriter for the next test.
            sw1 = new StringWriter();
            Console.SetOut(sw1);

            // Illegally pass less than two parameters.
            BoggleServer.Main(new string[] { "200", BoggleServerTests.DictionaryPath, "jimiergsatnesap1" });

            actual = sw1.ToString();

            Assert.AreEqual(expected, actual);
            sw1.Close();
        }


        [TestCleanup]
        public void CleanupConsoleOutput()
        {
            // Reset the standard console output stream.
            StreamWriter StandardOutput = new StreamWriter(Console.OpenStandardOutput());
            StandardOutput.AutoFlush = true;
            Console.SetOut(StandardOutput);
        }

        [TestMethod]
        public void TestConnection()
        {
            new BoggleServerTests().PairClients();
        }

        [TestMethod]
        public void TestPairClients()
        {
            // Once the server has received connections from two clients that are ready 
            // to play, it pairs them in a game.

            // The server begins the game by sending a command to each client. The command 
            // is "START $ # @"
            // Assert clients receive command START $ # @
        }

        [TestMethod]
        public void TestClientDisconnect()
        {
            // If at any point during a game a client disconnects or becomes inaccessible, the 
            // game ends. The server should send the command "TERMINATED" to the surviving client 
            // and then close the socket.
        }

        [TestMethod]
        public void TestIllegalClientProtocol()
        {
            // If a client deviates from the protocol at any point, the server should send a command 
            // to the offending client consisting of "IGNORING " followed by the offending command.

            /* There are two cases:
             *  1. The user doesn't begin with the PLAY command.
             *  2. The user doesn't provide a valid command after
             *     the game has started.
             */
            new BoggleServerTests().IllegalClientProtocol();
            

        }

        [TestMethod]
        public void TestWordCommand()
        {
            
        }

        /// <summary>
        /// Test to determine if two clients send the proper START commands initially. 
        /// If they don't then the server will reply with an IGNORING command.
        /// </summary>
        private void IllegalClientProtocol()
        {
            // This will coordinate communication between the threads of the test cases
            mre1 = new ManualResetEvent(false);
            mre2 = new ManualResetEvent(false);

            // Create a new Boggle Server which should begin listening for connection requests
            BoggleServer TestServer = new BoggleServer(GameTime, DictionaryPath, null);

            // Create a two clients to connect with the Boggle Server.
            TcpClient TestClient1 = new TcpClient("localhost", 2000);
            TcpClient TestClient2 = new TcpClient("localhost", 2000);

            // Create a client socket and then a client string socket.
            Socket ClientSocket1 = TestClient1.Client;
            Socket ClientSocket2 = TestClient2.Client;
            StringSocket ClientSS1 = new StringSocket(ClientSocket1, new UTF8Encoding());
            StringSocket ClientSS2 = new StringSocket(ClientSocket2, new UTF8Encoding());

            // Case 1: The user doesn't begin with the PLAY command.
            ClientSS1.BeginSend("Illegal Client1\n", PlayCallback1, 1);
            ClientSS2.BeginSend("Illegal Client2\n", PlayCallback2, 2);

            // When the connection between the server and clients is established,
            // the server expects the command PLAY @. But in this case we didn't
            // send that command, so we should expect an IGNORING reply from the
            // server.
            ClientSS1.BeginReceive(CompletedReceive1, 1);
            ClientSS2.BeginReceive(CompletedReceive2, 2);

            // Assert that the server sent back the correct commands after the two clients
            // connected.
            Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
            Assert.AreEqual("IGNORING Illegal Client1", s1);
            Assert.AreEqual(1, p1);
            Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");
            Assert.AreEqual("IGNORING Illegal Client2", s2);
            Assert.AreEqual(2, p2);
        }

        /// <summary>
        /// Test used to determine if two Boggle clients are being paired together properly. 
        /// Two clients are paired together properly if they receive the command START $ # @, 
        /// where $ are the characters that make up the board, # is the game time, and @ is 
        /// the opponents name.
        /// </summary>
        private void PairClients()
        {
            // This will coordinate communication between the threads of the test cases
            mre1 = new ManualResetEvent(false);
            mre2 = new ManualResetEvent(false);
                
            // Create a new Boggle Server which should begin listening for connection requests
            BoggleServer TestServer = new BoggleServer(GameTime, DictionaryPath, null);

            // Create a two clients to connect with the Boggle Server.
            TcpClient TestClient1 = new TcpClient("localhost", 2000);
            TcpClient TestClient2 = new TcpClient("localhost", 2000);

            // Create a client socket and then a client string socket.
            Socket ClientSocket1 = TestClient1.Client;
            Socket ClientSocket2 = TestClient2.Client;
            StringSocket ClientSS1 = new StringSocket(ClientSocket1, new UTF8Encoding());
            StringSocket ClientSS2 = new StringSocket(ClientSocket2, new UTF8Encoding());

            // Now our client needs to send the command PLAY @ and the server will receive it. 
            ClientSS1.BeginSend("PLAY Client1\n", PlayCallback1, 1);
            ClientSS2.BeginSend("PLAY Client2\n", PlayCallback2, 2);

            // When a connection has been established, the client sends a command to 
            // the server. The command is "PLAY @", where @ is the name of the player.
            // Assert that the server receives the command PLAY @
            ClientSS1.BeginReceive(CompletedReceive1, 1);
            ClientSS2.BeginReceive(CompletedReceive2, 2);

            // Assert that the server sent back the correct commands after the two clients
            // connected.
            string ExpectedExpression = @"^(START) [a-zA-Z]{16} \d+ [a-zA-Z1-9]+";
            Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
            Assert.IsTrue(Regex.IsMatch(s1, ExpectedExpression));
            Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");
            Assert.IsTrue(Regex.IsMatch(s2, ExpectedExpression));
        }

        // This is the callback for the first receive request.  We can't make assertions anywhere
        // but the main thread, so we write the values to member variables so they can be tested
        // on the main thread.
        private void CompletedReceive1(String s, Exception o, object payload)
        {
            s1 = s;
            p1 = payload;
            mre1.Set();
        }

        // This is the callback for the second receive request.
        private void CompletedReceive2(String s, Exception o, object payload)
        {
            s2 = s;
            p2 = payload;
            mre2.Set();
        }

        private void PlayCallback1(Exception error, Object Payload)
        {
            Assert.AreEqual(null, error);
            Assert.AreEqual(1, Payload);
        }

        private void PlayCallback2(Exception error, Object Payload)
        {
            Assert.AreEqual(null, error);
            Assert.AreEqual(2, Payload);
        }

        /// <summary>
        /// Finds all of the legal words of a given BoggleBoard based on the dictionary
        /// that was supplied.
        /// </summary>
        /// <param name="GameboardString">The randomly generated string representing the 
        /// current Gameboard.</param>
        private HashSet<string> FindLegalWords(string GameboardString)
        {
            
            BoggleBoard GameBoard = new BoggleBoard(GameboardString);
            HashSet<string> LegalWords = new HashSet<string>();

            foreach (string s in DictionaryWords)
            {
                if (GameBoard.CanBeFormed(s))
                    LegalWords.Add(s);
            }

            return LegalWords;
        }
    }
}