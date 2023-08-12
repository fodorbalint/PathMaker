# PathMaker

This program aims to solve the following riddle:

"Generate a line that goes through an n x n grid (where n is a natural number), passing through each field once. The line has to start from the field at the upper left corner (1 x 1) and end at n x n. At any time it is allowed to move left, right, up or down, and it has to randomly choose between the available fields.
Output the image in svg format."
 
From the simple rules of movement and lines to complete in the future, complicated patterns can result. In certain cases when you have drawn a path, it can be revealed that the enclosed or surrounding area cannot be filled.

Here is an example:.

![alt text](https://github.com/fodorbalint/PathMaker/blob/main/References/0701_1.svg)

The program calculated the blue lines for you. Do you see why this situation is impossible?

In the beginning of the project I let the program run on a 21x21 field, and whenever I noticed a trouble, I coded the solution into it. While you can discover many patterns this way, a gradual approach may be more effective.

A 3x3 field can only be filled in two ways, this and its mirrored version:

<img src="blob/main/References/project/3x3.svg" width="14.3%"/>

The 5x5 requires much more consideration. Whenever it is possible to draw future lines, the program has to be able to do it. The future lines can not only extend at each step but connect too.

<img src="blob/main/References/0806.svg" width="23.8%"/>

