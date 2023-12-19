# The one-way labyrinth algorithm

This research aims to solve the following problem:<br />
"Draw a line that goes through an n x n grid (where n is an odd number), passing through each field once. The line has to start from the field at the upper left corner (1,1) and end at (n,n). At any time it is allowed to move left, right, up or down, and it has to randomly choose between the available fields."

At first sight it may look easy. But look at the following example:

<img src="References/0701_1.svg"/>

<!-- page 1 -->

Based on the black line's movement, blue fragments were drawn to indicate a path we have to go through in the future in order to fill the board.<br />
Do you see why the situation is impossible from now on?

The question is, is there a single rule or a set of rules that will guarantee you can draw a labyrinth of any size? Or do the rules get infinitely complex?

To assist with the research, I have written a computer program. In the beginning, I let it run on a 21 x 21 field, and whenever I noticed a trouble, I coded the solution into it. While you can discover many patterns this way, they will be random and do not help in gaining a fundemental understanding. At one point you will find things get too complex, and you are still far from solving the 21 x 21 board.<br />
That's where a gradual approach comes in.

A 3 x 3 area can only be filled in two ways, like this and mirrored:

<img src="References/3x3.svg" width="14.3%"/>

The 5 x 5 requires much more consideration. Whenever it is possible to draw future lines, the program has to be able to do it. The future lines can not only extend at each step but connect too.
<!-- specify extension and connection rules -->
<img src="References/0806.svg" width="23.8%"/>

By August 21, 2023 all 5 x 5 scenarios were discovered. The number of walkthroughs are 104.<br />
Here are the things to consider on a grid of this size:

<!-- page 2 -->

<img src="References/rules/5/C-Shape.svg" width="19.05%" align="top" /><img src="References/spacer.svg" width="4.76%"/><img src="References/C-Shape example.svg" width="23.8%" align="top" />

- A single field next to the live end that is walled from two other sides (either by the border or the line) needs to be filled in the next step. I call it C-shape. The pattern is both mirrored and rotated, so that the empty field is straight ahead. To qualify for this rule, the empty field cannot be the end corner. If there is a C-shape, we don't need to check other rules.

<img src="References/near border.svg" width="23.8%"/>

- Movement near the edge: In the example, we cannot step left (3,5), since the (2,5) field is empty. 

<img src="References/0821_1.svg" width="23.8%"/>

- A 2 x 3 empty area next to the live end that is walled by three sides (2-3-2 long) will have a future line going through along the walls. At the wall next to the main line, its direction is the opposite of the main line, meaning it will go from (3,2) upwards whereas the main line just took a step downwards. How the middle field will be filled is not yet known. Either the near end (the one the main line will go through first) or the far end can fill it.

<!-- page 3 -->

<img src="References/0821_2.svg" width="23.8%"/>

- A 2 x 2 empty area next to the live end that is walled by three sides (2-2-2 long) will have a future line going through along the walls. In this example, the far end is already extended by one step as it had only one option to move.

<img src="References/1019_9.svg" width="23.8%"/><img src="References/spacer.svg" width="4.76%"/><img src="References/1019_10.svg" width="23.8%"/>

- Future line extension when we step on a future line: The far can be extended if it was 2 distance away from the near end. It can now fill the C-shape.

<img src="References/1021_4.svg" width="23.8%"/><img src="References/spacer.svg" width="4.76%"/><img src="References/1021_5.svg" width="23.8%"/><img src="References/spacer.svg" width="4.76%"/><img src="References/1021_6.svg" width="23.8%"/>

The same goes with 1 x- and y-distance. A C-Shape is not always created in this case.

<!-- page 4 -->

<img src="References/1019_11.svg" width="23.8%"/><img src="References/spacer.svg" width="4.76%"/><img src="References/1019_12.svg" width="23.8%"/>

If the far end was near the end corner, it has to choose the other empty field.

<img src="References/0821_3.svg" width="23.8%"/>

- Future line extension when stepping away: If there was a near end where the main line was in the previous step, it now may have only one choice to move, so it can be extended.

<img src="References/future connection.svg" width="23.8%"/><img src="References/spacer.svg" width="4.76%"/><img src="References/0821_4.svg" width="23.8%"/>

- Future line connection: In this case, the line being stepped on extends until the far end has two options. (When the end corner is one of them, it has to be removed.) Then, the line on the left extends and now has no other option than to connect to the line on the right.<br />

<!-- page 5 -->

<img src="References/0930.svg" width="23.8%"/><img src="References/spacer.svg" width="4.76%"/><img src="References/0930_0_1.svg" width="23.8%"/>

