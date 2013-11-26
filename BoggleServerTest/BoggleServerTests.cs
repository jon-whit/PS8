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
        // Generate the BoggleServer
        private static string GameboardString = "QDDEATSDCIESKYTI";
        private static string DictionaryPath = "..//..//..//Solution Items//dictionary.txt";
        private static BoggleServer TestServer = new BoggleServer(20, DictionaryPath, GameboardString);

        // Create a HashSet containing all of the unique words in the
        // supplied dictionary.
        private static HashSet<string> DictionaryWords = new HashSet<string>(File.ReadAllLines(DictionaryPath));

        
        private static readonly object IllegalWordsLock = new object();
        private static readonly object LegalWordsLock = new object();
        private static readonly object XLetterWordsLock = new object();

        // Timeout used in test case
        private static int timeout = 2000;

        #region Main Method Test
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
        #endregion

        #region Test Connection Tests

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public void TestConnection()
        {
            new TestConnectionsPairing().PairClients();
        }

        [TestClass]
        public class TestConnectionsPairing
        {
            private String s1;
            private object p1;
            private String s2;
            private object p2;
            ManualResetEvent mre1;
            ManualResetEvent mre2; 

            /// <summary>
            /// Test used to determine if two Boggle clients are being paired together properly. 
            /// Two clients are paired together properly if they receive the command START $ # @, 
            /// where $ are the characters that make up the board, # is the game time, and @ is 
            /// the opponents name.
            /// </summary>
            public void PairClients()
            {
                // This will coordinate communication between the threads of the test cases
                mre1 = new ManualResetEvent(false);
                mre2 = new ManualResetEvent(false);

                // Create a two clients to connect with the Boggle Server.
                TcpClient TestClient1 = new TcpClient("localhost", 2000);
                TcpClient TestClient2 = new TcpClient("localhost", 2000);

                // Create a client socket and then a client string socket.
                Socket ClientSocket1 = TestClient1.Client;
                Socket ClientSocket2 = TestClient2.Client;
                StringSocket ClientSS1 = new StringSocket(ClientSocket1, new UTF8Encoding());
                StringSocket ClientSS2 = new StringSocket(ClientSocket2, new UTF8Encoding());

                try
                {
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
                finally
                {
                    ClientSS1.Close();
                    ClientSS2.Close();
                }

            }

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

        #endregion

        #region Client Disconnect Tests

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public void TestClientDisconnections()
        {
            new ClientDisconnectTests().TestClientDisconnect1();
            //new ClientDisconnectTests().TestClientDisconnect2();
        }

        /// <summary>
        /// 
        /// </summary>
        [TestClass]
        public class ClientDisconnectTests
        {
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

            /// <summary>
            /// 
            /// </summary>
            public void TestClientDisconnect1()
            {
                // If at any point during a game a client disconnects or becomes inaccessible, the 
                // game ends. The server should send the command "TERMINATED" to the surviving client 
                // and then close the socket.
            
                //First test: disconnection while in a game
                // This will coordinate communication between the threads of the test cases
                mre1 = new ManualResetEvent(false);
                mre2 = new ManualResetEvent(false);
                mre3 = new ManualResetEvent(false);
                mre4 = new ManualResetEvent(false);

                // Create a two clients to connect with the Boggle Server.
                TcpClient TestClient1 = new TcpClient("localhost", 2000);
                TcpClient TestClient2 = new TcpClient("localhost", 2000);

                // Create a client socket and then a client string socket.
                Socket ClientSocket1 = TestClient1.Client;
                Socket ClientSocket2 = TestClient2.Client;
                StringSocket ClientSS1 = new StringSocket(ClientSocket1, new UTF8Encoding());
                StringSocket ClientSS2 = new StringSocket(ClientSocket2, new UTF8Encoding());

                try
                {
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

                    // Now that the game has started, we need to assert that we are receiving the
                    // Command when one client disconnects. 
                    ClientSS1.BeginReceive(CompletedReceive3, 3);
                    ClientSS1.BeginReceive(CompletedReceive4, 4);
                    ClientSS2.Close();

                    Assert.AreEqual(true, mre3.WaitOne(timeout), "Timed out waiting 3");
                    Assert.AreEqual("TERMINATED", s3);
                    Assert.AreEqual(3, p3);

                    Assert.AreEqual(true, mre4.WaitOne(timeout), "Timed out waiting 4");
                    Assert.AreEqual(null, s4);
                    Assert.AreEqual(4, p4);
                }
                finally
                {
                    ClientSS1.Close();
                    ClientSS2.Close();
                }
            }

            public void TestClientDisconnect2()
            {
                    // Now test for when a client connects and then disconnects after giving 
                    // the PLAY command
                    mre1 = new ManualResetEvent(false);
                    mre2 = new ManualResetEvent(false);
                    mre3 = new ManualResetEvent(false);


                    //Create three clients. Connect and send PLAY with the first client. 
                    // Create a two clients to connect with the Boggle Server.
                    TcpClient TestClient1 = new TcpClient("localhost", 2000);
                    TcpClient TestClient2 = new TcpClient("localhost", 2000);
                    TcpClient TestClient3 = new TcpClient("localhost", 2000);

                    // Create a client socket and then a client string socket.
                    Socket ClientSocket1 = TestClient1.Client;
                    Socket ClientSocket2 = TestClient2.Client;
                    Socket ClientSocket3 = TestClient3.Client;
                    StringSocket ClientSS1 = new StringSocket(ClientSocket1, new UTF8Encoding());
                    StringSocket ClientSS2 = new StringSocket(ClientSocket2, new UTF8Encoding());
                    StringSocket ClientSS3 = new StringSocket(ClientSocket3, new UTF8Encoding());

                try
                {
                    ClientSS1.BeginSend("PLAY Client1\n", (e, o) => { }, "1a");
                    ClientSS1.Close();
                    ClientSS1.BeginReceive(CompletedReceive1, null);

                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1a");
                    Assert.AreEqual(null, s1);

                    //Now send PLAY with the other two clients and assert that they are paried together.
                    ClientSS2.BeginSend("PLAY Client2\n", (e, o) => { }, null);
                    ClientSS3.BeginSend("PLAY Client3\n", (e, o) => { }, null);


                    ClientSS2.BeginReceive(CompletedReceive2, "2a");
                    ClientSS3.BeginReceive(CompletedReceive3, "3a");

                    string ExpectedExpression = @"^(START) [a-zA-Z]{16} \d+ [a-zA-Z1-9]+";
                    Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2a");
                    Assert.IsTrue(Regex.IsMatch(s2, ExpectedExpression));
                    Assert.AreEqual(true, mre3.WaitOne(timeout), "Timed out waiting 3a");
                    Assert.IsTrue(Regex.IsMatch(s3, ExpectedExpression));
                }
                finally
                {

                }
            }

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

            // This is the callback for the second receive request.
            private void CompletedReceive3(String s, Exception o, object payload)
            {
                    s3 = s;
                    p3 = payload;
                    mre3.Set();
            }

            // This is the callback for the second receive request.
            private void CompletedReceive4(String s, Exception o, object payload)
            {
                    s4 = s;
                    p4 = payload;
                    mre4.Set();
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

        #endregion

        #region Client Protocol Tests
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
            new IllegalClientProtocolClass().IllegalClientProtocol();
        }

        /// <summary>
        /// 
        /// </summary>
        [TestClass]
        public class IllegalClientProtocolClass
        {
            private ManualResetEvent mre1;
            private ManualResetEvent mre2;

            private String s1;
            private object p1;
            private String s2;
            private object p2;

            /// <summary>
            /// Test to determine if two clients send the proper START commands initially. 
            /// If they don't then the server will reply with an IGNORING command.
            /// </summary>
            public void IllegalClientProtocol()
            {
                // This will coordinate communication between the threads of the test cases
                mre1 = new ManualResetEvent(false);
                mre2 = new ManualResetEvent(false);

                // Create a two clients to connect with the Boggle Server.
                TcpClient TestClient1 = new TcpClient("localhost", 2000);
                TcpClient TestClient2 = new TcpClient("localhost", 2000);

                // Create a client socket and then a client string socket.
                Socket ClientSocket1 = TestClient1.Client;
                Socket ClientSocket2 = TestClient2.Client;
                StringSocket ClientSS1 = new StringSocket(ClientSocket1, new UTF8Encoding());
                StringSocket ClientSS2 = new StringSocket(ClientSocket2, new UTF8Encoding());

                try
                {
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
                finally
                {
                    ClientSS1.Close();
                    ClientSS2.Close();
                }
            }

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

        #endregion

        #region Score Checker Tests

        [TestMethod]
        public void TestScoreBasic()
        {
            BasicScoreCalculations BasicTestObject = new BasicScoreCalculations();

            BasicTestObject.TestWordCommand1();
            BasicTestObject.TestWordCommand2();
        }

        [TestClass]
        private class BasicScoreCalculations
        {
            private ManualResetEvent mre1;
            private ManualResetEvent mre2;

            private String s1;
            private object p1;
            private String s2;
            private object p2;

            public void TestWordCommand1()
            {
                // Test a single legal word played by one of the two Boggle players and
                // assert that the score incremented appropriately.

                // This will coordinate communication between the threads of the test cases
                mre1 = new ManualResetEvent(false);
                mre2 = new ManualResetEvent(false);

                // Create a two clients to connect with the Boggle Server.
                TcpClient TestClient1 = new TcpClient("localhost", 2000);
                TcpClient TestClient2 = new TcpClient("localhost", 2000);

                // Create a client socket and then a client StringSocket.
                Socket ClientSocket1 = TestClient1.Client;
                Socket ClientSocket2 = TestClient2.Client;
                StringSocket ClientSS1 = new StringSocket(ClientSocket1, new UTF8Encoding());
                StringSocket ClientSS2 = new StringSocket(ClientSocket2, new UTF8Encoding());

                try
                {
                    // Connect two clients together to play Boggle.
                    ClientSS1.BeginSend("PLAY Client1\n", (e, o) => { }, null);
                    ClientSS2.BeginSend("PLAY Client2\n", (e, o) => { }, null);

                    ClientSS1.BeginReceive(CompletedReceive1, 1);
                    ClientSS2.BeginReceive(CompletedReceive2, 3);
                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");

                    // Get all of the legal words from the gameboard for these two players.
                    HashSet<string> LegalWords = FindLegalWords(GameboardString);

                    ClientSS1.BeginSend("WORD " + LegalWords.ElementAt(0) + "\n", (e, o) => { }, null);
                    mre1 = new ManualResetEvent(false);
                    mre2 = new ManualResetEvent(false);

                    ClientSS1.BeginReceive(CompletedReceive1, 2);
                    ClientSS2.BeginReceive(CompletedReceive2, 3);

                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual("SCORE 1 0", s1);
                    Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");
                    Assert.AreEqual("SCORE 0 1", s2);
                }
                finally
                {
                    ClientSS1.Close();
                    ClientSS2.Close();
                }
            }

            public void TestWordCommand2()
            {
                // Test for words that are played that are less than 3 characters in
                // length. Assert that the scores didn't change.

                // This will coordinate communication between the threads of the test cases
                mre1 = new ManualResetEvent(false);
                mre2 = new ManualResetEvent(false);

                // Create a two clients to connect with the Boggle Server.
                TcpClient TestClient1 = new TcpClient("localhost", 2000);
                TcpClient TestClient2 = new TcpClient("localhost", 2000);

                // Create a client socket and then a client StringSocket.
                Socket ClientSocket1 = TestClient1.Client;
                Socket ClientSocket2 = TestClient2.Client;
                StringSocket ClientSS1 = new StringSocket(ClientSocket1, new UTF8Encoding());
                StringSocket ClientSS2 = new StringSocket(ClientSocket2, new UTF8Encoding());

                try
                {
                    // Connect two clients together to play Boggle.
                    ClientSS1.BeginSend("PLAY Client1\n", (e, o) => { }, null);
                    ClientSS2.BeginSend("PLAY Client2\n", (e, o) => { }, null);

                    ClientSS1.BeginReceive(CompletedReceive1, 1);
                    ClientSS2.BeginReceive(CompletedReceive2, 3);
                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");

                    // Get all of the legal words from the gameboard for these two players.
                    HashSet<string> LegalWords = FindXLetterWords(GameboardString, 3);

                    // Send a legal word and a word with less than 3 characters. The score
                    // shouldn't change.
                    ClientSS1.BeginSend("WORD " + LegalWords.ElementAt(0) + "\n", (e, o) => { }, null);
                    ClientSS1.BeginSend("WORD xx\n", (e, o) => { }, null);
                    ClientSS1.BeginSend("WORD " + LegalWords.ElementAt(1) + "\n", (e, o) => { }, null);

                    mre1 = new ManualResetEvent(false);
                    mre2 = new ManualResetEvent(false);


                    ClientSS1.BeginReceive(CompletedReceive1, 2);
                    ClientSS1.BeginReceive(CompletedReceive2, 2);

                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual("SCORE 1 0", s1);
                    Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 3");
                    Assert.AreEqual("SCORE 2 0", s2);

                    mre1 = new ManualResetEvent(false);
                    mre2 = new ManualResetEvent(false);

                    ClientSS2.BeginReceive(CompletedReceive1, 3);
                    ClientSS2.BeginReceive(CompletedReceive2, 3);

                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual("SCORE 0 1", s1);
                    Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 3");
                    Assert.AreEqual("SCORE 0 2", s2);
                }
                finally
                {
                    ClientSS1.Close();
                    ClientSS2.Close();
                }


            }

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
        public void TestDuplicates()
        {
            DuplicateTests DupTests = new DuplicateTests();

            DupTests.TestWordCommand3();
            DupTests.TestWordCommand4();
            DupTests.TestWordCommand5();
            DupTests.TestWordCommand6();
        }

        [TestClass]
        public class DuplicateTests
        {
            private ManualResetEvent mre1;
            private ManualResetEvent mre2;
            private ManualResetEvent mre3;
            private ManualResetEvent mre4;
            private ManualResetEvent mre5;
            private ManualResetEvent mre6;

            private String s1;
            private object p1;
            private String s2;
            private object p2;
            private String s3;
            private object p3;
            private String s4;
            private object p4;
            private String s5;
            private object p5;
            private String s6;
            private object p6;

            public void TestWordCommand3()
            {
                // Test the case in which a client plays a duplicate word.

                // This will coordinate communication between the threads of the test cases
                mre1 = new ManualResetEvent(false);
                mre2 = new ManualResetEvent(false); 
                mre3 = new ManualResetEvent(false);
                mre4 = new ManualResetEvent(false);
                mre5 = new ManualResetEvent(false);
                mre6 = new ManualResetEvent(false);

                // Create a two clients to connect with the Boggle Server.
                TcpClient TestClient1 = new TcpClient("localhost", 2000);
                TcpClient TestClient2 = new TcpClient("localhost", 2000);

                // Create a client socket and then a client StringSocket.
                Socket ClientSocket1 = TestClient1.Client;
                Socket ClientSocket2 = TestClient2.Client;
                StringSocket ClientSS1 = new StringSocket(ClientSocket1, new UTF8Encoding());
                StringSocket ClientSS2 = new StringSocket(ClientSocket2, new UTF8Encoding());
                try
                {
                    // Connect two clients together to play Boggle.
                    ClientSS1.BeginSend("PLAY Client1\n", (e, o) => { }, null);
                    ClientSS2.BeginSend("PLAY Client2\n", (e, o) => { }, null);

                    ClientSS1.BeginReceive(CompletedReceive1, 1);
                    ClientSS2.BeginReceive(CompletedReceive2, 3);
                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");

                    // Get all of the legal words from the gameboard for these two players.
                    HashSet<string> LegalWords = FindXLetterWords(GameboardString, 3);

                    // Send a duplicate word.
                    ClientSS1.BeginSend("WORD " + LegalWords.ElementAt(0) + "\n", (e, o) => { }, null);
                    ClientSS1.BeginSend("WORD " + LegalWords.ElementAt(0) + "\n", (e, o) => { }, null);

                    ClientSS1.BeginSend("WORD " + LegalWords.ElementAt(1) + "\n", (e, o) => { }, null);

                    ClientSS1.BeginReceive(CompletedReceive3, 2);
                    ClientSS1.BeginReceive(CompletedReceive4, 2);

                    Assert.AreEqual(true, mre3.WaitOne(timeout), "Timed out waiting 3");
                    Assert.AreEqual("SCORE 1 0", s3);
                    Assert.AreEqual(true, mre4.WaitOne(timeout), "Timed out waiting 4");
                    Assert.AreEqual("SCORE 2 0", s4);

                    mre1 = new ManualResetEvent(false);
                    mre2 = new ManualResetEvent(false);

                    ClientSS2.BeginReceive(CompletedReceive5, 3);
                    ClientSS2.BeginReceive(CompletedReceive6, 3);

                    Assert.AreEqual(true, mre5.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual("SCORE 0 1", s5);
                    Assert.AreEqual(true, mre6.WaitOne(timeout), "Timed out waiting 3");
                    Assert.AreEqual("SCORE 0 2", s6);
                }
                finally
                {
                    ClientSS1.Close();
                    ClientSS2.Close();
                }
            }

            public void TestWordCommand4()
            {
                // Test the case in which a the two clients play the same word.

                // This will coordinate communication between the threads of the test cases
                mre3 = new ManualResetEvent(false);
                mre4 = new ManualResetEvent(false);
                mre5 = new ManualResetEvent(false);
                mre6 = new ManualResetEvent(false);

                // Create a two clients to connect with the Boggle Server.
                TcpClient TestClient1 = new TcpClient("localhost", 2000);
                TcpClient TestClient2 = new TcpClient("localhost", 2000);

                // Create a client socket and then a client StringSocket.
                Socket ClientSocket1 = TestClient1.Client;
                Socket ClientSocket2 = TestClient2.Client;
                StringSocket ClientSS1 = new StringSocket(ClientSocket1, new UTF8Encoding());
                StringSocket ClientSS2 = new StringSocket(ClientSocket2, new UTF8Encoding());

                try
                {
                    // Connect two clients together to play Boggle.
                    ClientSS1.BeginSend("PLAY Client1\n", (e, o) => { }, null);
                    ClientSS2.BeginSend("PLAY Client2\n", (e, o) => { }, null);

                    ClientSS1.BeginReceive(CompletedReceive1, 1);
                    ClientSS2.BeginReceive(CompletedReceive2, 2);
                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");

                    // Get all of the legal words from the gameboard for these two players.
                    HashSet<string> LegalWords = FindLegalWords(GameboardString);

                    // One player plays a legal word. Assert that the score changes for each
                    // player appropriately.
                    ClientSS1.BeginSend("WORD " + LegalWords.ElementAt(0) + "\n", (e, o) => { }, null);

                    ClientSS1.BeginReceive(CompletedReceive3, 3);
                    ClientSS2.BeginReceive(CompletedReceive4, 4);

                    Assert.AreEqual(true, mre3.WaitOne(timeout), "Timed out waiting 3");
                    Assert.AreEqual("SCORE 1 0", s3);

                    Assert.AreEqual(true, mre4.WaitOne(timeout), "Timed out waiting 4");
                    Assert.AreEqual("SCORE 0 1", s4);

                    ClientSS2.BeginSend("WORD " + LegalWords.ElementAt(0) + "\n", (e, o) => { }, null);

                    ClientSS1.BeginReceive(CompletedReceive5, 5);
                    ClientSS2.BeginReceive(CompletedReceive6, 6);

                    Assert.AreEqual(true, mre5.WaitOne(timeout), "Timed out waiting 5");
                    Assert.AreEqual("SCORE 0 0", s5);

                    Assert.AreEqual(true, mre6.WaitOne(timeout), "Timed out waiting 6");
                    Assert.AreEqual("SCORE 0 0", s6);
                }
                finally
                {
                    ClientSS1.Close();
                    ClientSS2.Close();
                }
            }

            public void TestWordCommand5()
            {
                // Test the case in which a client plays a duplicate word.

                // This will coordinate communication between the threads of the test cases
                mre1 = new ManualResetEvent(false);
                mre2 = new ManualResetEvent(false);
                mre3 = new ManualResetEvent(false);
                mre4 = new ManualResetEvent(false);
                mre5 = new ManualResetEvent(false);
                mre6 = new ManualResetEvent(false);

                // Create a two clients to connect with the Boggle Server.
                TcpClient TestClient1 = new TcpClient("localhost", 2000);
                TcpClient TestClient2 = new TcpClient("localhost", 2000);

                // Create a client socket and then a client StringSocket.
                Socket ClientSocket1 = TestClient1.Client;
                Socket ClientSocket2 = TestClient2.Client;
                StringSocket ClientSS1 = new StringSocket(ClientSocket1, new UTF8Encoding());
                StringSocket ClientSS2 = new StringSocket(ClientSocket2, new UTF8Encoding());
                try
                {
                    // Connect two clients together to play Boggle.
                    ClientSS1.BeginSend("PLAY Client1\n", (e, o) => { }, null);
                    ClientSS2.BeginSend("PLAY Client2\n", (e, o) => { }, null);

                    ClientSS1.BeginReceive(CompletedReceive1, 1);
                    ClientSS2.BeginReceive(CompletedReceive2, 3);
                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");

                    // Get all of the legal words from the gameboard for these two players.
                    HashSet<string> LegalWords = FindXLetterWords(GameboardString, 3);

                    // Send a duplicate word.
                    ClientSS2.BeginSend("WORD " + LegalWords.ElementAt(0) + "\n", (e, o) => { }, null);
                    ClientSS2.BeginSend("WORD " + LegalWords.ElementAt(0) + "\n", (e, o) => { }, null);

                    ClientSS2.BeginSend("WORD " + LegalWords.ElementAt(1) + "\n", (e, o) => { }, null);

                    ClientSS2.BeginReceive(CompletedReceive3, 2);
                    ClientSS2.BeginReceive(CompletedReceive4, 2);

                    Assert.AreEqual(true, mre3.WaitOne(timeout), "Timed out waiting 3");
                    Assert.AreEqual("SCORE 1 0", s3);
                    Assert.AreEqual(true, mre4.WaitOne(timeout), "Timed out waiting 4");
                    Assert.AreEqual("SCORE 2 0", s4);

                    mre1 = new ManualResetEvent(false);
                    mre2 = new ManualResetEvent(false);

                    ClientSS1.BeginReceive(CompletedReceive5, 3);
                    ClientSS1.BeginReceive(CompletedReceive6, 3);

                    Assert.AreEqual(true, mre5.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual("SCORE 0 1", s5);
                    Assert.AreEqual(true, mre6.WaitOne(timeout), "Timed out waiting 3");
                    Assert.AreEqual("SCORE 0 2", s6);
                }
                finally
                {
                    ClientSS1.Close();
                    ClientSS2.Close();
                }
            }

            public void TestWordCommand6()
            {
                // Test the case in which a the two clients play the same word.

                // This will coordinate communication between the threads of the test cases
                mre3 = new ManualResetEvent(false);
                mre4 = new ManualResetEvent(false);
                mre5 = new ManualResetEvent(false);
                mre6 = new ManualResetEvent(false);

                // Create a two clients to connect with the Boggle Server.
                TcpClient TestClient1 = new TcpClient("localhost", 2000);
                TcpClient TestClient2 = new TcpClient("localhost", 2000);

                // Create a client socket and then a client StringSocket.
                Socket ClientSocket1 = TestClient1.Client;
                Socket ClientSocket2 = TestClient2.Client;
                StringSocket ClientSS1 = new StringSocket(ClientSocket1, new UTF8Encoding());
                StringSocket ClientSS2 = new StringSocket(ClientSocket2, new UTF8Encoding());

                try
                {
                    // Connect two clients together to play Boggle.
                    ClientSS1.BeginSend("PLAY Client1\n", (e, o) => { }, null);
                    ClientSS2.BeginSend("PLAY Client2\n", (e, o) => { }, null);

                    ClientSS1.BeginReceive(CompletedReceive1, 1);
                    ClientSS2.BeginReceive(CompletedReceive2, 2);
                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");

                    // Get all of the legal words from the gameboard for these two players.
                    HashSet<string> LegalWords = FindLegalWords(GameboardString);

                    // One player plays a legal word. Assert that the score changes for each
                    // player appropriately.
                    ClientSS2.BeginSend("WORD " + LegalWords.ElementAt(0) + "\n", (e, o) => { }, null);

                    ClientSS1.BeginReceive(CompletedReceive3, 3);
                    ClientSS2.BeginReceive(CompletedReceive4, 4);

                    Assert.AreEqual(true, mre3.WaitOne(timeout), "Timed out waiting 3");
                    Assert.AreEqual("SCORE 0 1", s3);

                    Assert.AreEqual(true, mre4.WaitOne(timeout), "Timed out waiting 4");
                    Assert.AreEqual("SCORE 1 0", s4);

                    ClientSS1.BeginSend("WORD " + LegalWords.ElementAt(0) + "\n", (e, o) => { }, null);

                    ClientSS1.BeginReceive(CompletedReceive5, 5);
                    ClientSS2.BeginReceive(CompletedReceive6, 6);

                    Assert.AreEqual(true, mre5.WaitOne(timeout), "Timed out waiting 5");
                    Assert.AreEqual("SCORE 0 0", s5);

                    Assert.AreEqual(true, mre6.WaitOne(timeout), "Timed out waiting 6");
                    Assert.AreEqual("SCORE 0 0", s6);
                }
                finally
                {
                    ClientSS1.Close();
                    ClientSS2.Close();
                }
            }

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

            private void CompletedReceive3(String s, Exception o, object payload)
            {
                s3 = s;
                p3 = payload;
                mre3.Set();
            }
            private void CompletedReceive4(String s, Exception o, object payload)
            {
                s4 = s;
                p4 = payload;
                mre4.Set();
            }
            private void CompletedReceive5(String s, Exception o, object payload)
            {
                s5 = s;
                p5 = payload;
                mre5.Set();
            }
            private void CompletedReceive6(String s, Exception o, object payload)
            {
                s6 = s;
                p6 = payload;
                mre6.Set();
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
        public void TestIllegalWords()
        {
            IllegalWordTests IllegalWords = new IllegalWordTests();

            IllegalWords.TestWordCommand5();
            IllegalWords.TestWordCommand6();
        }

        [TestClass]
        public class IllegalWordTests
        {
            private ManualResetEvent mre1;
            private ManualResetEvent mre2;

            private String s1;
            private object p1;
            private String s2;
            private object p2;

            public void TestWordCommand5()
            {
                // Assert that an illegal word with more than 3 characters decrements the players
                // score.

                // This will coordinate communication between the threads of the test cases
                mre1 = new ManualResetEvent(false);
                mre2 = new ManualResetEvent(false);

                // Create a two clients to connect with the Boggle Server.
                TcpClient TestClient1 = new TcpClient("localhost", 2000);
                TcpClient TestClient2 = new TcpClient("localhost", 2000);

                // Create a client socket and then a client StringSocket.
                Socket ClientSocket1 = TestClient1.Client;
                Socket ClientSocket2 = TestClient2.Client;
                StringSocket ClientSS1 = new StringSocket(ClientSocket1, new UTF8Encoding());
                StringSocket ClientSS2 = new StringSocket(ClientSocket2, new UTF8Encoding());

                try
                {
                    // Connect two clients together to play Boggle.
                    ClientSS1.BeginSend("PLAY Client1\n", (e, o) => { }, null);
                    ClientSS2.BeginSend("PLAY Client2\n", (e, o) => { }, null);

                    ClientSS1.BeginReceive(CompletedReceive1, 1);
                    ClientSS2.BeginReceive(CompletedReceive2, 3);
                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");

                    // Get all of the Illegal words from the gameboard for these two players.
                    HashSet<string> IlegalWords = FindIllegalWords(GameboardString);

                    // One player plays an illegal word. Assert that the score changes for each
                    // player appropriately.
                    ClientSS1.BeginSend("WORD xxxxxx\n", (e, o) => { }, null);

                    mre1 = new ManualResetEvent(false);
                    mre2 = new ManualResetEvent(false);
                    ClientSS1.BeginReceive(CompletedReceive1, 2);
                    ClientSS2.BeginReceive(CompletedReceive2, 3);

                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual("SCORE -1 0", s1);

                    Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");
                    Assert.AreEqual("SCORE 0 -1", s2);
                }
                finally
                {
                    ClientSS1.Close();
                    ClientSS2.Close();
                }
            }

            public void TestWordCommand6()
            {
                // Assert that an illegal word with more than 3 characters that is in the dictionary
                // decrements the players' score.

                // This will coordinate communication between the threads of the test cases
                mre1 = new ManualResetEvent(false);
                mre2 = new ManualResetEvent(false);

                // Create a two clients to connect with the Boggle Server.
                TcpClient TestClient1 = new TcpClient("localhost", 2000);
                TcpClient TestClient2 = new TcpClient("localhost", 2000);

                // Create a client socket and then a client StringSocket.
                Socket ClientSocket1 = TestClient1.Client;
                Socket ClientSocket2 = TestClient2.Client;
                StringSocket ClientSS1 = new StringSocket(ClientSocket1, new UTF8Encoding());
                StringSocket ClientSS2 = new StringSocket(ClientSocket2, new UTF8Encoding());

                try
                {
                    // Connect two clients together to play Boggle.
                    ClientSS1.BeginSend("PLAY Client1\n", (e, o) => { }, null);
                    ClientSS2.BeginSend("PLAY Client2\n", (e, o) => { }, null);

                    ClientSS1.BeginReceive(CompletedReceive1, 1);
                    ClientSS2.BeginReceive(CompletedReceive2, 3);
                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");

                    // Get all of the legal words from the gameboard for these two players.
                    HashSet<string> IllegalWords = FindIllegalWords(GameboardString);

                    // One player plays an illegal word. Assert that the score changes for each
                    // player appropriately.
                    ClientSS1.BeginSend("WORD " + IllegalWords.ElementAt(0) + "\n", (e, o) => { }, null);

                    mre1 = new ManualResetEvent(false);
                    mre2 = new ManualResetEvent(false);
                    ClientSS1.BeginReceive(CompletedReceive1, 2);
                    ClientSS2.BeginReceive(CompletedReceive2, 3);

                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual("SCORE -1 0", s1);

                    Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");
                    Assert.AreEqual("SCORE 0 -1", s2);
                }
                finally
                {
                    ClientSS1.Close();
                    ClientSS2.Close();
                }
            }

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
        public void TestLegalWords()
        {

            LegalWordScoreTests LegalWordTests = new LegalWordScoreTests();

            LegalWordTests.TestWordCommand7();

            LegalWordTests.TestWordCommand8();

            LegalWordTests.TestWordCommand9();

            LegalWordTests.TestWordCommand10();

            LegalWordTests.TestWordCommand11();

            LegalWordTests.TestWordCommand12();

        }

        [TestClass]
        public class LegalWordScoreTests
        {
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

            public void TestWordCommand7()
            {
                // Assert that a legal word with 3 characters increments the players
                // score.

                // This will coordinate communication between the threads of the test cases
                mre1 = new ManualResetEvent(false);
                mre2 = new ManualResetEvent(false);

                // Create a two clients to connect with the Boggle Server.
                TcpClient TestClient1 = new TcpClient("localhost", 2000);
                TcpClient TestClient2 = new TcpClient("localhost", 2000);

                // Create a client socket and then a client StringSocket.
                Socket ClientSocket1 = TestClient1.Client;
                Socket ClientSocket2 = TestClient2.Client;
                StringSocket ClientSS1 = new StringSocket(ClientSocket1, new UTF8Encoding());
                StringSocket ClientSS2 = new StringSocket(ClientSocket2, new UTF8Encoding());

                try
                {
                    // Connect two clients together to play Boggle.
                    ClientSS1.BeginSend("PLAY Client1\n", (e, o) => { }, null);
                    ClientSS2.BeginSend("PLAY Client2\n", (e, o) => { }, null);

                    ClientSS1.BeginReceive(CompletedReceive1, 1);
                    ClientSS2.BeginReceive(CompletedReceive2, 3);
                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");

                    // Get all of the legal words from the gameboard for these two players.
                    HashSet<string> LegalWords = FindXLetterWords(GameboardString, 3);

                    // One player plays a legal word. Assert that the score changes for each
                    // player appropriately.
                    ClientSS1.BeginSend("WORD " + LegalWords.ElementAt(0) + "\n", (e, o) => { }, null);

                    mre1 = new ManualResetEvent(false);
                    mre2 = new ManualResetEvent(false);
                    ClientSS1.BeginReceive(CompletedReceive1, 2);
                    ClientSS2.BeginReceive(CompletedReceive2, 3);

                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual("SCORE 1 0", s1);

                    Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");
                    Assert.AreEqual("SCORE 0 1", s2);
                }
                finally
                {
                    ClientSS1.Close();
                    ClientSS2.Close();
                }
            }

            public void TestWordCommand8()
            {
                // Assert that a legal word with 4 characters increments the players
                // score.

                // This will coordinate communication between the threads of the test cases
                mre1 = new ManualResetEvent(false);
                mre2 = new ManualResetEvent(false);

                // Create a two clients to connect with the Boggle Server.
                TcpClient TestClient1 = new TcpClient("localhost", 2000);
                TcpClient TestClient2 = new TcpClient("localhost", 2000);

                // Create a client socket and then a client StringSocket.
                Socket ClientSocket1 = TestClient1.Client;
                Socket ClientSocket2 = TestClient2.Client;
                StringSocket ClientSS1 = new StringSocket(ClientSocket1, new UTF8Encoding());
                StringSocket ClientSS2 = new StringSocket(ClientSocket2, new UTF8Encoding());
                try
                {
                    // Connect two clients together to play Boggle.
                    ClientSS1.BeginSend("PLAY Client1\n", (e, o) => { }, null);
                    ClientSS2.BeginSend("PLAY Client2\n", (e, o) => { }, null);

                    ClientSS1.BeginReceive(CompletedReceive1, 1);
                    ClientSS2.BeginReceive(CompletedReceive2, 3);
                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");

                    // Get all of the legal words from the gameboard for these two players.
                    HashSet<string> LegalWords = FindXLetterWords(GameboardString, 4);

                    // One player plays a legal word. Assert that the score changes for each
                    // player appropriately.
                    ClientSS1.BeginSend("WORD " + LegalWords.ElementAt(0) + "\n", (e, o) => { }, null);

                    mre1 = new ManualResetEvent(false);
                    mre2 = new ManualResetEvent(false);
                    ClientSS1.BeginReceive(CompletedReceive1, 2);
                    ClientSS2.BeginReceive(CompletedReceive2, 3);

                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual("SCORE 1 0", s1);

                    Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");
                    Assert.AreEqual("SCORE 0 1", s2);

                }
                finally
                {
                    ClientSS1.Close();
                    ClientSS2.Close();
                }
            }

            public void TestWordCommand9()
            {
                // Assert that a legal word with 5 characters increments the players
                // score.

                // This will coordinate communication between the threads of the test cases
                mre1 = new ManualResetEvent(false);
                mre2 = new ManualResetEvent(false);

                // Create a two clients to connect with the Boggle Server.
                TcpClient TestClient1 = new TcpClient("localhost", 2000);
                TcpClient TestClient2 = new TcpClient("localhost", 2000);

                // Create a client socket and then a client StringSocket.
                Socket ClientSocket1 = TestClient1.Client;
                Socket ClientSocket2 = TestClient2.Client;
                StringSocket ClientSS1 = new StringSocket(ClientSocket1, new UTF8Encoding());
                StringSocket ClientSS2 = new StringSocket(ClientSocket2, new UTF8Encoding());

                try
                {
                    // Connect two clients together to play Boggle.
                    ClientSS1.BeginSend("PLAY Client1\n", (e, o) => { }, null);
                    ClientSS2.BeginSend("PLAY Client2\n", (e, o) => { }, null);

                    ClientSS1.BeginReceive(CompletedReceive1, 1);
                    ClientSS2.BeginReceive(CompletedReceive2, 3);
                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");

                    // Get all of the legal words from the gameboard for these two players.
                    HashSet<string> LegalWords = FindXLetterWords(GameboardString, 5);

                    // One player plays a legal word. Assert that the score changes for each
                    // player appropriately.
                    ClientSS1.BeginSend("WORD " + LegalWords.ElementAt(0) + "\n", (e, o) => { }, null);

                    mre1 = new ManualResetEvent(false);
                    mre2 = new ManualResetEvent(false);
                    ClientSS1.BeginReceive(CompletedReceive1, 2);
                    ClientSS2.BeginReceive(CompletedReceive2, 3);

                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual("SCORE 2 0", s1);

                    Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");
                    Assert.AreEqual("SCORE 0 2", s2);
                }
                finally
                {
                    ClientSS1.Close();
                    ClientSS2.Close();
                }
            }

            public void TestWordCommand10()
            {
                // Assert that a legal word with 6 characters increments the players
                // score.

                // This will coordinate communication between the threads of the test cases
                mre1 = new ManualResetEvent(false);
                mre2 = new ManualResetEvent(false);

                // Create a two clients to connect with the Boggle Server.
                TcpClient TestClient1 = new TcpClient("localhost", 2000);
                TcpClient TestClient2 = new TcpClient("localhost", 2000);

                // Create a client socket and then a client StringSocket.
                Socket ClientSocket1 = TestClient1.Client;
                Socket ClientSocket2 = TestClient2.Client;
                StringSocket ClientSS1 = new StringSocket(ClientSocket1, new UTF8Encoding());
                StringSocket ClientSS2 = new StringSocket(ClientSocket2, new UTF8Encoding());

                try
                {
                    // Connect two clients together to play Boggle.
                    ClientSS1.BeginSend("PLAY Client1\n", (e, o) => { }, null);
                    ClientSS2.BeginSend("PLAY Client2\n", (e, o) => { }, null);

                    ClientSS1.BeginReceive(CompletedReceive1, 1);
                    ClientSS2.BeginReceive(CompletedReceive2, 3);
                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");

                    // Get all of the legal words from the gameboard for these two players.
                    HashSet<string> LegalWords = FindXLetterWords(GameboardString, 6);

                    // One player plays a legal word. Assert that the score changes for each
                    // player appropriately.
                    ClientSS1.BeginSend("WORD " + LegalWords.ElementAt(0) + "\n", (e, o) => { }, null);

                    mre1 = new ManualResetEvent(false);
                    mre2 = new ManualResetEvent(false);
                    ClientSS1.BeginReceive(CompletedReceive1, 2);
                    ClientSS2.BeginReceive(CompletedReceive2, 3);

                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual("SCORE 3 0", s1);

                    Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");
                    Assert.AreEqual("SCORE 0 3", s2);
                }
                finally
                {
                    ClientSS1.Close();
                    ClientSS2.Close();
                }
            }

            public void TestWordCommand11()
            {
                // Assert that a legal word with 7 characters increments the players
                // score.

                // This will coordinate communication between the threads of the test cases
                mre1 = new ManualResetEvent(false);
                mre2 = new ManualResetEvent(false);

                // Create a two clients to connect with the Boggle Server.
                TcpClient TestClient1 = new TcpClient("localhost", 2000);
                TcpClient TestClient2 = new TcpClient("localhost", 2000);

                // Create a client socket and then a client StringSocket.
                Socket ClientSocket1 = TestClient1.Client;
                Socket ClientSocket2 = TestClient2.Client;
                StringSocket ClientSS1 = new StringSocket(ClientSocket1, new UTF8Encoding());
                StringSocket ClientSS2 = new StringSocket(ClientSocket2, new UTF8Encoding());

                try
                {
                    // Connect two clients together to play Boggle.
                    ClientSS1.BeginSend("PLAY Client1\n", (e, o) => { }, null);
                    ClientSS2.BeginSend("PLAY Client2\n", (e, o) => { }, null);

                    ClientSS1.BeginReceive(CompletedReceive1, 1);
                    ClientSS2.BeginReceive(CompletedReceive2, 3);
                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");

                    // Get all of the legal words from the gameboard for these two players.
                    HashSet<string> LegalWords = FindXLetterWords(GameboardString, 7);

                    // One player plays a legal word. Assert that the score changes for each
                    // player appropriately.
                    ClientSS1.BeginSend("WORD " + LegalWords.ElementAt(0) + "\n", (e, o) => { }, null);

                    mre1 = new ManualResetEvent(false);
                    mre2 = new ManualResetEvent(false);
                    ClientSS1.BeginReceive(CompletedReceive1, 2);
                    ClientSS2.BeginReceive(CompletedReceive2, 3);

                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual("SCORE 5 0", s1);

                    Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");
                    Assert.AreEqual("SCORE 0 5", s2);
                }
                finally
                {
                    ClientSS1.Close();
                    ClientSS2.Close();
                }
            }

            public void TestWordCommand12()
            {
                // Assert that a legal word with 8 characters increments the players
                // score.

                // This will coordinate communication between the threads of the test cases
                mre1 = new ManualResetEvent(false);
                mre2 = new ManualResetEvent(false);

                // Create a two clients to connect with the Boggle Server.
                TcpClient TestClient1 = new TcpClient("localhost", 2000);
                TcpClient TestClient2 = new TcpClient("localhost", 2000);

                // Create a client socket and then a client StringSocket.
                Socket ClientSocket1 = TestClient1.Client;
                Socket ClientSocket2 = TestClient2.Client;
                StringSocket ClientSS1 = new StringSocket(ClientSocket1, new UTF8Encoding());
                StringSocket ClientSS2 = new StringSocket(ClientSocket2, new UTF8Encoding());

                try
                {
                    // Connect two clients together to play Boggle.
                    ClientSS1.BeginSend("PLAY Client1\n", (e, o) => { }, null);
                    ClientSS2.BeginSend("PLAY Client2\n", (e, o) => { }, null);

                    ClientSS1.BeginReceive(CompletedReceive1, 1);
                    ClientSS2.BeginReceive(CompletedReceive2, 3);
                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");

                    // Get all of the legal words from the gameboard for these two players.
                    HashSet<string> LegalWords = FindXLetterWords(GameboardString, 8);

                    // One player plays a legal word. Assert that the score changes for each
                    // player appropriately.
                    ClientSS1.BeginSend("WORD " + LegalWords.ElementAt(0) + "\n", (e, o) => { }, null);

                    mre1 = new ManualResetEvent(false);
                    mre2 = new ManualResetEvent(false);
                    ClientSS1.BeginReceive(CompletedReceive1, 2);
                    ClientSS2.BeginReceive(CompletedReceive2, 3);

                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual("SCORE 11 0", s1);

                    Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");
                    Assert.AreEqual("SCORE 0 11", s2);
                }
                finally
                {
                    ClientSS1.Close();
                    ClientSS2.Close();
                }
            }

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

            private void CompletedReceive3(String s, Exception o, object payload)
            {
                s3 = s;
                p3 = payload;
                mre3.Set();
            }

            // This is the callback for the second receive request.
            private void CompletedReceive4(String s, Exception o, object payload)
            {
                s4 = s;
                p4 = payload;
                mre4.Set();
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
        public void TestQWords()
        {
             new QWordTests().TestWordCommand13();
        }

        [TestClass]
        public class QWordTests
        {
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

            public void TestWordCommand13()
            {
                // Assert that a legal word with Q as one of its characters and increments the
                // players score by the correct amount.

                // This will coordinate communication between the threads of the test cases
                mre1 = new ManualResetEvent(false);
                mre2 = new ManualResetEvent(false);
                mre3 = new ManualResetEvent(false);
                mre4 = new ManualResetEvent(false);

                // Create a two clients to connect with the Boggle Server.
                TcpClient TestClient1 = new TcpClient("localhost", 2000);
                TcpClient TestClient2 = new TcpClient("localhost", 2000);

                // Create a client socket and then a client StringSocket.
                Socket ClientSocket1 = TestClient1.Client;
                Socket ClientSocket2 = TestClient2.Client;
                StringSocket ClientSS1 = new StringSocket(ClientSocket1, new UTF8Encoding());
                StringSocket ClientSS2 = new StringSocket(ClientSocket2, new UTF8Encoding());

                try
                {
                    // Connect two clients together to play Boggle.
                    ClientSS1.BeginSend("PLAY Client1\n", (e, o) => { }, null);
                    ClientSS2.BeginSend("PLAY Client2\n", (e, o) => { }, null);

                    ClientSS1.BeginReceive(CompletedReceive1, 1);
                    ClientSS2.BeginReceive(CompletedReceive2, 3);
                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");

                    // Get all of the legal words from the gameboard for these two players.
                    HashSet<string> LegalWords = FindLegalWords(GameboardString);

                    // One player plays a legal word. Assert that the score changes for each
                    // player appropriately.
                    ClientSS1.BeginSend("WORD QACK\n", (e, o) => { }, null);

                    ClientSS1.BeginReceive(CompletedReceive3, 2);
                    ClientSS2.BeginReceive(CompletedReceive4, 3);

                    Assert.AreEqual(true, mre3.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual("SCORE 2 0", s3);

                    Assert.AreEqual(true, mre4.WaitOne(timeout), "Timed out waiting 2");
                    Assert.AreEqual("SCORE 0 2", s4);
                }
                finally
                {
                    ClientSS1.Close();
                    ClientSS2.Close();
                }
            }

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

            private void CompletedReceive3(String s, Exception o, object payload)
            {
                s3 = s;
                p3 = payload;
                mre3.Set();
            }

            // This is the callback for the second receive request.
            private void CompletedReceive4(String s, Exception o, object payload)
            {
                s4 = s;
                p4 = payload;
                mre4.Set();
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
        #endregion

        #region Helpers for Finding Legal/Illegal Words
        /// <summary>
        /// Finds all of the legal words of a given BoggleBoard based on the dictionary
        /// that was supplied.
        /// </summary>
        /// <param name="GameboardString">The randomly generated string representing the 
        /// current Gameboard.</param>
        private static HashSet<string> FindLegalWords(string GameboardString)
        {
            lock (LegalWordsLock)
            {
                BoggleBoard GameBoard = new BoggleBoard(GameboardString);
                HashSet<string> LegalWords = new HashSet<string>();

                foreach (string s in DictionaryWords)
                {
                    if (GameBoard.CanBeFormed(s) && s.Length > 2)
                        LegalWords.Add(s);
                }

                return LegalWords;
            }
        }

        /// <summary>
        /// Finds all of the legal words of a given BoggleBoard based on the dictionary
        /// that was supplied.
        /// </summary>
        /// <param name="GameboardString">The randomly generated string representing the 
        /// current Gameboard.</param>
        private static HashSet<string> FindXLetterWords(string GameboardString, int StringLength)
        {
            lock (XLetterWordsLock)
            {
                BoggleBoard GameBoard = new BoggleBoard(GameboardString);
                HashSet<string> LegalWords = new HashSet<string>();

                foreach (string s in DictionaryWords)
                {
                    if (GameBoard.CanBeFormed(s) && s.Length == StringLength)
                        LegalWords.Add(s);
                }

                return LegalWords;
            }
        }

        /// <summary>
        /// Finds all of the illegal words of a given BoggleBoard based on the dictionary
        /// that was supplied.
        /// </summary>
        /// <param name="GameboardString">The randomly generated string representing the 
        /// current Gameboard.</param>
        private static HashSet<string> FindIllegalWords(string GameboardString)
        {
            lock (IllegalWordsLock)
            {
                BoggleBoard GameBoard = new BoggleBoard(GameboardString);
                HashSet<string> IllegalWords = new HashSet<string>();

                foreach (string s in DictionaryWords)
                {
                    if (!GameBoard.CanBeFormed(s) && s.Length > 2)
                        IllegalWords.Add(s);
                }

                return IllegalWords;
            }
        }
        #endregion 

        [TestCleanup]
        public void CleanupConsoleOutput()
        {
            // Reset the standard console output stream.
            StreamWriter StandardOutput = new StreamWriter(Console.OpenStandardOutput());
            StandardOutput.AutoFlush = true;
            Console.SetOut(StandardOutput);
        }
    }
}
