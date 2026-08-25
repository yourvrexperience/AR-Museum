using System;
using UnityEngine;
using System.Collections;

namespace yourvrexperience.Utils
{

	public class NodePathMatrix
	{

		public const int NODE_NOT_VISITED = -1;
		public const int NODE_VISITED = 1;

		public int X;
		public int Y;
		public int Z;
		public int DirectionInitial;
		public int HasBeenVisited;
		public float ValueSearch;
		public int PreviousCell;

		public NodePathMatrix()
		{
			Reset();
		}

		public void Reset()
		{
			X = -1;
			Y = -1;
			Z = -1;
			DirectionInitial = PathFindingController.DIRECTION_NONE;
			HasBeenVisited = NODE_NOT_VISITED;
			ValueSearch = 10000000.0f;
			PreviousCell = -1;
		}
	}

}