- When we are two distance away from the edge, we need to check if stepping towards it is possible.
It is because if we do so, an enclosed area is created, with one way to go out of it. If that area has an impair amount of cells, it cannot be filled, so we cannot take that step.<br />
The explanation is simple: Imagine if the table was a chess board. In order to step from white to black, you would need to take an impair amount of steps - the color changes at every step. Here, the entry of the area would be (4,3) and the exit (5,3). An impair amount of steps means pair amount of cells.<br />
In the example, you can also say that we cannot step right, because there is a future line start 2 to straight and an end 2 to straight and 2 to right. On 7 x 7, there will be examples where this is the rule we have to apply, because area counting is not getting triggered: 

<img src="References/1001.svg" width="33.3%"/>

<!-- page 6 -->

But let's start with the simpler rules:

- Future line extension: When a near end is at 2 distance left or right from the live end, it will fill the field between them if the live end steps elsewhere. That's what happened in the 5 x 5 example above before the line failed.

<img src="References/0911.svg" width="33.3%"/><img src="References/spacer.svg" width="4.76%"/><img src="References/0911_0_1.svg" width="33.3%"/>

In other situations, there is a 1-thin future line next to the live end that can be extended if its far end is at the corner. Though disabling this rule does not affect the total amount of walkthroughs on a 7 x 7 grid, I chose to include it in the project on the basis that if a future line can be extended, we should do it. It can make a considerable difference. The left picture is without the rule, the right is with it.

<img src="References/0901.svg" width="33.3%"/>

- Just like moving near the edge, we need to disable some fields if we are approaching an older section of the main line. In order to determine on which side the enclosed area is created, we need to examine the direction of the line at the connection point.

<!-- page 7 -->

<img src="References/checknearfield/close straight left right.svg" width="9.5%"/><img src="References/spacer.svg" width="4.76%"/><img src="References/checknearfield/close straight left left.svg" width="14.3%"/><img src="References/spacer.svg" width="4.76%"/><img src="References/checknearfield/close straight right right.svg" width="14.3%"/><img src="References/spacer.svg" width="4.76%"/><img src="References/checknearfield/close straight right left.svg" width="9.5%"/>

The gray square means empty field. When the field 2 to straight is taken, its left or right side will be taken too.

<img src="References/checknearfield/close mid across right.svg" width="14.3%"/><img src="References/spacer.svg" width="4.76%"/><img src="References/checknearfield/close mid across left.svg" width="19%"/>

These will only be checked if one of the above 4 situations were not present. (They have to be mirrored, too.)

<img src="References/checknearfield/close across right.svg" width="19%"/><img src="References/spacer.svg" width="4.76%"/><img src="References/checknearfield/close across left.svg" width="23.8%"/>

Likewise, these will be not be checked if the previous rules were true.

And when none of the 1-distance situations are valid, we check for 2-distance.

<img src="References/0929_1.svg" width="33.3%"/>

Impair areas can now happen inside the grid, not just on the edge, and the following rules have to be applied:

<!-- page 8 -->

<img src="References/checknearfield/far straight left.svg" width="19%"/><img src="References/spacer.svg" width="4.76%"/><img src="References/checknearfield/far straight right.svg" width="19%"/>

The procedure is similar to the the straight 2-distance rule. The only difference is that we count the area starting and ending at the marked fields. In the first, the direction of the circle is left, in the second right.<br />
Besides mirroring them, we also have to rotate them both counter-clockwise and clockwise.<br />
But we do not need 12 of such rules. Taking the first, the live end cannot come from the left, because the area parity was already checked in the previous step, and now we just added 2 fields to it. It can come from the right, and then there is naturally only one field we might have to disable.<br />
Here are the representations of the two scenarios for the left side:

<img align="top" src="References/checknearfield/far side down.svg" width="19%"/><img src="References/spacer.svg" width="4.76%"/><img align="top" src="References/checknearfield/far side up.svg" width="19%"/>

Similarly to the straight rules, these will only apply if there is no wall 2 distance to the left or right. Let's construct these preconditions.

<img align="top" src="References/checknearfield/close side straight.svg" width="14.3%"/><img src="References/spacer.svg" width="4.76%"/><img align="top" src="References/checknearfield/close side mid across up.svg" width="14.3%"/><img src="References/spacer.svg" width="4.76%"/><img align="top" src="References/checknearfield/close side mid across down.svg" width="14.3%"/>

