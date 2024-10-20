import sys
import re

if len(sys.argv)==2:
    # Do thing, open file and remove bad words
    file = open(sys.argv[1], "r")
    output = open(f"{sys.argv[1]}-cleaned", "w")
    pattern = re.compile("[a-z]+\n")

    for line in file:
        match = re.match(pattern, line)
        if match is not None:
            output.write(line)

else:
    print("Please include a single parameter with the name of the file to clean")