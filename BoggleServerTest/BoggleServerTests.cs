using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BS;

namespace BoggleServerTest
{
    [TestClass]
    public class BoggleServerTests
    {
        [TestMethod]
        public void TestCMDArgs()
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
            BoggleServer.Main(new string[] {"dictionary.txt", "200"});

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

            Assert.AreEqual(expected, actual);
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
            BoggleServer.Main(new string[] { "200", "..\\..\\..\\Solution Items\\dictionary.txt", "arg" });

            actual = sw1.ToString();

            Assert.AreEqual(expected, actual);
            sw1.Close();

            // The optional third parameter must be 16 characters and all letters. If it is not, then 
            // the user has supplied illegal parameters.

            // Create a new StringWriter for the next test.
            sw1 = new StringWriter();
            Console.SetOut(sw1);

            // Illegally pass less than two parameters.
            BoggleServer.Main(new string[] { "200", "..\\..\\..\\Solution Items\\dictionary.txt", "jimiergsatnesap1" });

            actual = sw1.ToString();

            Assert.AreEqual(expected, actual);
            sw1.Close();

            BoggleServer.Main(new string[] { "200", "..\\..\\..\\Solution Items\\dictionary.txt", "jimiergsatnesapa" });
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
        public void TestEstablishConnection()
        {
            // When a connection has been established, the client sends a command to 
            // the server. The command is "PLAY @", where @ is the name of the player.
            // Assert that the server receives the command PLAY @
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
        }

    }
}
