Jonathan Whitaker U0752100 & Daniel Houston U0671205
****************************************************
**********************PS8***************************

** TODO: Take into account what would happen if 
a player connected and then disconnected before another player conected. **

** TODO: Delete all carriage returns from Send method strings **
** TODO: Uncomment the BeginSend command on the time          **

Change Log:
	* ServerCommandReceived: 
	* What if the user disconnects before sending any initial command? I have included support to handle this.
	* I made each game run on its own thread now.

	* EndGame:
	* Added a helper to send out the game summary to each client.