using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace yourvrexperience.Utils
{

    public class PathFindingInstance : MonoBehaviour
	{
        private int _cols;                     //! Cols of the matrix
		private int _rows;                     //! Rows of the matrix
		private int _layers;                   //! Height of the matrix
		private int _totalCells;               //! Total number of cells
		private float _cellSize;               //! Size of the cell
		private float _xIni;                   //! Initial shift X
		private float _yIni;                   //! Initial shift Y
		private float _zIni;                   //! Initial shift Z
        private float _waypointHeight = 2;

        private int _sizeMatrix;
		private int _numCellsGenerated;

		private int[] _floor;

		private int[][] _cells;                //! List of cells to apply the pathfinding

		private List<NodePathMatrix> _matrixAI;
		private List<GameObject> _dotPaths = new List<GameObject>();

        private PrecalculatedData _vectorPaths;

        private float _pathCheckHeight = 1;

        public int Cols
		{
			get { return _cols; }
			set { _cols = value; }
		}
		public int Rows
		{
			get { return _rows; }
			set { _rows = value; }
		}
		public int Height
		{
			get { return _layers; }
			set { _layers = value; }
		}
		public int TotalCells
		{
			get { return _totalCells; }
			set { _totalCells = value; }
		}
		public float CellSize
		{
			get { return _cellSize; }
			set { _cellSize = value; }
		}
		public float xIni
		{
			get { return _xIni; }
			set { _xIni = value; }
		}
		public float yIni
		{
			get { return _yIni; }
			set { _yIni = value; }
		}
		public float zIni
		{
			get { return _zIni; }
			set { _zIni = value; }
		}
        public float WaypointHeight
        {
            get { return _waypointHeight; }
            set { _waypointHeight = value; }
        }
        public float PathCheckHeight
        {
            get { return _pathCheckHeight; }
            set { _pathCheckHeight = value; }
        }

        public void Initialize()
		{
		}

		public void ClearDotPaths()
		{
			foreach (GameObject dot in _dotPaths)
			{
				Destroy(dot);
			}
			_dotPaths.Clear();
		}

		public void ClearMemoryAllocated()
		{
			if (_matrixAI != null) _matrixAI.Clear();
		}

		public void Destroy()
		{
			ClearDotPaths();
		}

		private void CreateDotPath(Vector3 position, int totalDots)
		{
			if (PathFindingController.Instance.DebugPathPoints)
			{
				GameObject newdot = (GameObject)Instantiate(PathFindingController.Instance.DotReferenceWay, position, new Quaternion());
                float cellSize = (_cellSize / 2);
                newdot.transform.localScale = new Vector3(cellSize, cellSize, cellSize);
				_dotPaths.Add(newdot);
			}
		}

        public GameObject CreateSingleDot(Vector3 position, float size, int type)
        {
            GameObject prefabDot = PathFindingController.Instance.DotReferenceWay;
            switch (type)
            {
                case 1:
                    prefabDot = PathFindingController.Instance.DotReference;
                    break;

                case 2:
                    prefabDot = PathFindingController.Instance.DotReferenceEmtpy;
                    break;

                default:
                    prefabDot = PathFindingController.Instance.DotReferenceWay;
                    break;
            }

            GameObject newdot = (GameObject)Instantiate(prefabDot, position, new Quaternion());
            newdot.transform.localScale = new Vector3(size, size, size);
            return newdot;
        }

        private void CreateDot(Vector3 position, bool enableRenderer = true, float scaleSize = 3)
        {
            GameObject newdot = (GameObject)Instantiate(PathFindingController.Instance.DotReferenceWay, position, new Quaternion());
            if (!enableRenderer)
            {
                newdot.GetComponent<Renderer>().enabled = true;
            }
            float cellSize = (_cellSize / scaleSize);
            newdot.transform.localScale = new Vector3(cellSize, cellSize, cellSize);
            _dotPaths.Add(newdot);
        }

        public bool CheckBlockedPath(Vector3 origin, Vector3 target, float dotSize = 3, params string[] masksToIgnore)
        {
            return (RaycastingTools.GetCollidedObjectBySegmentTargetIgnore(target, origin, masksToIgnore));
        }

        public void AllocateMemoryMatrix(int cols,
										int rows,
										int layers,
										float cellSize,
										float xIni,
										float yIni,
										float zIni,
										int[][][] initContent = null)
		{
			_cols = rows;
			_rows = cols;
			_layers = layers;
			_totalCells = _cols * _rows * _layers;
			_cellSize = cellSize;
			_xIni = xIni;
			_yIni = yIni;
			_zIni = zIni;

			// INIT
			_cells = new int[_layers][];
			for (int z = 0; z < _layers; z++)
			{
				_cells[z] = new int[_totalCells];
				for (int x = 0; x < _rows; x++)
				{
					for (int y = 0; y < _cols; y++)
					{
						_cells[z][(x * _cols) + y] = PathFindingController.CELL_EMPTY;
					}
				}
			}

            // COLLISION
            if (initContent != null)
            {
                for (int z = 0; z < _layers; z++)
                {
                    for (int x = 0; x < _rows; x++)
                    {
                        for (int y = 0; y < _cols; y++)
                        {
                            int cellContent = initContent[z][x][y];
                            _cells[z][(x * _cols) + y] = ((cellContent != 0) ? PathFindingController.CELL_COLLISION : PathFindingController.CELL_EMPTY);
                        }
                    }
                }
            }

            _matrixAI = new List<NodePathMatrix>();
			for (int i = 0; i < _totalCells; i++)
			{
				_matrixAI.Add(new NodePathMatrix());
			}

			if (PathFindingController.DEBUG_MATRIX_CONSTRUCTION)
			{
                if (initContent == null)
                {
                    RenderDebugMatrixConstruction(0);
                }				
			}
		}


		public void SetContentCollisionCell(Vector3 posMatrix, int content)
		{
			_cells[(int)posMatrix.z][(int)((posMatrix.x * _cols) + posMatrix.y)] = content;
		}

		public void SetContentCollisionFloor(Vector3 posMatrix, int content)
		{
			_floor[(int)((posMatrix.x * _cols) + posMatrix.y)] = content;
        }

        private List<GameObject> m_temporalDots = new List<GameObject>();

        public void DestroyDebugMatrixConstruction()
        {
            if (m_temporalDots.Count > 0)
            {
                for (int i = 0; i < m_temporalDots.Count; i++)
                {
                    GameObject.DestroyImmediate(m_temporalDots[i]);
                }
            }
            m_temporalDots.Clear();
        }

        public void RenderDebugMatrixConstruction(int layerToCheck = 0, int heightLayer = -1, float timeToDisplayCollisions = 0)
		{
			if (_dotPaths.Count > 0) return;

            if (timeToDisplayCollisions > 0)
            {
                if (m_temporalDots.Count > 0)
                {
                    for (int i = 0; i < m_temporalDots.Count; i++)
                    {
                        GameObject.DestroyImmediate(m_temporalDots[i]);
                    }
                }
                m_temporalDots.Clear();
            }

            for (int x = 0; x < _rows; x++)
			{
				for (int y = 0; y < _cols; y++)
				{
					int cellContent = _cells[layerToCheck][(x * _cols) + y];
                    float finalHeightDot = 0f * _cellSize + _yIni;
                    if (heightLayer != -1)
                    {
                        finalHeightDot += (heightLayer * (_cellSize * 2));
                    }
                    Vector3 pos = new Vector3((float)((x * _cellSize)) + (_cellSize / 2) + _xIni, finalHeightDot, (float)((y * _cellSize)) + (_cellSize / 2) + _zIni);
                    GameObject newdot;
                    if (cellContent == PathFindingController.CELL_EMPTY)
					{
						newdot = (GameObject)Instantiate(PathFindingController.Instance.DotReferenceEmtpy, this.gameObject.transform);
					}
                    else
                    {
                        newdot = (GameObject)Instantiate(PathFindingController.Instance.DotReference, this.gameObject.transform);
                    }
                    newdot.transform.localScale = new Vector3(_cellSize / 3, _cellSize / 3, _cellSize / 3);
                    newdot.transform.position = pos;
                    newdot.transform.parent = PathFindingController.Instance.transform;
                    if (timeToDisplayCollisions > 0)
                    {
                        m_temporalDots.Add(newdot);
                        GameObject.Destroy(newdot, timeToDisplayCollisions);
                    }
                    else
                    {
                        _dotPaths.Add(newdot);
                    }                    
                }
			}
		}

        public void CalculateCollisions(int layerToCheck = 0, params string[] layersToIgnore)
        {
            _cells = new int[_layers][];
            for (int z = 0; z < _layers; z++)
            {
                _cells[z] = new int[_totalCells];
                for (int x = 0; x < _rows; x++)
                {
                    for (int y = 0; y < _cols; y++)
                    {
                        _cells[z][(x * _cols) + y] = PathFindingController.CELL_EMPTY;
                    }
                }
            }

            for (int x = 0; x < _rows; x++)
            {
                for (int y = 0; y < _cols; y++)
                {
                    int cellContent = _cells[layerToCheck][(x * _cols) + y];
                    Vector3 posAir = new Vector3((float)((x * _cellSize)) + (_cellSize / 2) + _xIni, 1000, (float)((y * _cellSize)) + (_cellSize / 2) + _zIni);
                    Vector3 posAir1 = new Vector3(posAir.x - (_cellSize / 3), posAir.y, posAir.z - (_cellSize / 3));
                    Vector3 posAir2 = new Vector3(posAir.x + (_cellSize / 3), posAir.y, posAir.z - (_cellSize / 3));
                    Vector3 posAir3 = new Vector3(posAir.x - (_cellSize / 3), posAir.y, posAir.z + (_cellSize / 3));
                    Vector3 posAir4 = new Vector3(posAir.x + (_cellSize / 3), posAir.y, posAir.z + (_cellSize / 3));

                    RaycastHit raycastHit = new RaycastHit();
                    if (RaycastingTools.GetRaycastHitInfoByRay(posAir1, new Vector3(0, -1, 0), ref raycastHit, layersToIgnore))
                    {
                        if (raycastHit.collider.gameObject.layer != LayerMask.NameToLayer(PathFindingController.TAG_FLOOR))
                        {
                            _cells[layerToCheck][(x * _cols) + y] = PathFindingController.CELL_COLLISION;
                        }
                    }
                    if (RaycastingTools.GetRaycastHitInfoByRay(posAir2, new Vector3(0, -1, 0), ref raycastHit, layersToIgnore))
                    {
                        if (raycastHit.collider.gameObject.layer != LayerMask.NameToLayer(PathFindingController.TAG_FLOOR))
                        {
                            _cells[layerToCheck][(x * _cols) + y] = PathFindingController.CELL_COLLISION;
                        }
                    }
                    if (RaycastingTools.GetRaycastHitInfoByRay(posAir3, new Vector3(0, -1, 0), ref raycastHit, layersToIgnore))
                    {
                        if (raycastHit.collider.gameObject.layer != LayerMask.NameToLayer(PathFindingController.TAG_FLOOR))
                        {
                            _cells[layerToCheck][(x * _cols) + y] = PathFindingController.CELL_COLLISION;
                        }
                    }
                    if (RaycastingTools.GetRaycastHitInfoByRay(posAir4, new Vector3(0, -1, 0), ref raycastHit, layersToIgnore))
                    {
                        if (raycastHit.collider.gameObject.layer != LayerMask.NameToLayer(PathFindingController.TAG_FLOOR))
                        {
                            _cells[layerToCheck][(x * _cols) + y] = PathFindingController.CELL_COLLISION;
                        }
                    }
                }
            }
        }

        private int GetDirectionByPosition(int xOrigin, int yOrigin, int xDestination, int yDestination)
		{
			if (yOrigin > yDestination) return (PathFindingController.DIRECTION_UP);
			if (yOrigin < yDestination) return (PathFindingController.DIRECTION_DOWN);
			if (xOrigin < xDestination) return (PathFindingController.DIRECTION_RIGHT);
			if (xOrigin > xDestination) return (PathFindingController.DIRECTION_LEFT);

			return (PathFindingController.DIRECTION_NONE);
		}

		public bool CheckOutsideBoard(float x, float y, float z)
		{
			int xCheck = (int)(x / _cellSize);
			int zCheck = (int)(z / _cellSize);
			if (zCheck < 0) return true;
			if (xCheck < 0) return true;
			if (xCheck >= _rows) return true;
			if (zCheck >= _cols) return true;
			return false;
		}

        public Vector3 GetCellPositionInMatrix(float x, float y, float z)
        {
            int xCheck = (int)((x - _xIni) / _cellSize);
            int zCheck = (int)((z - _zIni) / _cellSize);
            return new Vector3(xCheck, zCheck, 0);
        }

        public int GetCellContentByRealPosition(float x, float y, float z)
		{
			int xCheck = (int)((x - _xIni) / _cellSize);
			int zCheck = (int)((z - _zIni) / _cellSize);
			return GetCellContent(xCheck, zCheck, 0);
		}

		public int GetCellContent(int x, int y, int z)
		{
			if (y < 0) return PathFindingController.CELL_COLLISION;
			if (x < 0) return PathFindingController.CELL_COLLISION;
			if (z < 0) return PathFindingController.CELL_COLLISION;
			if (y >= _cols) return PathFindingController.CELL_COLLISION;
			if (x >= _rows) return PathFindingController.CELL_COLLISION;
			if (z >= _layers) return PathFindingController.CELL_COLLISION;
			return (int)(_cells[z][(x * _cols) + y]);
		}

		public bool OutOfBoundaries(int x, int y, int z)
		{
			if (y < 0) return true;
			if (x < 0) return true;
			if (z < 0) return true;
			if (y >= _cols) return true;
			if (x >= _rows) return true;
			if (z >= _layers) return true;
			return false;
		}

		private float GetDistance(int xOrigin, int yOrigin, int zOrigin, int xDestination, int yDestination, int zDestination)
		{
			return (Mathf.Abs(xOrigin - xDestination) + Math.Abs(yOrigin - yDestination) + Math.Abs(zOrigin - zDestination));
		}

		public bool CheckCollidedContent(int _content)
		{
			return (_content != PathFindingController.CELL_EMPTY);
		}

        public Vector3 GetRandomFreeCellBorder(int layer = 0)
        {
            int finalX = -1;
            int finalY = -1;
            do
            {
                if (UnityEngine.Random.Range(0, 100) > 50)
                {
                    // ROWS
                    if (UnityEngine.Random.Range(0, 100) > 50)
                    {
                        finalX = 0;
                    }
                    else
                    {
                        finalX = _rows - 1;
                    }

                    finalY = UnityEngine.Random.Range((int)0, (int)_cols);
                }
                else
                {
                    // COLS
                    if (UnityEngine.Random.Range(0, 100) > 50)
                    {
                        finalY = 0;
                    }
                    else
                    {
                        finalY = _cols - 1;
                    }

                    finalX = UnityEngine.Random.Range((int)0, (int)_rows);
                }
            }
            while (GetCellContent(finalX, finalY, layer) != PathFindingController.CELL_EMPTY);

            return new Vector3((finalX * _cellSize) + (_cellSize / 2) + _xIni, (_cellSize / _waypointHeight), (finalY * _cellSize) + (_cellSize / 2) + _zIni);
        }

        private int GetHops(int current)
		{
			int curIndexBack = current;
			int hops = 0;
			do
			{
				curIndexBack = _matrixAI[curIndexBack].PreviousCell;
				hops++;
			} while ((curIndexBack != 0) && (curIndexBack != -1));
			return hops;
		}

        public Vector3 IsPositionInFreeNode(Vector3 position)
        {
            Vector3 positionCheck = new Vector3(position.x, (_cellSize / _waypointHeight), position.z);
            Vector3 basePosition = GetCellPositionInMatrix(positionCheck.x, positionCheck.y, positionCheck.z);
            if (GetCellContent((int)basePosition.x, (int)basePosition.y, 0) == PathFindingController.CELL_EMPTY)
            {
                return new Vector3((basePosition.x * _cellSize) + _xIni, (_cellSize / _waypointHeight), (basePosition.y * _cellSize) + _zIni);
            }
            else
            {
                return Vector3.down;
            }
        }

        public Vector3 GetClosestFreeNode(Vector3 position)
        {
            Vector3 positionCheck = new Vector3(position.x, (_cellSize / _waypointHeight), position.z);
            Vector3 basePosition = GetCellPositionInMatrix(positionCheck.x, positionCheck.y, positionCheck.z);
            if (GetCellContent((int)basePosition.x, (int)basePosition.y, 0) == PathFindingController.CELL_EMPTY)
            {
                return new Vector3((basePosition.x * _cellSize) + (_cellSize/2) + _xIni, (_cellSize / _waypointHeight), (basePosition.y * _cellSize) + (_cellSize / 2) + _zIni);
            }
            else
            {
                Vector3 closestFreeCell = Vector3.down;
                float minimumDistance = 1000000f;
                for (int i = (int)basePosition.x - 5; i < (int)basePosition.x + 5; i++)
                {
                    for (int j = (int)basePosition.y - 5; j < (int)basePosition.y + 5; j++)
                    {
                        if (!OutOfBoundaries(i,j,0))
                        {
                            if (GetCellContent(i, j, 0) == PathFindingController.CELL_EMPTY)
                            {
                                Vector3 currentPosition = new Vector3((i * _cellSize) + (_cellSize / 2) + _xIni, (_cellSize / _waypointHeight), (j * _cellSize) + (_cellSize / 2) + _zIni);
                                float currentDistance = Vector3.Distance(position, currentPosition);
                                if (currentDistance < minimumDistance)
                                {
                                    minimumDistance = currentDistance;
                                    closestFreeCell = currentPosition;
                                }
                            }
                        }
                    }
                }
                return closestFreeCell;
            }
        }

        public Vector3 GetPath(Vector3 origin,
                                Vector3 destination,
                                List<Vector3> waypoints,
                                int oneLayer,
                                bool raycastFilter,
                                int limitSearch = -1,
                                params string[] masksToIgnore)
        {
            Vector3 originCheck = new Vector3();
            originCheck.x = (int)((origin.x - _xIni) / _cellSize);
            originCheck.y = (int)((origin.z - _zIni) / _cellSize);
            originCheck.z = (oneLayer != -1? oneLayer : ((int)((origin.y - _yIni) / _cellSize)));

            Vector3 destinationCheck = new Vector3();
            destinationCheck.x = (int)((destination.x - _xIni) / _cellSize);
            destinationCheck.y = (int)((destination.z - _zIni) / _cellSize);
            destinationCheck.z = (oneLayer != -1 ? oneLayer : ((int)((destination.y - _yIni) / _cellSize)));

            int limitSearchCheck = ((limitSearch == -1) ? _totalCells - 5 : limitSearch);

            if (!_hasBeenFileLoaded)
            {
                return SearchAStar(originCheck, destinationCheck, origin, destination, waypoints, (oneLayer != -1), limitSearchCheck, raycastFilter, masksToIgnore);
            }
            else
            {
                int cellOrigin = (int)((originCheck.x * _cols) + originCheck.y);
                int cellDestination = (int)((destinationCheck.x * _cols) + destinationCheck.y);
                if (_vectorPaths.Data[cellOrigin] == null) return Vector3.zero;
                if (_vectorPaths.Data[cellOrigin][cellDestination] == null) return Vector3.zero;
                return _vectorPaths.Data[cellOrigin][cellDestination].GetVector3();
            }
        }

        public Vector3 SearchAStar(Vector3 origin,
								Vector3 destination,
                                Vector3 realOrigin,
                                Vector3 realDestination,
                                List<Vector3> waypoints,
								bool oneLayer,
                                int limitSearch,
                                bool raycastFilter = false,
                                params string[] masksToIgnore)
		{
			int i;
			int j;
			float minimalValue;
			int currentNodeEvaluated;

			_sizeMatrix = 0;
			_numCellsGenerated = 0;

			if (PathFindingController.DEBUG_PATHFINDING)
			{
				Debug.Log("cPathFinding.as::SearchAStar:: ORIGIN(" + origin.x + "," + origin.y + "," + origin.z + "); DESTINATION(" + destination.x + "," + origin.y + "," + origin.z + "); COLUMNS=" + _cols + ";ROWS=" + _rows + ";HEIGHT=" + _layers);
				Debug.Log("CONTENT=" + _cells);
			}

			// SAME POSITION
			if ((origin.x == destination.x) && (origin.y == destination.y) && (origin.z == destination.z))
			{
				return Vector3.zero;
			}

			// RESET MATRIX
			for (i = 0; i < _totalCells; i++)
			{
				_matrixAI[i].Reset();
			}

			// INITIALIZE FIRST POSITION
			_sizeMatrix = 0;
			_matrixAI[_sizeMatrix].X = (int)origin.x;
			_matrixAI[_sizeMatrix].Y = (int)origin.y;
			_matrixAI[_sizeMatrix].Z = (int)origin.z;
			_matrixAI[_sizeMatrix].HasBeenVisited = NodePathMatrix.NODE_VISITED;
			_matrixAI[_sizeMatrix].DirectionInitial = PathFindingController.DIRECTION_NONE;
			if ((destination.x == -1) && (destination.y == -1) && (destination.z == -1))
			{
				_matrixAI[_sizeMatrix].ValueSearch = 0;
			}
			else
			{
				_matrixAI[_sizeMatrix].ValueSearch = GetDistance((int)origin.x, (int)origin.y, (int)origin.z,
																(int)destination.x, (int)destination.y, (int)destination.z);
			}
			_matrixAI[_sizeMatrix].PreviousCell = -1;

			// SEARCH
			i = 0;
			do
			{
				_numCellsGenerated = 0;

				// CHECK OVERFLOW
				if (_sizeMatrix > limitSearch)
				{
                    if (PathFindingController.DEBUG_PATHFINDING) Debug.Log("cPathFinding.as::SearchAStar:: RETURN 1");
                    return Vector3.zero;
				}

				// LOOK FOR THE FIRST BEST NODE TO CONTINUE
				minimalValue = 100000000;
				i = -1;
				for (j = 0; j <= _sizeMatrix; j++)
				{
					if (_matrixAI[j].HasBeenVisited == NodePathMatrix.NODE_VISITED) // CHECKED
					{
						if (_matrixAI[j].ValueSearch < minimalValue)
						{
							i = j;
							minimalValue = _matrixAI[j].ValueSearch;
						}
					}
				}

				if (i == -1)
				{
					if (PathFindingController.DEBUG_PATHFINDING) Debug.Log("cPathFinding.as::SearchAStar:: RETURN 2");
					return Vector3.zero;
				}

				// SELECT NODE
				currentNodeEvaluated = i;
				if ((_matrixAI[i].X == destination.x) && (_matrixAI[i].Y == destination.y) && (_matrixAI[i].Z == destination.z))
				{
					// CREATE THE LIST OF CELLS BETWEEN DESTINATION-ORIGIN
					List<Vector3> way = new List<Vector3>();
					if (i == -1)
					{
						return Vector3.zero;
					}
					else
					{
						int curIndexBack = i;
						Vector3 sGoalNext = new Vector3(-1f, -1f, -1f);
						Vector3 sGoalCurrent = new Vector3(0f, 0f, 0f);
                        Vector3 pivotReference = Vector3.zero;
                        Vector3 currentChecked = Vector3.zero;
                        Vector3 previousChecked = Vector3.zero;
						do
						{

							sGoalCurrent.x = _matrixAI[curIndexBack].X;
							sGoalCurrent.y = _matrixAI[curIndexBack].Y;
							sGoalCurrent.z = _matrixAI[curIndexBack].Z;

							sGoalNext.x = sGoalCurrent.x;
							sGoalNext.y = sGoalCurrent.y;
							sGoalNext.z = sGoalCurrent.z;

                            curIndexBack = _matrixAI[curIndexBack].PreviousCell;

                            // INSERT WAYPOINT
                            if (!raycastFilter)
                            {
                                if (oneLayer)
                                {
                                    way.Insert(0, new Vector3((sGoalNext.x * _cellSize) + _xIni + (_cellSize/2), (_cellSize / _waypointHeight), (sGoalNext.y * _cellSize) + _zIni + (_cellSize / 2)));
                                }
                                else
                                {
                                    way.Insert(0, new Vector3((sGoalNext.x * _cellSize) + _xIni + (_cellSize / 2), sGoalNext.z - (_cellSize / _waypointHeight) + _yIni, (sGoalNext.y * _cellSize) + _zIni + (_cellSize / 2)));
                                }
                            }
                            else
                            {
                                if (oneLayer)
                                {
                                    if (pivotReference == Vector3.zero)
                                    {
                                        currentChecked = new Vector3(realDestination.x + (_cellSize / 2), (_cellSize / _waypointHeight), realDestination.z + (_cellSize / 2));
                                        way.Insert(0, currentChecked);
                                        pivotReference = currentChecked;
                                    }
                                    else
                                    {
                                        Vector3 lastValidChecked = previousChecked;
                                        previousChecked = currentChecked;
                                        currentChecked = new Vector3((sGoalNext.x * _cellSize) + _xIni + (_cellSize / 2), (_cellSize / _waypointHeight), (sGoalNext.y * _cellSize) + _zIni + (_cellSize / 2));
                                        if ((curIndexBack == 0) || (curIndexBack == -1))
                                        {
                                            currentChecked = new Vector3(realOrigin.x + (_cellSize / 2), (_cellSize / _waypointHeight), realOrigin.z + (_cellSize / 2));
                                        }
                                        if (CheckBlockedPath(new Vector3(currentChecked.x, _pathCheckHeight, currentChecked.z), new Vector3(pivotReference.x, _pathCheckHeight, pivotReference.z), 3, masksToIgnore))
                                        {
                                            way.Insert(0, lastValidChecked);
                                            pivotReference = previousChecked;                                            
                                        }
                                    }
                                }
                                else
                                {
                                    way.Insert(0, new Vector3((sGoalNext.x * _cellSize) + _xIni + (_cellSize/2), sGoalNext.z - (_cellSize / _waypointHeight) + _yIni, (sGoalNext.y * _cellSize) + _zIni));
                                }
                            }

                            

						} while ((curIndexBack != 0) && (curIndexBack != -1));

						ClearDotPaths();

                        // DRAW DEBUG BALLS
                        if (waypoints != null)
                        {
                            for (int o = 0; o < way.Count; o++)
                            {
                                Vector3 sway = way[o];
                                waypoints.Add(new Vector3(sway.x, sway.y, sway.z));
                                CreateDotPath(sway, way.Count);
                            }
                        }

                        if (way.Count > 0)
                        {
                            return way[0];
                        }
                        else
                        {
                            return Vector3.zero;
                        }                        
                    }
				}

				// SET AS VISITED NODE
				_matrixAI[currentNodeEvaluated].HasBeenVisited = 0;

				if (PathFindingController.DEBUG_PATHFINDING) Debug.Log("cPathFinding.as::SearchAStar::ANALIZING(" + _matrixAI[i].X + "," + _matrixAI[i].Y + "," + _matrixAI[i].Z + ")");

				// CHILD UP
				ChildGeneration(i, currentNodeEvaluated, (int)(_matrixAI[i].X), (int)(_matrixAI[i].Y + 1), (int)(_matrixAI[i].Z), (int)destination.x, (int)destination.y, (int)destination.z, PathFindingController.DIRECTION_DOWN, oneLayer);
				if (!oneLayer) ChildGeneration(i, currentNodeEvaluated, (int)(_matrixAI[i].X), (int)(_matrixAI[i].Y + 1), (int)(_matrixAI[i].Z - 1), (int)destination.x, (int)destination.y, (int)destination.z, PathFindingController.DIRECTION_DOWN, oneLayer);
				if (!oneLayer) ChildGeneration(i, currentNodeEvaluated, (int)(_matrixAI[i].X), (int)(_matrixAI[i].Y + 1), (int)(_matrixAI[i].Z + 1), (int)destination.x, (int)destination.y, (int)destination.z, PathFindingController.DIRECTION_DOWN, oneLayer);

				// Child DOWN
				ChildGeneration(i, currentNodeEvaluated, (int)(_matrixAI[i].X), (int)(_matrixAI[i].Y - 1), (int)(_matrixAI[i].Z), (int)destination.x, (int)destination.y, (int)destination.z, PathFindingController.DIRECTION_UP, oneLayer);
				if (!oneLayer) ChildGeneration(i, currentNodeEvaluated, (int)(_matrixAI[i].X), (int)(_matrixAI[i].Y - 1), (int)(_matrixAI[i].Z - 1), (int)destination.x, (int)destination.y, (int)destination.z, PathFindingController.DIRECTION_UP, oneLayer);
				if (!oneLayer) ChildGeneration(i, currentNodeEvaluated, (int)(_matrixAI[i].X), (int)(_matrixAI[i].Y - 1), (int)(_matrixAI[i].Z + 1), (int)destination.x, (int)destination.y, (int)destination.z, PathFindingController.DIRECTION_UP, oneLayer);

				// Child LEFT
				ChildGeneration(i, currentNodeEvaluated, (int)(_matrixAI[i].X - 1), (int)(_matrixAI[i].Y), (int)(_matrixAI[i].Z), (int)destination.x, (int)destination.y, (int)destination.z, PathFindingController.DIRECTION_LEFT, oneLayer);
				if (!oneLayer) ChildGeneration(i, currentNodeEvaluated, (int)(_matrixAI[i].X - 1), (int)(_matrixAI[i].Y), (int)(_matrixAI[i].Z - 1), (int)destination.x, (int)destination.y, (int)destination.z, PathFindingController.DIRECTION_LEFT, oneLayer);
				if (!oneLayer) ChildGeneration(i, currentNodeEvaluated, (int)(_matrixAI[i].X - 1), (int)(_matrixAI[i].Y), (int)(_matrixAI[i].Z + 1), (int)destination.x, (int)destination.y, (int)destination.z, PathFindingController.DIRECTION_LEFT, oneLayer);

				//  Child RIGHT
				ChildGeneration(i, currentNodeEvaluated, (int)(_matrixAI[i].X + 1), (int)(_matrixAI[i].Y), (int)(_matrixAI[i].Z), (int)destination.x, (int)destination.y, (int)destination.z, PathFindingController.DIRECTION_RIGHT, oneLayer);
				if (!oneLayer) ChildGeneration(i, currentNodeEvaluated, (int)(_matrixAI[i].X + 1), (int)(_matrixAI[i].Y), (int)(_matrixAI[i].Z - 1), (int)destination.x, (int)destination.y, (int)destination.z, PathFindingController.DIRECTION_RIGHT, oneLayer);
				if (!oneLayer) ChildGeneration(i, currentNodeEvaluated, (int)(_matrixAI[i].X + 1), (int)(_matrixAI[i].Y), (int)(_matrixAI[i].Z + 1), (int)destination.x, (int)destination.y, (int)destination.z, PathFindingController.DIRECTION_RIGHT, oneLayer);

			} while (true);
		}

		private int GetCorrectChild(int xPosition, int yPosition, int zPosition, int sizeMatrix, bool oneLayer)
		{
			int sCell;
			int i;

			// Position outside the bounds
			if ((xPosition < 0) || (xPosition >= _rows)) return (0);
			if ((yPosition < 0) || (yPosition >= _cols)) return (0);
			if ((zPosition < 0) || (zPosition >= _layers)) return (0);

			// Collision control
			sCell = GetCellContent(xPosition, yPosition, zPosition);
			if (!CheckCollidedContent(sCell))
			{
				// Check if the cell has been evaluated
				for (i = 0; i <= sizeMatrix; i++)
				{
					if ((_matrixAI[i].X == xPosition) && (_matrixAI[i].Y == yPosition) && (_matrixAI[i].Z == zPosition))
						return (0);
				}
				return 1;
			}
			return 0;
		}

		private void ChildGeneration(int index,
									int searched,
									int xOrigin, int yOrigin, int zOrigin,
									int xDestination, int yDestination, int zDestination,
									int initialDirection,
									bool oneLayer)
		{
			int posx = xOrigin;
			int posy = yOrigin;
			int posz = zOrigin;
			int directionInitial = -1;
			if (GetCorrectChild(posx, posy, posz, _sizeMatrix, oneLayer) == 1)
			{
				if (_matrixAI[searched].DirectionInitial == PathFindingController.DIRECTION_NONE)
				{
					directionInitial = initialDirection;
				}
				else
				{
					directionInitial = _matrixAI[searched].DirectionInitial;
				}

				_sizeMatrix++;
				_matrixAI[_sizeMatrix].X = posx;
				_matrixAI[_sizeMatrix].Y = posy;
				_matrixAI[_sizeMatrix].Z = posz;
				_matrixAI[_sizeMatrix].HasBeenVisited = NodePathMatrix.NODE_VISITED;
				_matrixAI[_sizeMatrix].DirectionInitial = directionInitial;
				if ((xDestination == PathFindingController.DIRECTION_NONE) && (yDestination == PathFindingController.DIRECTION_NONE) && (zDestination == PathFindingController.DIRECTION_NONE))
				{
					_matrixAI[_sizeMatrix].ValueSearch = 0;
				}
				else
				{
                    _matrixAI[_sizeMatrix].ValueSearch = (float)GetHops(index); // hops
                }
				_matrixAI[_sizeMatrix].PreviousCell = index;
				_numCellsGenerated++;
			}
		}

        private int _xIterator = -1;
        private int _yIterator = 0;
        private bool _calculationCompleted = false;

        private int _iIterator = -1;
        private int _jIterator = 0;
        private bool _iterationCompleted = true;

        private string _filenamePath = "Assets/pathfinding.dat";
        private bool _hasBeenFileLoaded = false;

        private void SavePathfindingData()
        {
            FileStream file;

            if (File.Exists(_filenamePath)) file = File.OpenWrite(_filenamePath);
            else file = File.Create(_filenamePath);

            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(file, _vectorPaths);
            file.Close();
        }

        public void LoadFile(string filenamePath)
        {
            if (_hasBeenFileLoaded) return;
            _hasBeenFileLoaded = true;

            FileStream file;

            _filenamePath = filenamePath;

            if (File.Exists(_filenamePath)) file = File.OpenRead(_filenamePath);
            else
            {
                Debug.LogError("File not found");
                return;
            }

            BinaryFormatter bf = new BinaryFormatter();
            _vectorPaths = (PrecalculatedData)bf.Deserialize(file);
            file.Close();
        }

        public void LoadAsset(TextAsset fileAsset)
        {
            if (_hasBeenFileLoaded) return;
            _hasBeenFileLoaded = true;

            Stream stream = new MemoryStream(fileAsset.bytes);
            BinaryFormatter formatter = new BinaryFormatter();
            _vectorPaths = formatter.Deserialize(stream) as PrecalculatedData;
        }

        public void CalculateAll(string filenamePath, bool raycastFilter = false, params string[] masksToIgnore)
        {
            if (_calculationCompleted) return;

            _filenamePath = filenamePath;

            if (_xIterator == -1)
            {
                _vectorPaths = new PrecalculatedData(new CustomVector3[_rows * _cols][]);
            }

            PathFindingController.Instance.DebugPathPoints = false;
            if (_iterationCompleted)
            {
                _iterationCompleted = false;
                _xIterator++;
                if (_xIterator >= _rows)
                {
                    _xIterator = 0;
                    _yIterator++;
                    if (_yIterator >= _cols)
                    {
                        _calculationCompleted = true;
                        SavePathfindingData();
                        return;
                    }
                }
                int originatorCell = (_xIterator * _cols) + _yIterator;
                _vectorPaths.Data[originatorCell] = new CustomVector3[_rows * _cols];
            }

            int originCell = (_xIterator * _cols) + _yIterator;
            if (_cells[0][originCell] != PathFindingController.CELL_EMPTY)
            {
                _iterationCompleted = true;             
            }
            else
            {
                _iIterator++;
                if (_iIterator >= _rows)
                {
                    _iIterator = 0;
                    _jIterator++;
                    if (_jIterator >= _cols)
                    {
                        _iIterator = -1;
                        _jIterator = 0;
                        _iterationCompleted = true;
                        return;
                    }
                }
                int targetCell = (_iIterator * _cols) + _jIterator;
                _vectorPaths.Data[originCell][targetCell] = new CustomVector3();
                if (!((_xIterator == _iIterator) && (_yIterator == _jIterator)))
                {
                    if (_cells[0][targetCell] == PathFindingController.CELL_EMPTY)
                    {
                        Vector3 origin = new Vector3(_xIterator, _yIterator, 0);
                        Vector3 destination = new Vector3(_iIterator, _jIterator, 0);
                        int limitSearch = _totalCells - 1;                        
                        _vectorPaths.Data[originCell][targetCell].SetVector3( SearchAStar(origin, destination, Vector3.zero, Vector3.one, null, true, limitSearch, raycastFilter, masksToIgnore));
                    }
                }
            }
        }
    }

}