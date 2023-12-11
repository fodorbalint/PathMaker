namespace OneWayLabyrinth
{
	using System.Collections.Generic;

	public partial class Path
	{
		int directionFieldIndex = 0;
		public bool Future2x2StartEnd = false;
		public bool Future2x3StartEnd = false;
		public bool Future3x3StartEnd = false;
		public bool FutureL = false;
		public bool CountArea2AcrossC = false;
		public bool CountArea2Across = false;
		public bool DoubleAreaCShape = false;
		public bool DoubleCShape = false;
		public bool Square4x2 = false;
		public bool StraightAcross3EndArea = false;
		public bool StraightAcrossEndArea = false;
		public bool TripleArea = false;
		public bool Across3ImpairDetermined = false;

		public void RunRules()
		{
			Future2x2StartEnd = false;
			Future2x3StartEnd = false;
			Future3x3StartEnd = false;
			FutureL = false;
			CountArea2AcrossC = false;
			CountArea2Across = false;
			DoubleAreaCShape = false;
			DoubleCShape = false;
			Square4x2 = false;
			StraightAcross3EndArea = false;
			StraightAcrossEndArea = false;
			TripleArea = false;
			Across3ImpairDetermined = false;

			if (size == 5)
			{
				// C-Shape
				// Embedded in Path.cs as the absolute checking functions need it.
			}

			if (size == 7)
			{
				// Future 2 x 2 Start End
				for (int i = 0; i < 2; i++)
				{
					if ((InTakenRel(4,1) || InBorderRel(4,1)) && InFutureStartRel(1,0) && InFutureEndRel(3,0) && InTakenRel(0,3) && InTakenRel(-1,2) && InTakenRel(-1,1) && !InTakenRel(0,2) && !InBorderRel(0,2) && foundSectionStart == foundSectionEnd)
					{
						Future2x2StartEnd = true;
						forbidden.Add(new int[] { x + lx, y + ly });
					}
					lx = -lx;
					ly = -ly;
				}
				lx = thisLx;
				ly = thisLy;

				// Future 2 x 3 Start End
				for (int i = 0; i < 2; i++)
				{
					if ((InTakenRel(1,-2) || InBorderRel(1,-2)) && !InTakenRel(1,-1) && !InBorderRel(1,-1) && InFutureStartRel(0,1) && InFutureEndRel(2,1) && foundSectionStart == foundSectionEnd)
					{
						Future2x3StartEnd = true;
						forbidden.Add(new int[] { x + lx, y + ly });
					}
					lx = -lx;
					ly = -ly;
				}
				lx = thisLx;
				ly = thisLy;

				// Future 3 x 3 Start End
				for (int i = 0; i < 2; i++)
				{
					for (int j = 0; j < 2; j++)
					{
						if (!InTakenRel(3,3) && !InBorderRel(3,3) && !InTakenRel(3,1) && !InBorderRel(3,1) && (InTakenRel(3,4) || InBorderRel(3,4)) && (InTakenRel(2,4) || InBorderRel(2,4)) && (InTakenRel(1,4) || InBorderRel(1,4)) && (InTakenRel(4,3) || InBorderRel(4,3)) && (InTakenRel(4,2) || InBorderRel(4,2)) && (InTakenRel(4,1) || InBorderRel(4,1)) && InFutureStartRel(0,1) && InFutureEndRel(0,3) && !InCornerRel(3,3) && foundSectionStart == foundSectionEnd)
						{
							Future3x3StartEnd = true;
							forbidden.Add(new int[] { x + sx, y + sy });
						}
						int s0 = sx;
						int s1 = sy;
						sx = -lx;
						sy = -ly;
						lx = s0;
						ly = s1;
					}
					sx = thisSx;
					sy = thisSy;
					lx = -thisLx;
					ly = -thisLy;
				}
				sx = thisSx;
				sy = thisSy;
				lx = thisLx;
				ly = thisLy;

				// Future L
				for (int i = 0; i < 2; i++)
				{
					for (int j = 0; j < 2; j++)
					{
						if (InFutureStartRel(2,0) && InFutureEndRel(2,2) && foundSectionStart == foundSectionEnd)
						{
							FutureL = true;
							forbidden.Add(new int[] { x + sx, y + sy });
							forbidden.Add(new int[] { x - lx, y - ly });
						}
						int s0 = sx;
						int s1 = sy;
						sx = -lx;
						sy = -ly;
						lx = s0;
						ly = s1;
					}
					sx = thisSx;
					sy = thisSy;
					lx = -thisLx;
					ly = -thisLy;
				}
				sx = thisSx;
				sy = thisSy;
				lx = thisLx;
				ly = thisLy;
			}

			if (size >= 9)
			{
				// Count Area 2 Across C
				for (int i = 0; i < 2; i++)
				{
					for (int j = 0; j < 2; j++)
					{
						if ((InTakenRel(-2,1) || InBorderRel(-2,1)) && !InTakenRel(-1,1) && !InBorderRel(-1,1) && (InTakenRel(3,3) || InBorderRel(3,3)) && !InTakenRel(1,1) && !InBorderRel(1,1) && !InTakenRel(2,2) && !InBorderRel(2,2) && !InTakenRel(3,2) && !InBorderRel(3,2) && !InTakenRel(3,0) && !InBorderRel(3,0) && !InTakenRel(1,0) && !InBorderRel(1,0) && InTakenRel(-1,0))
						{
							bool CountArea2AcrossC_circle1 = false;
							directionFieldIndex = InTakenIndexRel(3,3);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(4,3))
								{
									int leftIndex = InTakenIndexRel(4,3);
									if (leftIndex > directionFieldIndex)
									{
										CountArea2AcrossC_circle1 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(2,3);
									if (rightIndex < directionFieldIndex)
									{
										CountArea2AcrossC_circle1 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(3,3);
								int farSideIndex = InBorderIndexRel(2,3);
								if (farSideIndex > directionFieldIndex)
								{
									CountArea2AcrossC_circle1 = true;
								}
							}
							
							if (CountArea2AcrossC_circle1 && CountAreaRel(1,1,2,2,new List<int[]> {new int[] {2,1}},i==0?true:!true,1))
							{
								CountArea2AcrossC = true;
								forbidden.Add(new int[] { x + sx, y + sy });
							}
						}
						int s0 = sx;
						int s1 = sy;
						sx = -lx;
						sy = -ly;
						lx = s0;
						ly = s1;
					}
					sx = thisSx;
					sy = thisSy;
					lx = -thisLx;
					ly = -thisLy;
				}
				sx = thisSx;
				sy = thisSy;
				lx = thisLx;
				ly = thisLy;

				// Count Area 2 Across
				for (int i = 0; i < 2; i++)
				{
					for (int j = 0; j < 2; j++)
					{
						if ((InTakenRel(3,3) || InBorderRel(3,3)) && !InTakenRel(3,2) && !InBorderRel(3,2) && !InTakenRel(2,2) && !InBorderRel(2,2) && !InTakenRel(1,1) && !InBorderRel(1,1) && !InTakenRel(3,0) && !InBorderRel(3,0) && !InTakenRel(1,0) && !InBorderRel(1,0))
						{
							bool CountArea2Across_circle1 = false;
							directionFieldIndex = InTakenIndexRel(3,3);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(4,3))
								{
									int leftIndex = InTakenIndexRel(4,3);
									if (leftIndex > directionFieldIndex)
									{
										CountArea2Across_circle1 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(2,3);
									if (rightIndex < directionFieldIndex)
									{
										CountArea2Across_circle1 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(3,3);
								int farSideIndex = InBorderIndexRel(2,3);
								if (farSideIndex > directionFieldIndex)
								{
									CountArea2Across_circle1 = true;
								}
							}
							
							if (CountArea2Across_circle1 && CountAreaRel(1,1,2,2,new List<int[]> {new int[] {2,1}},i==0?true:!true,0))
							{
								CountArea2Across = true;
								forbidden.Add(new int[] { x + sx, y + sy });
								forbidden.Add(new int[] { x - lx, y - ly });
							}
						}
						int s0 = sx;
						int s1 = sy;
						sx = -lx;
						sy = -ly;
						lx = s0;
						ly = s1;
					}
					sx = thisSx;
					sy = thisSy;
					lx = -thisLx;
					ly = -thisLy;
				}
				sx = thisSx;
				sy = thisSy;
				lx = thisLx;
				ly = thisLy;

				// Double Area C-Shape
				for (int i = 0; i < 2; i++)
				{
					for (int j = 0; j < 2; j++)
					{
						if ((InTakenRel(-1,4) || InBorderRel(-1,4)) && (InTakenRel(-3,0) || InBorderRel(-3,0)) && !InTakenRel(0,3) && !InBorderRel(0,3) && !InTakenRel(-1,3) && !InBorderRel(-1,3) && !InTakenRel(1,3) && !InBorderRel(1,3) && !InTakenRel(0,1) && !InBorderRel(0,1) && !InTakenRel(1,1) && !InBorderRel(1,1) && !InTakenRel(-1,0) && !InBorderRel(-1,0) && !InTakenRel(-2,0) && !InBorderRel(-2,0))
						{
							bool DoubleAreaCShape_circle1 = false;
							directionFieldIndex = InTakenIndexRel(-1,4);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(0,4))
								{
									int leftIndex = InTakenIndexRel(0,4);
									if (leftIndex > directionFieldIndex)
									{
										DoubleAreaCShape_circle1 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(-2,4);
									if (rightIndex < directionFieldIndex)
									{
										DoubleAreaCShape_circle1 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(-1,4);
								int farSideIndex = InBorderIndexRel(-2,4);
								if (farSideIndex > directionFieldIndex)
								{
									DoubleAreaCShape_circle1 = true;
								}
							}
							
							bool DoubleAreaCShape_circle2 = false;
							directionFieldIndex = InTakenIndexRel(-3,0);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(-3,1))
								{
									int leftIndex = InTakenIndexRel(-3,1);
									if (leftIndex < directionFieldIndex)
									{
										DoubleAreaCShape_circle2 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(-3,-1);
									if (rightIndex > directionFieldIndex)
									{
										DoubleAreaCShape_circle2 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(-3,0);
								int farSideIndex = InBorderIndexRel(-3,1);
								if (farSideIndex > directionFieldIndex)
								{
									DoubleAreaCShape_circle2 = true;
								}
							}
							
							if (DoubleAreaCShape_circle1 && DoubleAreaCShape_circle2 && CountAreaRel(0,1,0,3,new List<int[]> {new int[] {0,2}},i==0?true:!true,0) && CountAreaRel(-1,0,-2,0,null,i==0?false:!false,0))
							{
								DoubleAreaCShape = true;
								forbidden.Add(new int[] { x + sx, y + sy });
							}
						}
						int l0 = lx;
						int l1 = ly;
						lx = -sx;
						ly = -sy;
						sx = l0;
						sy = l1;
					}
					sx = thisSx;
					sy = thisSy;
					lx = -thisLx;
					ly = -thisLy;
				}
				sx = thisSx;
				sy = thisSy;
				lx = thisLx;
				ly = thisLy;

				// Double C-Shape
				for (int i = 0; i < 2; i++)
				{
					for (int j = 0; j < 2; j++)
					{
						if (!InTakenRel(2,3) && !InBorderRel(2,3) && !InTakenRel(1,3) && !InBorderRel(1,3) && !InTakenRel(1,2) && !InBorderRel(1,2) && !InTakenRel(0,3) && !InBorderRel(0,3) && !InTakenRel(2,1) && !InBorderRel(2,1) && !InTakenRel(1,1) && !InBorderRel(1,1) && !InTakenRel(0,1) && !InBorderRel(0,1) && !InTakenRel(1,0) && !InBorderRel(1,0) && (InTakenRel(0,4) || InBorderRel(0,4)))
						{
							bool DoubleCShape_circle1 = false;
							directionFieldIndex = InTakenIndexRel(0,4);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(1,4))
								{
									int leftIndex = InTakenIndexRel(1,4);
									if (leftIndex > directionFieldIndex)
									{
										DoubleCShape_circle1 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(-1,4);
									if (rightIndex < directionFieldIndex)
									{
										DoubleCShape_circle1 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(0,4);
								int farSideIndex = InBorderIndexRel(-1,4);
								if (farSideIndex > directionFieldIndex)
								{
									DoubleCShape_circle1 = true;
								}
							}
							
							if (DoubleCShape_circle1 && CountAreaRel(1,1,1,3,new List<int[]> {new int[] {1,2}},i==0?true:!true,1))
							{
								DoubleCShape = true;
								forbidden.Add(new int[] { x + lx, y + ly });
							}
						}
						int s0 = sx;
						int s1 = sy;
						sx = -lx;
						sy = -ly;
						lx = s0;
						ly = s1;
					}
					sx = thisSx;
					sy = thisSy;
					lx = -thisLx;
					ly = -thisLy;
				}
				sx = thisSx;
				sy = thisSy;
				lx = thisLx;
				ly = thisLy;

				// Square 4 x 2
				for (int i = 0; i < 2; i++)
				{
					for (int j = 0; j < 2; j++)
					{
						if ((InTakenRel(0,3) || InBorderRel(0,3)) && (InTakenRel(-3,3) || InBorderRel(-3,3)) && (InTakenRel(-3,0) || InBorderRel(-3,0)) && !InTakenRel(0,2) && !InBorderRel(0,2) && !InTakenRel(-1,3) && !InBorderRel(-1,3) && !InTakenRel(-2,3) && !InBorderRel(-2,3) && !InTakenRel(-2,0) && !InBorderRel(-2,0) && !InTakenRel(-1,0) && !InBorderRel(-1,0))
						{
							bool Square4x2_circle1 = false;
							directionFieldIndex = InTakenIndexRel(0,3);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(1,3))
								{
									int leftIndex = InTakenIndexRel(1,3);
									if (leftIndex > directionFieldIndex)
									{
										Square4x2_circle1 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(-1,3);
									if (rightIndex < directionFieldIndex)
									{
										Square4x2_circle1 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(0,3);
								int farSideIndex = InBorderIndexRel(-1,3);
								if (farSideIndex > directionFieldIndex)
								{
									Square4x2_circle1 = true;
								}
							}
							
							bool Square4x2_circle2 = false;
							directionFieldIndex = InTakenIndexRel(-3,3);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(-3,4))
								{
									int leftIndex = InTakenIndexRel(-3,4);
									if (leftIndex > directionFieldIndex)
									{
										Square4x2_circle2 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(-3,2);
									if (rightIndex < directionFieldIndex)
									{
										Square4x2_circle2 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(-3,3);
								int farSideIndex = InBorderIndexRel(-3,2);
								if (farSideIndex > directionFieldIndex)
								{
									Square4x2_circle2 = true;
								}
							}
							
							bool Square4x2_circle3 = false;
							directionFieldIndex = InTakenIndexRel(-3,0);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(-3,1))
								{
									int leftIndex = InTakenIndexRel(-3,1);
									if (leftIndex < directionFieldIndex)
									{
										Square4x2_circle3 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(-3,-1);
									if (rightIndex > directionFieldIndex)
									{
										Square4x2_circle3 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(-3,0);
								int farSideIndex = InBorderIndexRel(-3,1);
								if (farSideIndex > directionFieldIndex)
								{
									Square4x2_circle3 = true;
								}
							}
							
							if (Square4x2_circle1 && Square4x2_circle2 && Square4x2_circle3 && CountAreaRel(0,1,0,2,null,i==0?true:!true,0) && CountAreaRel(-1,0,-2,0,null,i==0?false:!false,0))
							{
								Square4x2 = true;
								forbidden.Add(new int[] { x + sx, y + sy });
							}
						}
						int l0 = lx;
						int l1 = ly;
						lx = -sx;
						ly = -sy;
						sx = l0;
						sy = l1;
					}
					sx = thisSx;
					sy = thisSy;
					lx = -thisLx;
					ly = -thisLy;
				}
				sx = thisSx;
				sy = thisSy;
				lx = thisLx;
				ly = thisLy;

				// Straight Across 3 End Area
				for (int i = 0; i < 2; i++)
				{
					for (int j = 0; j < 2; j++)
					{
						if ((InTakenRel(2,4) || InBorderRel(2,4)) && (InTakenRel(-1,4) || InBorderRel(-1,4)) && !InTakenRel(1,3) && !InBorderRel(1,3) && !InTakenRel(1,1) && !InBorderRel(1,1) && !InTakenRel(2,3) && !InBorderRel(2,3) && !InTakenRel(2,1) && !InBorderRel(2,1) && !InTakenRel(1,4) && !InBorderRel(1,4) && !InTakenRel(0,4) && !InBorderRel(0,4) && !InTakenRel(1,0) && !InBorderRel(1,0))
						{
							bool StraightAcross3EndArea_circle1 = false;
							directionFieldIndex = InTakenIndexRel(-1,4);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(-1,5))
								{
									int leftIndex = InTakenIndexRel(-1,5);
									if (leftIndex > directionFieldIndex)
									{
										StraightAcross3EndArea_circle1 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(-1,3);
									if (rightIndex < directionFieldIndex)
									{
										StraightAcross3EndArea_circle1 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(-1,4);
								int farSideIndex = InBorderIndexRel(-1,3);
								if (farSideIndex > directionFieldIndex)
								{
									StraightAcross3EndArea_circle1 = true;
								}
							}
							
							bool StraightAcross3EndArea_circle2 = false;
							directionFieldIndex = InTakenIndexRel(2,4);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(3,4))
								{
									int leftIndex = InTakenIndexRel(3,4);
									if (leftIndex > directionFieldIndex)
									{
										StraightAcross3EndArea_circle2 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(1,4);
									if (rightIndex < directionFieldIndex)
									{
										StraightAcross3EndArea_circle2 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(2,4);
								int farSideIndex = InBorderIndexRel(1,4);
								if (farSideIndex > directionFieldIndex)
								{
									StraightAcross3EndArea_circle2 = true;
								}
							}
							
							if (StraightAcross3EndArea_circle1 && StraightAcross3EndArea_circle2 && CountAreaRel(1,1,1,3,new List<int[]> {new int[] {1,2}},i==0?true:!true,1))
							{
								StraightAcross3EndArea = true;
								forbidden.Add(new int[] { x + lx, y + ly });
							}
						}
						int s0 = sx;
						int s1 = sy;
						sx = -lx;
						sy = -ly;
						lx = s0;
						ly = s1;
					}
					sx = thisSx;
					sy = thisSy;
					lx = -thisLx;
					ly = -thisLy;
				}
				sx = thisSx;
				sy = thisSy;
				lx = thisLx;
				ly = thisLy;

				// Straight Across End Area
				for (int i = 0; i < 2; i++)
				{
					for (int j = 0; j < 2; j++)
					{
						if ((InTakenRel(2,3) || InBorderRel(2,3)) && (InTakenRel(-1,4) || InBorderRel(-1,4)) && !InTakenRel(1,3) && !InBorderRel(1,3) && !InTakenRel(1,1) && !InBorderRel(1,1) && !InTakenRel(2,1) && !InBorderRel(2,1) && !InTakenRel(0,1) && !InBorderRel(0,1))
						{
							bool StraightAcrossEndArea_circle1 = false;
							directionFieldIndex = InTakenIndexRel(-1,4);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(-1,5))
								{
									int leftIndex = InTakenIndexRel(-1,5);
									if (leftIndex > directionFieldIndex)
									{
										StraightAcrossEndArea_circle1 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(-1,3);
									if (rightIndex < directionFieldIndex)
									{
										StraightAcrossEndArea_circle1 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(-1,4);
								int farSideIndex = InBorderIndexRel(-1,3);
								if (farSideIndex > directionFieldIndex)
								{
									StraightAcrossEndArea_circle1 = true;
								}
							}
							
							bool StraightAcrossEndArea_circle2 = false;
							directionFieldIndex = InTakenIndexRel(2,3);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(3,3))
								{
									int leftIndex = InTakenIndexRel(3,3);
									if (leftIndex > directionFieldIndex)
									{
										StraightAcrossEndArea_circle2 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(1,3);
									if (rightIndex < directionFieldIndex)
									{
										StraightAcrossEndArea_circle2 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(2,3);
								int farSideIndex = InBorderIndexRel(1,3);
								if (farSideIndex > directionFieldIndex)
								{
									StraightAcrossEndArea_circle2 = true;
								}
							}
							
							if (StraightAcrossEndArea_circle1 && StraightAcrossEndArea_circle2 && CountAreaRel(1,1,1,2,null,i==0?true:!true,0))
							{
								StraightAcrossEndArea = true;
								forbidden.Add(new int[] { x + lx, y + ly });
							}
						}
						int s0 = sx;
						int s1 = sy;
						sx = -lx;
						sy = -ly;
						lx = s0;
						ly = s1;
					}
					sx = thisSx;
					sy = thisSy;
					lx = -thisLx;
					ly = -thisLy;
				}
				sx = thisSx;
				sy = thisSy;
				lx = thisLx;
				ly = thisLy;

				// Triple Area
				for (int i = 0; i < 2; i++)
				{
					for (int j = 0; j < 2; j++)
					{
						if (!InTakenRel(0,3) && !InBorderRel(0,3) && !InTakenRel(0,1) && !InBorderRel(0,1) && !InTakenRel(1,3) && !InBorderRel(1,3) && !InTakenRel(1,1) && !InBorderRel(1,1) && (InTakenRel(0,4) || InBorderRel(0,4)) && (InTakenRel(-3,0) || InBorderRel(-3,0)) && !InTakenRel(-2,0) && !InBorderRel(-2,0) && !InTakenRel(-1,0) && !InBorderRel(-1,0) && (InTakenRel(-3,4) || InBorderRel(-3,4)) && !InTakenRel(-1,4) && !InBorderRel(-1,4) && !InTakenRel(-2,4) && !InBorderRel(-2,4))
						{
							bool TripleArea_circle1 = false;
							directionFieldIndex = InTakenIndexRel(0,4);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(1,4))
								{
									int leftIndex = InTakenIndexRel(1,4);
									if (leftIndex > directionFieldIndex)
									{
										TripleArea_circle1 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(-1,4);
									if (rightIndex < directionFieldIndex)
									{
										TripleArea_circle1 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(0,4);
								int farSideIndex = InBorderIndexRel(-1,4);
								if (farSideIndex > directionFieldIndex)
								{
									TripleArea_circle1 = true;
								}
							}
							
							bool TripleArea_circle2 = false;
							directionFieldIndex = InTakenIndexRel(-3,0);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(-3,1))
								{
									int leftIndex = InTakenIndexRel(-3,1);
									if (leftIndex < directionFieldIndex)
									{
										TripleArea_circle2 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(-3,-1);
									if (rightIndex > directionFieldIndex)
									{
										TripleArea_circle2 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(-3,0);
								int farSideIndex = InBorderIndexRel(-3,1);
								if (farSideIndex > directionFieldIndex)
								{
									TripleArea_circle2 = true;
								}
							}
							
							bool TripleArea_circle3 = false;
							directionFieldIndex = InTakenIndexRel(-3,4);
							if (directionFieldIndex != -1)
							{
								if (InTakenRel(-3,5))
								{
									int leftIndex = InTakenIndexRel(-3,5);
									if (leftIndex > directionFieldIndex)
									{
										TripleArea_circle3 = true;
									}
								}
								else
								{
									int rightIndex = InTakenIndexRel(-3,3);
									if (rightIndex < directionFieldIndex)
									{
										TripleArea_circle3 = true;
									}
								}
							}
							else
							{
								directionFieldIndex = InBorderIndexRel(-3,4);
								int farSideIndex = InBorderIndexRel(-3,3);
								if (farSideIndex > directionFieldIndex)
								{
									TripleArea_circle3 = true;
								}
							}
							
							if (TripleArea_circle1 && TripleArea_circle2 && TripleArea_circle3 && CountAreaRel(0,1,0,3,new List<int[]> {new int[] {0,2}},i==0?true:!true,0) && CountAreaRel(-1,0,-2,0,null,i==0?false:!false,0))
							{
								TripleArea = true;
								forbidden.Add(new int[] { x + sx, y + sy });
							}
						}
						int l0 = lx;
						int l1 = ly;
						lx = -sx;
						ly = -sy;
						sx = l0;
						sy = l1;
					}
					sx = thisSx;
					sy = thisSy;
					lx = -thisLx;
					ly = -thisLy;
				}
				sx = thisSx;
				sy = thisSy;
				lx = thisLx;
				ly = thisLy;
			}

			if (size >= 13)
			{
				// Across 3 Impair Determined
				for (int i = 0; i < 2; i++)
				{
					if ((InTakenRel(4,4) || InBorderRel(4,4)) && !InTakenRel(1,0) && !InBorderRel(1,0) && !InTakenRel(1,1) && !InBorderRel(1,1) && !InTakenRel(3,1) && !InBorderRel(3,1) && !InTakenRel(3,0) && !InBorderRel(3,0) && !InTakenRel(4,1) && !InBorderRel(4,1) && !InTakenRel(3,3) && !InBorderRel(3,3) && !InTakenRel(4,3) && !InBorderRel(4,3))
					{
						bool Across3ImpairDetermined_circle1 = false;
						directionFieldIndex = InTakenIndexRel(4,4);
						if (directionFieldIndex != -1)
						{
							if (InTakenRel(5,4))
							{
								int leftIndex = InTakenIndexRel(5,4);
								if (leftIndex > directionFieldIndex)
								{
									Across3ImpairDetermined_circle1 = true;
								}
							}
							else
							{
								int rightIndex = InTakenIndexRel(3,4);
								if (rightIndex < directionFieldIndex)
								{
									Across3ImpairDetermined_circle1 = true;
								}
							}
						}
						else
						{
							directionFieldIndex = InBorderIndexRel(4,4);
							int farSideIndex = InBorderIndexRel(3,4);
							if (farSideIndex > directionFieldIndex)
							{
								Across3ImpairDetermined_circle1 = true;
							}
						}
						
						if (Across3ImpairDetermined_circle1 && CountAreaRel(1,1,3,3,new List<int[]> {new int[] {3,2},new int[] {3,1},new int[] {2,1}},i==0?true:!true,2))
						{
							Across3ImpairDetermined = true;
							forbidden.Add(new int[] { x - lx, y - ly });
							forbidden.Add(new int[] { x + sx, y + sy });
						}
					}
					lx = -lx;
					ly = -ly;
				}
				lx = thisLx;
				ly = thisLy;
			}
			T("Future2x2StartEnd: " + Future2x2StartEnd + "\n" + "Future2x3StartEnd: " + Future2x3StartEnd + "\n" + "Future3x3StartEnd: " + Future3x3StartEnd + "\n" + "FutureL: " + FutureL + "\n" + "CountArea2AcrossC: " + CountArea2AcrossC + "\n" + "CountArea2Across: " + CountArea2Across + "\n" + "DoubleAreaCShape: " + DoubleAreaCShape + "\n" + "DoubleCShape: " + DoubleCShape + "\n" + "Square4x2: " + Square4x2 + "\n" + "StraightAcross3EndArea: " + StraightAcross3EndArea + "\n" + "StraightAcrossEndArea: " + StraightAcrossEndArea + "\n" + "TripleArea: " + TripleArea + "\n" + "Across3ImpairDetermined: " + Across3ImpairDetermined);
		}
	}
}