using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BS;
using System.Net.Sockets;
using CustomNetworking;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;

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
        public void TestConnection()
        {
            new TestEstablishConnection().run();
        }

        [TestClass]
        public class TestEstablishConnection
        {
            // Data that is shared across threads
            private ManualResetEvent mre1;
            private ManualResetEvent mre2;
            private ManualResetEvent mre3;
            private ManualResetEvent mre4;
            private String s1;
            private object p1;
            private String s2;
            private object p2;
            private String s3;
            private object p3;
            private String s4;
            private object p4;

            // Timeout used in test case
            private static int timeout = 8000;

            public void run()
            {
                try
                {
                    // This will coordinate communication between the threads of the test cases
                    mre1 = new ManualResetEvent(false);
                    mre2 = new ManualResetEvent(false);
                    mre3 = new ManualResetEvent(false);
                    mre4 = new ManualResetEvent(false);

                    // Create a new Boggle Server which should begin listening for connection requests
                    BoggleServer TestServer = new BoggleServer(200, "..//..//..//Solution Items//dictionary.txt", null);

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

                    string ExpectedExpression = @"^(START) \d+ [a-zA-Z]{16} [a-zA-Z1-9]+";
                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.IsTrue(Regex.IsMatch(s1, ExpectedExpression));
                    Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");
                    Assert.IsTrue(Regex.IsMatch(s2, ExpectedExpression));   
                }
                catch (Exception e)
                {
                    throw e;
                }
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
