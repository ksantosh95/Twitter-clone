-------------------------------------------------------------------------------------------

Project 4.2 - Twitter Clone

-------------------------------------------------------------------------------------------

Team Members:
-------------
1.	Avanti Kulkarni	UFID - 9517 1250
2.	Santosh Kannan	UFID - 9095 5971


Working Modules:
----------------
All the required functionalities are working:
1.	Register, subscribe, tweets with hashtags and mentions, and retweets through JSON based REST APIs.
2.	Querying tweets subscribed to, specific hashtags and user mentions.
3.	Live tweets and retweets are shared through web sockets
	
Steps for Execution:
------------------- 
1. 	Unzip the contents of the zipped folder (KannanKulkarni_Project4_2.zip)
2.	Move inside folder src
					"cd src"
3. 	Open Terminal / CMD window inside the folder location for running the engine process
					"dotnet run"
4. 	Wait till the engine terminal displays the message "APPLICATION STARTED"
5.	Open another terminal to start a client process
					"dotnet fsi --langversion:preview client.fsx" 
6.	Follow the instructions on the terminal to access the twitter clone
7.	Once the server is closed, please delete the database file called "sample.sqlite" before running the server again to start afresh
	

Output:
-------
1.	Primary Menu : This menu allows user to register and login
	[----------- LOGIN SCREEN -----------]
	1. Register
	2. Login

2.	Main Menu : User can choose what he wants to do among the below options:
	[----------- MAIN SCREEN -----------]
	1. Tweet
	2. Subscribe
	3. Get Subscribed Tweets
	4. Get My Mentions
	5. Get Hashtag Tweets
	6. ReTweet
	7. Logout
	8. Exit
	Enter your choice:
	