We are not finished. Did you notice the example above is not covered by these rules? We have to move the taken fields 1 and 2 steps to the side, both in straight and side direction.

<!-- page 9 -->

<img align="top" src="References/checknearfield/far mid across left.svg" width="19%"/><img src="References/spacer.svg" width="4.76%"/><img align="top" src="References/checknearfield/far mid across right.svg" width="23.8%"/>
<img src="References/spacer.svg" height="17"/>
<img align="top" src="References/checknearfield/far across left.svg" width="23.8%"/><img src="References/spacer.svg" width="4.76%"/><img align="top" src="References/checknearfield/far across right_0.svg" width="19%"/>
<img src="References/spacer.svg" height="17"/>
<img align="top" src="References/checknearfield/far side mid across up.svg" width="19%"/><img src="References/spacer.svg" width="4.76%"/><img align="top" src="References/checknearfield/far side mid across down.svg" width="19%"/>
<img src="References/spacer.svg" height="17"/>
<img align="top" src="References/checknearfield/far side across up.svg" width="19%"/>

When any of the straight 2-distance rules are present, we don't need to check the side rules or the area created with the border. This is not entirely proven, but take these 9 x 9 examples:

<img src="References/1019_8.svg" width="42.9%"/><img src="References/spacer.svg" width="4.76%"/><img align="top" src="References/1021_2.svg" width="42.9%"/>

<!-- page 10 -->

And these are the rest of the rules:

<img align="top" src="References/rules/7/Future L.svg" width="19.05%"/><img src="References/spacer.svg" width="4.76%"/><img align="top" src="References/Future L 65.svg" width="33.3%"/>

- This is what I started the 7 x 7 introduction with. I will call it Future L.

<img align="top" src="References/rules/7/Future 2 x 2 Start End.svg" width="28.57%"/><img src="References/spacer.svg" width="4.76%"/><img align="top" src="References/Future 2 x 2 Start End 450.svg" width="33.3%"/><br />
<img src="References/spacer.svg" height="23"/><br />
<img align="top" src="References/rules/7/Future 2 x 3 Start End.svg" width="14.3%"/><img src="References/spacer.svg" width="19.05%"/><img align="top" src="References/Future 2 x 3 Start End 465.svg" width="33.3%"/><br />
<img src="References/spacer.svg" height="23"/><br /><!-- page 11 -->
<img align="top" src="References/rules/7/Future 3 x 3 Start End.svg" width="23.81%"/><img src="References/spacer.svg" width="4.76%"/><img align="top" src="References/Future 3 x 3 Start End 1861.svg" width="33.3%"/>

- And these are the remaining size-specific rules. Future 2 x 2 Start End, Future 2 x 3 Start End and Future 3 x 3 Start End.

The program, in fast mode, can run through approximately 100 cases per second, depending on your computer speed. This enables us to discover all 7 x 7 walkthroughs, which is 111 712.<br />
It is equal to what is described in the Online Encyclopedia of Integer Series (Number of simple Hamiltonian paths connecting opposite corners of a 2n+1 x 2n+1 grid).

As the sizes grow, it will be impossible to run through all cases with one computer in a reasonable time. In order to discover the patterns, we need to run the program randomly.

Is it possible to develop an algorythm that works for all sizes? The edge-related and area-counting rules are universal, but the size-specific rules get more and more complex. Can you define them with one statement?

I have made statistics about how many random walkthroughs you can complete on different grids using the 7 x 7-specific and the universal rules before running into an error. Based on 1000 attempts, here are the results:<br />
9: 19.5<br />
11: 5.7<br />
13: 2.6<br />
15: 1.2<br />
17: 0.7<br />
19: 0.4<br />
21: 0.2

<!-- page 12 -->

To discover 9-specific patterns, I run the program keeping it left as long as the time to get to the first error is too big. After that, I will run it randomly. The first 13 826 walkthroughs are completed before we encounter a situation. It is similar to the last one we discovered on 7 x 7:

<img align="top" src="References/1007.svg" width="42.86%"/>

Let's simplify the pattern. Which will be impossible to fill?

<img align="top" src="References/1008.svg" width="42.86%"/><img src="References/spacer.svg" width="4.76%"/><img align="top" src="References/1008_1.svg" width="42.86%"/>

It is the picture on the left. Since the yellow-bordered area is impair, adding the (4,2) (4,3) (4,4) fields will be pair. We enter the area at (4,4), so we will exit at (4,3). Now we enter the 3 x 3 area in the top left corner at its side, (3,3) and will exit at (2,4). The results is two C-shapes on each side:

