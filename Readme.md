# Scrabble Solver

This is a C# implementation of a program to search a Scrabble board for the best possible move given the current letters. Words are ranked by score.

## Command line arguments

The default config file will always be loaded before parsing arguments. Arguments may be used to override its values for customised behaviour.

The first unlabelled argument will always be the letters to solve with, and the second is the path to the game board to be loaded. All other arguments must be labelled (these two have their own labels as well).

Labels for values can either be '--key value' with a full name, or '-k value' with a short name. Short names may also omit the space such as '-kvalue'.