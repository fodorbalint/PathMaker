namespace OneWayLabyrinth
{
	public partial class Path
	{
		public bool CShape1 = false;
		public bool Future2x2StartEnd = false;
		public bool Future2x3StartEnd = false;
		public bool Future3x3StartEnd = false;
		public bool FutureL = false;
		public bool Sideback = false;
		public bool SidefrontL = false;
		public bool Sidefront = false;

		public void RunRules()
		{
			CShape1 = false;
			Future2x2StartEnd = false;
			Future2x3StartEnd = false;
			Future3x3StartEnd = false;
			FutureL = false;
			Sideback = false;
			SidefrontL = false;
			Sidefront = false;

			if (size >= 5)
			{
				// C-Shape1
				for (int i = 0; i < 2; i++)
				{
					if ((InTakenRel(2,0) || InBorderRel(2,0)) && (InTakenRel(1,-1) || InBorderRel(1,-1)) && !InTakenRel(1,0) && !InCornerRel(1,0))
					{
						CShape1 = true;
						forbidden.Add(new int[] { x + sx, y + sy });
						forbidden.Add(new int[] { x - lx, y - ly });
					}
					lx = -lx;
					ly = -ly;
				}
				lx = thisLx;
				ly = thisLy;
			}

			if (size >= 7)
			{
				// Future 2 x 2 Start End
				for (int i = 0; i < 2; i++)
				{
					if (InTakenRel(0,3) && InTakenRel(-1,2) && InTakenRel(-1,1) && !InTakenRel(0,2) && InFutureStartRel(1,0) && InFutureEndRel(3,0) && (InTakenRel(4,1) || InBorderRel(4,1)) && foundSectionStart == foundSectionEnd)
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

				// Side back
				for (int i = 0; i < 2; i++)
				{
					if (InTakenRel(2,-1) && !InTakenRel(1,0) && !InTakenRel(1,-1))
					{
						Sideback = true;
						forbidden.Add(new int[] { x + lx, y + ly });
					}
					lx = -lx;
					ly = -ly;
				}
				lx = thisLx;
				ly = thisLy;

				// Side front L
				for (int i = 0; i < 2; i++)
				{
					if (InTakenRel(2,1) && !InTakenRel(1,1) && !InTakenRel(1,0) && !InTakenRel(2,0))
					{
						SidefrontL = true;
						forbidden.Add(new int[] { x + lx, y + ly });
					}
					lx = -lx;
					ly = -ly;
				}
				lx = thisLx;
				ly = thisLy;

				// Side front
				for (int i = 0; i < 2; i++)
				{
					if (InTakenRel(2,1) && !InTakenRel(1,1) && !InTakenRel(1,0) && !InTakenRel(1,-1))
					{
						Sidefront = true;
						forbidden.Add(new int[] { x + lx, y + ly });
					}
					lx = -lx;
					ly = -ly;
				}
				lx = thisLx;
				ly = thisLy;
			}
		}
	}
}