<!-- page 13 -->

<img align="top" src="References/1008_2.svg" width="42.86%"/>

We can define a rule by marking the following fields and counting the area from the fields in front of the main line to the right:

<img align="top" src="References/rules/9_old/Future 3 x 3 Start End 9.svg" width="28.57%"/>

Start_1 field is (4,3) and Start_2 field is (4,4) in the actual example. End field is (4,2). Direction of the circle: right (counter-clockwise). If the area is pair, we cannot step straight.

When generating code from the drawing, we have to check on which side the enclosed area was created. Here, we want it to be on the right side, so there are two cases to look at:
- The taken or border field beyond the end field is a taken field. In this case, if the field to its left is taken, its index must be lower. If the field to the right is taken, its index must be higher.
- It is the border. Add together the x- and y-coordinates to get a value. A higher value is closer to the end corner. Here, we compare the border field straight ahead and on its left, and we want the first-mentioned to be the smaller.

<!-- page 14 -->

I have applied this rule rotated clockwise (besides mirroring it, of course), so that the live end can both come from the bottom and the right. But it can also come from the left in this example:

<img align="top" src="References/1010_2.svg" width="42.86%"/>

This will probably be another rule, because in this case it is not necessary to have an empty 3 x 3 field on the left.

Now let's run the program further up to number 13 992:<!-- (from stepping back + 142 (with first rule disabled) / 158 = 13 984) why? -->

<img align="top" src="References/1010_4_error.svg" width="42.86%"/><img src="References/spacer.svg" width="4.76%"/><img align="top" src="References/1010_4.svg" width="42.86%"/>

It is also just like the 7 x 7 rule, just with the extension of the area on the opposite side of the future line ends. But we can't simply remove the two taken fields on that side, because the line might continue in that direction, as it is the case here:

<!-- page 15 -->

<img align="top" src="References/1013.svg" width="33.3%"/>

It would be a mistake to disable the right field.

So we need to check if an enclosed has been created on that side, but counting the area is unnecessary. Nevertheless, we can represent the rule this way, setting the circle direction to right:

<img align="top" src="References/rules/9_old/Future 2 x 2 Start End 9.svg" width="23.8%"/>

The code generator will examine if the count area start and end fields are 1 or 2 distance apart. In the first case, it will only determine in which direction the taken field straight ahead is going to, and if it is right, the forbidden field will take effect.<br />
You may ask, why that field is "taken", not "taken or border". From what I found through some examples, if that field is border, the enclosed area on the right is impair, so the line cannot step in the other direction anyway. But it needs further examination.

<!-- page 16 -->

The next error, at 14 004 has something to with how I defined the universal rules of approaching an older section of the line, it needs to be reworked in light of the C-shape the main line can create with the border.

<img align="top" src="References/1013_1.svg" width="42.86%"/>

We need to take a few steps back, and then we can create the rule. It is similar to the universal 2-distance rule on the side, it just checks the field 2 behind and 1 to the side too. Even though the area counted is pair, now stepping to the right is disabled.

<img align="top" src="References/rules/9_old/Future 2 x 3 Start End 9.svg" width="19%"/><img src="References/spacer.svg" width="4.76%"/><img align="top" src="References/1013_2.svg" width="42.86%"/>

<!-- page 17 -->

At 55 298, we get this:

<img align="top" src="References/1022.svg" width="42.86%"/>

Let's analyze it! A double C-shape is created, because the line occupied the A field, and out of the B, C and D fields it exited the right-side area at C. It means, the area enclosed by the marked fields is pair. In this case, we shouldn't step right and the rule will therefore look like:

<img align="top" src="References/Double C-Shape orig.svg" width="19%"/><img src="References/spacer.svg" width="4.76%"/><img align="top" src="References/1022_1.svg" width="42.86%"/>

But what if from the A position, we step upwards in another situation?<br />
Compare these two on 11 x 11:

<!-- page 18 -->

<img align="top" src="References/1022_2.svg" width="52.4%"/><br />
<img src="References/spacer.svg" height="23"/><br />
<img align="top" src="References/1022_3.svg" width="52.4%"/>

If the area we started with is pair, then the other will be impair. We can only enter the area at the light-gray field and will exit at A. From there we must go through B, C and D, and then a double C-shape is again created.

<!-- page 19 -->

One certain situation reveals the incorrectness of the 7-rules when it comes to a 9-grid. In the following example, when I apply a rule rotated, it will disable a field that would otherwise be viable.

