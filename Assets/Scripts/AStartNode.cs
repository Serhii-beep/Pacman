using System;
using System.Collections;
using System.Collections.Generic;

public class AStarNode
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Cost { get; set; }
    public int Distance { get; set; }
    public int CostDistance => Cost + Distance;
    public AStarNode Parent { get; set; }
    public void SetDistance(int targetX, int targetY)
    {
        Distance = Math.Abs(targetX  - X) + Math.Abs(targetY - Y);
    }
}
