namespace OneWayLabyrinth
{
	public partial class Path
	{
		public bool Future2x2StartEnd = false;
		public bool Future2x3StartEnd = false;
		public bool Future3x3StartEnd = false;
		public bool FutureL = false;
		public bool CountArea3x3 = false;
		public bool FarMidAcrossCShape = false;
		public bool Future2x2StartEnd9 = false;

		public void RunRules()
		{
			Future2x2StartEnd = false;
			Future2x3StartEnd = false;
			Future3x3StartEnd = false;
			FutureL = false;
			CountArea3x3 = false;
			FarMidAcrossCShape = false;
			Future2x2StartEnd9 = false;

			if (size >= 5)
			{
				// C-Shape
				// Embedded in Path.cs as the absolute checking functions need it.
			}

			if (size >= 7)
			{
				// Future 2 x 2 Start End
				for (int i = 0; i < 2; i++)
				{
					for (int j = 0; j < 2; j++)
					{
						if (InTakenRel(0,3) && InTakenRel(-1,2) && InTakenRel(-1,1) && !InTakenRel(0,2) && InFutureStartRel(1,0) && InFutureEndRel(3,0) && (InTakenRel(4,1) || InBorderRel(4,1)) && foundSectionStart == foundSectionEnd)
						{
							Future2x2StartEnd = true;
							forbidden.Add(new int[] { x + lx, y + ly });
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

				// Future 2 x 3 Start End
				for (int i = 0; i < 2; i++)
				{
					if ((InTakenRel(1,-2) || InBorderRel(1,-2)) && !InTakenRel(1,-1) && InFutureStartRel(0,1) && InFutureEndRel(2,1) && foundSectionStart == foundSectionEnd)
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
						if (!InTakenRel(3,3) && !InTakenRel(3,1) && (InTakenRel(4,3) || InBorderRel(4,3)) && (InTakenRel(4,2) || InBorderRel(4,2)) && (InTakenRel(4,1) || InBorderRel(4,1)) && (InTakenRel(3,4) || InBorderRel(3,4)) && (InTakenRel(2,4) || InBorderRel(2,4)) && (InTakenRel(1,4) || InBorderRel(1,4)) && InFutureStartRel(0,1) && InFutureEndRel(0,3) && !InCornerRel(3,3) && foundSectionStart == foundSectionEnd)
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
				// Count Area 3 x 3
				for (int i = 0; i < 2; i++)
				{
					for (int j = 0; j < 2; j++)
					{
						if ((InTakenRel(3,4) || InBorderRel(3,4)) && (InTakenRel(2,4) || InBorderRel(2,4)) && (InTakenRel(1,4) || InBorderRel(1,4)) && (InTakenRel(4,3) || InBorderRel(4,3)) && (InTakenRel(4,2) || InBorderRel(4,2)) && (InTakenRel(4,1) || InBorderRel(4,1)) && !InTakenRel(3,3) && !InTakenRel(3,1) && !InTakenRel(0,3) && !InTakenRel(0,1) && !InTakenRel(-1,3) && !InTakenRel(-1,1) && !InTakenRel(-2,2) && (InTakenRel(0,4) || InBorderRel(0,4)) && !InCornerRel(3,3))
						{
							int middleIndex = InTakenIndexRel(0,4);
							if (middleIndex != -1)
							{
								if (InTakenRel(1,4))
								{
									int sideIndex = InTakenIndexRel(1,4);
									if (sideIndex < middleIndex)
									{
										circleDirectionLeft = (i == 0) ? false : true;
										if (CountAreaRel(0,1, 0,3))
										{
											CountArea3x3 = true;
											forbidden.Add(new int[] { x + sx, y + sy });
										}
									}
								}
								else
								{
									int sideIndex = InTakenIndexRel(-1,4);
									if (sideIndex > middleIndex)
									{
										circleDirectionLeft = (i == 0) ? false : true;
										if (CountAreaRel(0,1, 0,3))
										{
											CountArea3x3 = true;
											forbidden.Add(new int[] { x + sx, y + sy });
										}
									}
								}
							}
							else
							{
								middleIndex = InBorderIndexRel(0,4);
								int farSideIndex = InBorderIndexRel(1,4);
								if (farSideIndex > middleIndex)
								{
									circleDirectionLeft = (i == 0) ? false : true;
									if (CountAreaRel(0,1, 0,3))
									{
										CountArea3x3 = true;
										forbidden.Add(new int[] { x + sx, y + sy });
									}
								}
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

				// Far Mid Across C-Shape
				for (int i = 0; i < 2; i++)
				{
					if ((InTakenRel(1,-2) || InBorderRel(1,-2)) && InTakenRel(3,1) && !InTakenRel(2,1))
					{
						int middleIndex = InTakenIndexRel(3,1);
						if (middleIndex != -1)
						{
							if (InTakenRel(3,0))
							{
								int sideIndex = InTakenIndexRel(3,0);
								if (sideIndex < middleIndex)
								{
									FarMidAcrossCShape = true;
									forbidden.Add(new int[] { x + lx, y + ly });
								}
							}
							else
							{
								int sideIndex = InTakenIndexRel(3,2);
								if (sideIndex > middleIndex)
								{
									FarMidAcrossCShape = true;
									forbidden.Add(new int[] { x + lx, y + ly });
								}
							}
						}
						else
						{
							middleIndex = InBorderIndexRel(3,1);
							int farSideIndex = InBorderIndexRel(3,0);
							if (farSideIndex > middleIndex)
							{
								FarMidAcrossCShape = true;
								forbidden.Add(new int[] { x + lx, y + ly });
							}
						}
					}
					lx = -lx;
					ly = -ly;
				}
				lx = thisLx;
				ly = thisLy;

				// Future 2 x 2 Start End 9
				for (int i = 0; i < 2; i++)
				{
					for (int j = 0; j < 2; j++)
					{
						if (InTakenRel(0,3) && (InTakenRel(4,1) || InBorderRel(4,1)) && InFutureStartRel(1,0) && InFutureEndRel(3,0) && !InTakenRel(0,2) && foundSectionStart == foundSectionEnd)
						{
							int middleIndex = InTakenIndexRel(0,3);
							if (middleIndex != -1)
							{
								if (InTakenRel(1,3))
								{
									int sideIndex = InTakenIndexRel(1,3);
									if (sideIndex < middleIndex)
									{
										Future2x2StartEnd9 = true;
										forbidden.Add(new int[] { x + lx, y + ly });
									}
								}
								else
								{
									int sideIndex = InTakenIndexRel(-1,3);
									if (sideIndex > middleIndex)
									{
										Future2x2StartEnd9 = true;
										forbidden.Add(new int[] { x + lx, y + ly });
									}
								}
							}
							else
							{
								middleIndex = InBorderIndexRel(0,3);
								int farSideIndex = InBorderIndexRel(1,3);
								if (farSideIndex > middleIndex)
								{
									Future2x2StartEnd9 = true;
									forbidden.Add(new int[] { x + lx, y + ly });
								}
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
			}
		}
	}
}