<img align="top" src="References/Future 2 x 2 Start End rotated.svg" width="19%"/><img src="References/spacer.svg" width="4.76%"/><img align="top" src="References/1027.svg" width="42.86%"/>

Rotating was not necessary to start with on 7 x 7, because no such situation occurred.

We can see that defining a rule with future line starts and ends does not tell us on which side the future line was created. That is the side that contains the enclosed area. We need to therefore replace such rules with area counting, which we actually already did, with the exception of Future L. Here the future line couldn't have been created on the other side, because that's the side the live end is at right now. And area counting is not always possible, like in this situation:

<!-- page 20 -->

<img align="top" src="References/1031.svg" width="61.9%"/>

<!-- page 21 -->

As we run the program further, we will discover this at 227 200:

<img align="top" src="References/227200.svg" width="42.86%"/>

Intuitively, we can draw up the square, and let's mark the exit as well. There can be loops on the upper, lower and right side, they have no importance when tracing it back to the live end. There is only one way to go through.

<img align="top" src="References/Square 4 x 2 orig.svg" width="23.8%"/><img src="References/spacer.svg" width="4.76%"/><img align="top" src="References/227200_1.svg" width="42.86%"/>

<!-- page 22 -->

233 810 will look like:

<img align="top" src="References/233810.svg" width="42.86%"/>

Once we step to A, it is unavoidable to get to B before entering the outlined area. It is because we can only reach B from the left or the bottom.<br />
The area is impair, therefore we cannot complete it starting in C and ending in D.<br />
As with many of the previous rules, the C-shape created with the border is to blame and therefore we need to represent it.

<img align="top" src="References/rules/9_old/Count Area Across Border C.svg" width="19%"/><img src="References/spacer.svg" width="4.76%"/><img align="top" src="References/233810_1.svg" width="42.86%"/>

<!-- page 23 -->

234 256 has at first sight something to do with future lines.

<img align="top" src="References/234256.svg" width="42.86%"/>

But it is more than that. Notice that enclosed areas has been created on both sides simultaneously. Because of the universal rules for approaching an older section of the line, now we have no option to move. The areas can be filled individually, but we cannot step to left and right at the same time.<br />
We have to create 2-distance rules, which take both sides into account.

<img align="top" src="References/checknearfield/2 far mid across across.svg" width="28.6%"/><img src="References/spacer.svg" width="4.76%"/><img align="top" src="References/checknearfield/2 far side mid across across down.svg" width="19%"/><img src="References/spacer.svg" width="4.76%"/><img align="top" src="References/checknearfield/2 far side mid across across up.svg" width="19%"/>

These are just a few of the possible combinations.<br />
Any of the far straight rules (straight, mid across and across as I call them, depending on the horizontal distance of the obstacle) on the left side can be combined with any of those on the right side when the enclosed area is going to the same direction - left for left side and right for right side.<br />
And the same is true when the pattern is rotated to the left or right side.<br />
As far as porgramming concerned, it just needed a rework of the universal rules, we didn't need to make completely new ones.

<!-- page 24 -->

At 349 215, we find this:

<img align="top" src="References/349215.svg" width="42.86%"/>

Though a double C-shape has been created in backwards direction, it indicates that the area on the right cannot be filled either.
We have made a similar rule previously. Now we need to simplify it.

<img align="top" src="References/Double C-Shape orig.svg" width="19%"/><img src="References/spacer.svg" width="4.76%"/><img align="top" src="References/rules/9_old/Double C-Shape.svg" width="14.3%"/>

The area now has to be impair for the right direction to be forbidden. Essentially, we just added the three extra fields to the pair area.

<!-- page 25 -->

478 361 is similar to what we have seen before, only now there is a 2-wide path to exit the area:

<img align="top" src="References/478361.svg" width="42.86%"/>

We have to mark where the area has been created in another way.

<img align="top" src="References/Square 4 x 2 orig.svg" width="23.8%"/><img src="References/spacer.svg" width="4.76%"/><img align="top" src="References/rules/9_old/Square 4 x 2.svg" width="19%"/>

The taken field in the upper right corner is now checked for direction, but it is not enough. It can go upwards, and the exit of the area can still be on the bottom edge, just look at the example and imagine the live end was at A with the pattern already drawn. (On 11 x 11, it is possible to draw it.)<br />
In order to establish an enclosed area, we must not encounter the bottom-right corner of the grid when walking along the edge of it.

<!-- page 26 -->

626 071 is:

<img align="top" src="References/626071.svg" width="42.86%"/><img src="References/spacer.svg" width="4.76%"/><img align="top" src="References/626071_1.svg" width="42.86%"/>

