# PathMaker

This program aims to solve the following riddle:

Generate a line that goes through an n x n grid (where n is a natural number), passing through each field once. The line has to start from the field at the upper left corner (1 x 1) and end at n x n. At any time it is allowed to move left, right, up or down, and it has to randomly choose between the available fields.
Output the image in svg format.
 
It does so applying the observed rules and by calculating lines to move along in the future depending on the current line. In many cases, the line cannot be completed, and I aim to discover them.
Here is an example, but see more under the References folder.

![alt text](https://github.com/fodorbalint/PathMaker/blob/main/References/0701_1.svg)