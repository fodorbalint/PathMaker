# One Way Labyrinth

This program aims to solve the following riddle:

"Generate a line that goes through an n x n grid (where n is a natural number), passing through each field once. The line has to start from the field at the upper left corner (1 x 1) and end at n x n. At any time it is allowed to move left, right, up or down, and it has to randomly choose between the available fields.
Output the image in svg format."
 
From the simple rules of movement and lines to complete in the future, complicated patterns can result. In certain cases when you have drawn a path, it can be revealed that the enclosed or surrounding area cannot be filled.

Here is an example:.

![alt text](https://github.com/fodorbalint/PathMaker/blob/main/References/0701_1.svg)

The program calculated the blue lines for you. Do you see why this situation is impossible?

In the beginning of the project I let the program run on a 21x21 field, and whenever I noticed a trouble, I coded the solution into it. While you can discover many patterns this way, a gradual approach may be more effective.

A 3x3 field can only be filled in two ways, this and its mirrored version:

<img src="References/project/3x3.svg" width="14.3%"/>

The 5x5 requires much more consideration. Whenever it is possible to draw future lines, the program has to be able to do it. The future lines can not only extend at each step but connect too.

<img src="References/0806.svg" width="23.8%"/>

As of August 21, 2023 all 5x5 scenarios are successfully handled. The number of walkthroughs are 104.
Improvements can be made to reduce computation time as many of the rules are not applicable at this size. With every aize, new rules will be added.

To summarize, here are the things to consider on 5x5:

<img src="References/0821_1.svg" width="23.8%"/>

- A single field next to the live end that is walled from two other sides (either by the border or the line) needs to be filled in the next step.
- A 2x3 empty area next to the live end that is walled by three sides (2-3-2) will have a future line going through along the walls. At the wall next to the main line, its direction is the opposite of the main line, meaning it will go from (3,2) upwards whereas the main line just took a step downwards. How the middle field will be filled is not yet known. Either the near end (the one the main line will go through first) or the far end can fill it.

<img src="References/0821_2.svg" width="23.8%"/>

- A 2x2 empty area next to the live end that is walled by three sides (2-2-2) will have a future line going through along the walls. In the example above, the far end is already extended by one step as it had only one option to move.

<img src="References/0821_3.svg" width="23.8%"/><img src="References/spacer.svg" width="8%"/><img src="References/0821_4.svg" width="23.8%"/>

Taking a step further, another future line is created an extended on the left side. Any step we take now will further extend and connect the two future lines, giving a complete walkthrough. Future lines are first extended when we step on them. Then, if there are other lines that started from the position next to where the live end was in the previous step, they get extended too.
Note that the line being stepped on has its end at (5,4). The nearby empty fields are (4,4) and the corner, (5,5). It cannot choose the corner, because then nothing would fill (4,4). Then, the line on the left gets extended until it connects to the other. As the near end cannot be extended more, the far end gets extended until it reaches the corner. 

There have not been found any case where the future line cannot extend, and the main line has to step back. This will change on 7x7. See these examples:

<img src="References/0821.svg" width="33.3%"/><img src="References/spacer.svg" width="8%"/><img src="References/0827.svg" width="33.3%"/>

In the first, the upper right future line fails when we step right. In the second, the future line on the left. The results are:

<img src="References/0821_fail.svg" width="33.3%"/><img src="References/spacer.svg" width="8%"/><img src="References/0827_fail.svg" width="33.3%"/>

The number of 7x7 walkthroughs may be tens or hundreds of thousands. Right now, I let the program run randomly to find errors to correct, but it may be possible in the future to run the program through all possibilities, detecting errors on its way. If the line reaches the corner, and the number of steps taken have been less than 49, there has been something wrong.