With the marked area being pair, if we enter the area by stepping left, we will exit at A. But we can only get there from B; if we entered from the top, nothing would fill B, and we cannot enter and exit it after we left the area - subtracting 1 from the area would make it impair, so then we couldn't have exited at A.<br />
The taken field C creates a C-shape, which we need to step into from B.<br />
The universal far across rule have to be extended. By default, we disable the option to step straight or right if the counted area is impair. When it is pair, we need to disable the left field.

<img align="bottom" src="References/checknearfield/far across left.svg" width="23.8%"/><img src="References/spacer.svg" width="4.76%"/><img align="bottom" src="References/checknearfield/far across left end C.svg" width="23.8%"/><br />
<img src="References/spacer.svg" height="23"/><br />
<img align="bottom" src="References/checknearfield/far side across up.svg" width="19%"/><img src="References/spacer.svg" width="4.76%"/><img align="bottom" src="References/checknearfield/far side across up end C.svg" width="23.8%"/>

<!-- page 27 -->

The same concept we encounter at 635 301, only the C-shape is created when we enter an area, on the other side of it.

<img align="top" src="References/635301.svg" width="42.86%"/><img src="References/spacer.svg" width="4.76%"/><img align="top" src="References/635301_1.svg" width="42.86%"/>

We have seen this in the third 9 x 9 rule. There the taken field next to the exit was in middle across position, and now it is across. And we also need to think about an obstacle straight ahead. Here are the original universal rules and their modifications.<br />
Straight, circle direction left:

<img src="References/checknearfield/far straight left.svg" width="19%"/><img src="References/spacer.svg" width="4.76%"/><img src="References/checknearfield/far straight left start C.svg" width="23.8%"/><br />
<img src="References/spacer.svg" height="23"/><br />
<img src="References/checknearfield/far mid across left.svg" width="19%"/><img src="References/spacer.svg" width="4.76%"/><img src="References/checknearfield/far mid across left start C.svg" width="23.8%"/><br />
<img src="References/spacer.svg" height="23"/><br />
<img src="References/checknearfield/far across left.svg" width="23.8%"/><img src="References/spacer.svg" width="4.76%"/><img src="References/checknearfield/far across left start C.svg" width="28.6%"/>

<!-- page 28 -->

Circle direction right:

<img src="References/checknearfield/far straight right.svg" width="19%"/><img src="References/spacer.svg" width="4.76%"/><img src="References/checknearfield/far straight right start C.svg" width="23.8%"/><br />
<img src="References/spacer.svg" height="23"/><br />
<img src="References/checknearfield/far mid across right.svg" width="23.8%"/><img src="References/spacer.svg" width="4.76%"/><img src="References/checknearfield/far mid across right start C.svg" width="23.8%"/><br />
<img src="References/spacer.svg" height="23"/><br />
<img src="References/checknearfield/far across right.svg" width="28.57%"/><img src="References/spacer.svg" width="4.76%"/><img src="References/checknearfield/far across right start C.svg" width="28.57%"/>

Side, with taken fields above and below:

<img src="References/checknearfield/far side up.svg" align="top" width="19%"/><img src="References/spacer.svg" width="4.76%"/><img src="References/checknearfield/far side up start C.svg" align="top" width="19%"/><br />
<img src="References/spacer.svg" height="23"/><br />
<img src="References/checknearfield/far side down.svg" align="top" width="19%"/><img src="References/spacer.svg" width="4.76%"/><img src="References/checknearfield/far side down start C.svg" align="top" width="19%"/><br />
<img src="References/spacer.svg" height="23"/><br />

<!-- page 29 -->

<img src="References/checknearfield/far side mid across up.svg" align="top" width="19%"/><img src="References/spacer.svg" width="4.76%"/><img src="References/checknearfield/far side mid across up start C.svg" align="top" width="19%"/><br />
<img src="References/spacer.svg" height="23"/><br />
<img src="References/checknearfield/far side mid across down.svg" align="top" width="19%"/><img src="References/spacer.svg" width="4.76%"/><img src="References/checknearfield/far side mid across down start C.svg" align="top" width="19%"/><br />
<img src="References/spacer.svg" height="23"/><br />
<img src="References/checknearfield/far side across up.svg" align="top" width="19%"/><img src="References/spacer.svg" width="4.76%"/><img src="References/checknearfield/far side across up start C.svg" align="top" width="19%"/><br />
<img src="References/spacer.svg" height="23"/><br />
<img src="References/checknearfield/far side across down.svg" align="bottom" width="19%"/><img src="References/spacer.svg" width="4.76%"/><img src="References/checknearfield/far side across down start C.svg" align="bottom" width="19%"/><br />

