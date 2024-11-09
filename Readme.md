# Scrabble Solver

This is a C# implementation of a program to search a Scrabble board for the best possible move given the current letters. Words are ranked by score.

## Getting started

Ensure you have .NET 7.0 installed since that is what the project is built with.

Navigate a console to the 'Source' directory and run the sample program with the command 'dotnet run --config .\example.config'. You can experiment with this and try different letters, for example with the command 'dotnet run --gameboard ..\TestBoards\TestBoard.txt --letters abcdefg'. (This same command can be abbreviated to 'dotnet run abcdefg ..\TestBoards\TestBoard.txt' or 'dotnet run -g ..\TestBoards\TestBoard.txt -l abcdefg').

Another default board available is the theoretical highest scoring Scrabble move (I found it online). This can be run with 'dotnet run --config .\highestscore.config'.

## Some more stuff

The application can of course be built, an example command for this is 'dotnet publish -r win-x64 -c release'. Please ensure all config files are available and appropriately located for this.

When defining a game board letters which were placed with a blank tile can be denoted by a capital letter. For example, 'heLlo', where the player had the letters helo and a blank tile. However, the application does not currently support finding words while using a blank tile.

If you have a question feel free to get in touch via the Issues tab on GitHub. I'm happy to improve documentation and provide further clarification on any system which may be unclear. 

## Command line arguments

The default config file will always be loaded before parsing arguments. Arguments may be used to override its values for customised behaviour.

The first unlabelled argument will always be the letters to solve with, and the second is the path to the game board to be loaded. All other arguments must be labelled (these two have their own labels as well).

Labels for values can either be '--key value' with a full name, or '-k value' with a short name. Short names may also omit the space such as '-kvalue'.

The argument '--help' is available for a rundown of all commands.

The argument '--config' is available to load extra config files.