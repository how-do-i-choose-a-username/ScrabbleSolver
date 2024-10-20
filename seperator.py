import sys
import os

if len(sys.argv)==2:
    # Open file to split
    file = open(sys.argv[1], "r")

    path = f"{sys.argv[1]}-seperated/"
    if not os.path.exists(path):
        os.mkdir(path)

    # Open files for words of length 1 through to 15
    outputs = [open(f"{path}list-{i}", "w") for i in range(1,16)]

    # For every line in the file, write it to the appropriate sub file
    for line in file:
        # Subtract 1 from the length to remove new lines
        length = len(line) - 1
        if length <= 15 and length > 0:
            outputs[length - 1].write(line)
else:
    print("Please include a single parameter with the name of the file to split")