I have made some changes by adding some empty fields in side positions, so they are the same as the straight rules, just rotated.<br />
Also, I have added the side across down rule and changed the straight across rule accordingly. Not only fields next to each other can define an area, they can be across too. In that case, if the area originally was marked impair, now it has to be pair.<br />
Notice that in side rules, when the taken field that would create the C-shape is below the obstacle creating the area, it can be a border field too. We have seen an example of that previously.

<!-- page 30 -->

Now what if both the start and end C-conditions are true? We can construct this on 13 x 13:

<img align="top" src="References/1119.svg" width="61.9%"/>

Several walkthrough attempts will leave you thinking why you cannot fill the area once obstacle responsible for the start C-shape is created (A). The area enclosed by A, B and C is pair. So when you enter it at A or B (obviously C is not a possibility), in order to exit at C, you need to leave out an impair amount of fields from the area. In case of entering at B, you cannot leave out A, but when you enter at A, you can leave out B, and no more than that. Now the area will be impair.<br />
The minimal area would be stepping left from A, left again, up and up to get to C. You have covered 5 fields.<br />
In order to make a walkthroughable area, you would need to extend it by pairs of fields next to each other, like D and E. One will be filled at a pair amount of steps, the other at an impair amount.<br />

<!-- page 31 -->

Let's mark the original example as a checkerboard.

<img align="top" src="References/1119_2.svg" width="61.9%"/>

We enter at a black field and exit at black too, so the number of black fields should be one more than the number of white fields.
Here there are 14 black fields and 15 white. That's why the area cannot be filled. The up and right directions need to be disabled, so we can only step left.

This is the rule representation. The reddish arealine now means the arealine is impair, and we know that the entry and exit points are the arealine start and end fields.

<img align="top" src="References/rules/13/Across 3 impair determined.svg" width="28.6%"/>


<!-- page 32 -->

And now the walkthrough is possible.

<img align="top" src="References/1119_3.svg" width="61.9%"/>

Continuing the 9 x 9 program, we get this at 641 019:

<img align="top" src="References/641019.svg" width="42.86%"/><img src="References/spacer.svg" width="4.76%"/><img align="top" src="References/641019_1.svg" width="42.86%"/>

If you enter the pair area straight ahead, you will exit at A, and you need to turn towards B because of the C-shape. Now you cannot go in and out of the area enclosed by C, and the situation would be the same if that obstacle was in D. 

<!-- page 33 -->

To mark the two areas, each one has to be given a directional obstacle next to the count area end field. In this case, it represents a taken field, but we don't go wrong if we include the border as well.

<img align="top" src="References/rules/9/Double Area C-Shape.svg" width="23.8%"/>

And with this marking system, we can correct the rules prevously made. All rules featuring future line start and end fields have to be rewritten to start with.<br />
So we get the 2-distance across rules, the straight 3-distance rule to prevent a double C-shape, and the square constellation with 3 areas. All of them are rotated clockwise or counter-clockwise.

<img align="top" src="References/rules/9/Count Area 2 Across.svg" width="23.8%"/><img src="References/spacer.svg" width="4.76%"/><img align="top" src="References/rules/9/Count Area 2 Across C.svg" width="28.57%"/><img src="References/spacer.svg" width="4.76%"/><img align="top" src="References/rules/9/Double C-Shape.svg" width="14.3%"/><img src="References/spacer.svg" width="4.76%"/><img align="top" src="References/rules/9_old/Square 4 x 2_2.svg" width="19%"/>

Let's return to the last example and make a modification:

<img align="top" src="References/641019_2.svg" width="42.86%"/>

<!-- page 34 -->

The field previously marked with B is now empty. But we still need to step in that direction, due to the area enclosed by A, which obstacle could as well be in B.<br />
The rule will be now symmetrical. It is similar to the square obstacle pattern.

<img align="top" src="References/rules/9/Triple Area.svg" width="23.8%"/>

The same concept we encounter at 725 325. We have seen this prevously, just with C-shape, not an area.

<img align="top" src="References/725325_1.svg" width="42.86%"/>

The rule will now look like this:

<img align="top" src="References/rules/9/Straight Across End Area.svg" width="19%"/>

<!-- page 35 -->

740 039 is a slight modification.

<img align="top" src="References/740039.svg" width="42.86%"/><img src="References/spacer.svg" width="4.76%"/><img align="top" src="References/740039_1.svg" width="42.86%"/>

The only difference is, that the obstacle is 3-distance away. With the area being impair, if we enter at A, we must exit at C.<br />
What if we omit D from the area? Then the area will be pair, so we must exit at B, and the only way to get there is from C. And if D is included, we can only step to C from there. Either way, we step away from the area beyond D, so the rule will be:

<img align="top" src="References/rules/9/Straight Across 3 End Area.svg" width="19%"/>

<!-- page 36 -->

811 808:

<img align="top" src="References/811808.svg" width="42.86%"/><img src="References/spacer.svg" width="4.76%"/><img align="top" src="References/811808_1.svg" width="42.86%"/>

Recognize it is a variation of the square obstacle pattern where instead of an area, there is a C-shape at the rule's upper edge. 

<img align="top" src="References/rules/9/Square 4 x 2 C-Shape.svg" width="23.8%"/>

1 261 580:

<img align="top" src="References/1261580.svg" width="42.86%"/><img src="References/spacer.svg" width="4.76%"/><img align="top" src="References/1261580_1.svg" width="42.86%"/>

<!-- page 37 -->

Again, same pattern with area. The upper obstacle is now moved, but it will satisfy the previous examples too. The rule replaces the old one.

<img align="top" src="References/rules/9/Square 4 x 2 Area.svg" width="23.8%"/>

2 022 337 is getting stuck because of the stair-shaped walls that force the future line along them. Therefore, an area is created with only one field to go in and out of it. What is the solution?

<img align="top" src="References/2022337.svg" width="42.86%"/><img src="References/spacer.svg" width="4.76%"/><img align="top" src="References/2022337_1.svg" width="42.86%"/>

Though not as universal as we want it to be, this will solve this specific situation:

<img align="top" src="References/rules/9/Double Area Stair.svg" width="28.57%"/>

<!-- page 38 -->

And soon we will encounter a similar one:

<img align="top" src="References/2022773.svg" width="42.86%"/><img src="References/spacer.svg" width="4.76%"/><img align="top" src="References/2022773_1.svg" width="42.86%"/><br />
<img src="References/spacer.svg" height="23"/><br />
<img align="top" src="References/rules/9/Double Area Stair 2.svg" width="23.8%"/>

<!-- page 39 -->

On 17 x 17, we can construct a situation where the obstacle across the stair is 2 behind and 2 to right. As the table size increases, the stair-obstacle narrowing can move infinite distance away from the live end. That's why it is important to group these rules as one.

<img align="top" src="References/1218_1.svg" width="80.95%"/>

<!-- page 40 -->

We have all the tools to handle 2 034 575.

<img align="top" src="References/2034575.svg" width="42.86%"/><img src="References/spacer.svg" width="4.76%"/><img align="top" src="References/2034575_1.svg" width="42.86%"/>

It is an impair area where the number of the starting field's color is less than the other color.

<img align="top" src="References/rules/9/Mid Across 3 Impair Determined.svg" width="19%"/>

<!-- page 41 -->

Next stop is at 3 224 847.

<img align="top" src="References/3224847.svg" width="42.86%"/><img src="References/spacer.svg" width="4.76%"/><img align="top" src="References/3224847_1.svg" width="42.86%"/>

A pair area is created with the obstacle 3 distance away, so if we step into it, we will exit at the middle, but because of an area, we cannot step there.

<img align="top" src="References/rules/9/Straight Mid Across 3 End Area.svg" width="19%"/>

<!-- page 42 -->

From our experience, the area can be substituted with C-shape.

<img align="top" src="References/1219.svg" width="52.4%"/><img src="References/spacer.svg" width="4.76%"/><img align="top" src="References/rules/9/Straight Mid Across 3 End C.svg" width="9.5%"/>

<!-- page 43 -->

---

The project contains the source code for use with Visual Studio. To start the program, run OneWayLabyrinth.exe in the folder "bin/Debug/net6.0-windows".

Screenshots:

<img align="top" src="References/screenshot_main.png" width="100%"/><br />
<img src="References/spacer.svg" height="54"/><br />
<img align="top" src="References/screenshot_rules.png" width="100%"/>

---

Hotkeys:

Enter: Reload or Close error message<br />
Ctrl + S: Save path<br />
Right arrow: Step forward<br />
Left arrow: Step back<br />
Ctrl/Shift + arrows: step in direction if possible. If CapsLock is on, pressing the Ctrl or Shift keys is not necessary.<br />
Space: Run automatically / Stop automatic running
