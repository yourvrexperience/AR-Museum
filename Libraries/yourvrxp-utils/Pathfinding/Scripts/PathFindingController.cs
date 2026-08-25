using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace yourvrexperience.Utils
{
    public class PathFindingController : MonoBehaviour
    {
        public const bool DEBUG_MATRIX_CONSTRUCTION = false;
        public const bool DEBUG_PATHFINDING = false;
        public const bool DEBUG_DOTPATHS = false;

        public const string TAG_FLOOR = "Floor";
        public const string TAG_PATH = "PATH";

        public const int CELL_EMPTY = 0;
        public const int CELL_COLLISION = 1;

        public const int DIRECTION_LEFT = 1;
        public const int DIRECTION_RIGHT = 2;
        public const int DIRECTION_UP = 100;
        public const int DIRECTION_DOWN = 200;
        public const int DIRECTION_NONE = -1;

        private static PathFindingController _instance;

        public static PathFindingController Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = GameObject.FindObjectOfType(typeof(PathFindingController)) as PathFindingController;
                }
                return _instance;
            }
        }

        public GameObject PathFindingPrefab;
        public GameObject DotReference;
        public GameObject DotReferenceEmtpy;
        public GameObject DotReferenceWay;

        public bool DebugPathPoints;

        private List<PathFindingInstance> pathfindingInstances = new List<PathFindingInstance>();
        private bool _isPrecalculated = false;

        public bool IsPrecalculated
        {
            get { return _isPrecalculated; }
        }

        public void Initialize()
        {
        }

        public void Destroy()
        {
            if (_instance == null) return;
            _instance = null;
        }

        public void SetWaypointHeight(float waypointHeight, int layer = -1)
        {
            if (layer == -1)
            {
                pathfindingInstances[pathfindingInstances.Count - 1].WaypointHeight = waypointHeight;
            }
            else
            {
                pathfindingInstances[layer].WaypointHeight = waypointHeight;
            }
        }

        public void SetPathWaypointHeight(float pathHeight, int layer = -1)
        {
            if (layer == -1)
            {
                pathfindingInstances[pathfindingInstances.Count - 1].PathCheckHeight = pathHeight;
            }
            else
            {
                pathfindingInstances[layer].PathCheckHeight = pathHeight;
            }
        }

        public bool CheckOutsideBoard(float x, float y, float z, int layer = -1)
        {
            if (layer == -1)
            {
                return pathfindingInstances[pathfindingInstances.Count - 1].CheckOutsideBoard(x, y, z);
            }
            else
            {
                return pathfindingInstances[layer].CheckOutsideBoard(x, y, z);
            }
        }        

        public Vector3 GetCellPositionInMatrix(float x, float y, float z, int layer = -1)
        {
            if (layer == -1)
            {
                return pathfindingInstances[pathfindingInstances.Count - 1].GetCellPositionInMatrix(x, y, z);
            }
            else
            {
                return pathfindingInstances[layer].GetCellPositionInMatrix(x, y, z);
            }
        }

        public int GetCellContentByRealPosition(float x, float y, float z, int layer = -1)
        {
            if (layer == -1)
            {
                return pathfindingInstances[pathfindingInstances.Count - 1].GetCellContentByRealPosition(x, y, z);
            }
            else
            {
                return pathfindingInstances[layer].GetCellContentByRealPosition(x, y, z);
            }
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
            GameObject newPathfindingInstance = Instantiate(PathFindingPrefab);
            newPathfindingInstance.GetComponent<PathFindingInstance>().AllocateMemoryMatrix(cols, rows, layers, cellSize, xIni, yIni, zIni, initContent);
            pathfindingInstances.Add(newPathfindingInstance.GetComponent<PathFindingInstance>());
        }

        public void DestroyInstances()
        {
            if (pathfindingInstances != null)
            {
                foreach (PathFindingInstance instance in pathfindingInstances)
                {
                    if (instance != null)
                    {
                        instance.ClearMemoryAllocated();
                        instance.Destroy();
                        instance.DestroyDebugMatrixConstruction();
                        GameObject.Destroy(instance.gameObject);
                    }
                }
                pathfindingInstances.Clear();
            }
        }

        public void CalculateCollisions(int layerToCheck = 0, params string[] layersToIgnore)
        {
            foreach (PathFindingInstance pathInstance in pathfindingInstances)
            {
                pathInstance.CalculateCollisions(layerToCheck, layersToIgnore);
            }
        }

        public void ClearDotPaths()
        {
            foreach (PathFindingInstance pathInstance in pathfindingInstances)
            {
                pathInstance.ClearDotPaths();
            }
        }

        public GameObject CreateSingleDot(Vector3 position, float size, int type, int layer = -1)
        {
            if (layer == -1)
            {
                return pathfindingInstances[pathfindingInstances.Count - 1].CreateSingleDot(position, size, type);
            }
            else
            {
                return pathfindingInstances[layer].CreateSingleDot(position, size, type);
            }
        }

        public void RenderDebugMatrixConstruction(int layer = -1, float timeToDisplayCollisions = 0)
        {
            if (timeToDisplayCollisions > 0)
            {
                if (layer == -1)
                {
                    for (int i = 0; i < pathfindingInstances.Count; i++)
                    {
                        pathfindingInstances[i].RenderDebugMatrixConstruction(0, pathfindingInstances.Count - 1 - i, timeToDisplayCollisions);
                    }
                }
                else
                {
                    pathfindingInstances[layer].RenderDebugMatrixConstruction(layer, -1, timeToDisplayCollisions);
                }
            }
        }

        public void DestroyDebugMatrixConstruction(int layer = -1)
        {
            if (pathfindingInstances != null)
            {
                if (layer == -1)
                {
                    for (int i = 0; i < pathfindingInstances.Count; i++)
                    {
                        pathfindingInstances[i].DestroyDebugMatrixConstruction();
                    }
                }
                else
                {
                    pathfindingInstances[layer].DestroyDebugMatrixConstruction();
                }
            }
        }

        public bool CheckBlockedPath(Vector3 origin, Vector3 target, float dotSize = 3, params string[] masksToIgnore)
        {
            return (RaycastingTools.GetCollidedObjectBySegmentTargetIgnore(target, origin, masksToIgnore));
        }

        public Vector3 GetPath(Vector3 origin,
                                Vector3 destination,
                                List<Vector3> waypoints,
                                int oneLayer,
                                bool raycastFilter,
                                int limitSearch = -1,
                                params string[] masksToIgnore)
        {
            return pathfindingInstances[pathfindingInstances.Count - 1].GetPath(origin, destination, waypoints, oneLayer, raycastFilter, limitSearch, masksToIgnore);
        }

        public Vector3 GetPathLayer(int layer,
                                Vector3 origin,
                                Vector3 destination,
                                List<Vector3> waypoints,
                                int oneLayer,
                                bool raycastFilter,
                                int limitSearch = -1,
                                params string[] masksToIgnore)
        {
            return pathfindingInstances[layer].GetPath(origin, destination, waypoints, oneLayer, raycastFilter, limitSearch, masksToIgnore);
        }

        public Vector3 IsPositionInFreeNode(Vector3 position, int layer = -1)
        {
            if (layer == -1)
            {
                return pathfindingInstances[pathfindingInstances.Count - 1].IsPositionInFreeNode(position);
            }
            else
            {
                return pathfindingInstances[layer].IsPositionInFreeNode(position);
            }
        }

        public Vector3 GetClosestFreeNode(Vector3 position, int layer = -1)
        {
            if (layer == -1)
            {
                return pathfindingInstances[pathfindingInstances.Count - 1].GetClosestFreeNode(position);
            }
            else
            {
                return pathfindingInstances[layer].GetClosestFreeNode(position);
            }
        }

        public float GetCellSize(int layer = -1)
        {
            if (layer == -1)
            {
                return pathfindingInstances[pathfindingInstances.Count - 1].CellSize;
            }
            else
            {
                return pathfindingInstances[layer].CellSize;
            }
        }

        public Vector3 GetRandomFreeCellBorder(int layer = -1)
        {
            if (layer == -1)
            {
                return pathfindingInstances[pathfindingInstances.Count - 1].GetRandomFreeCellBorder();
            }
            else
            {
                return pathfindingInstances[layer].GetRandomFreeCellBorder();
            }
        }

        public bool CheckOutsideBoard(Vector3 position, int layer = -1)
        {
            if (layer == -1)
            {
                return pathfindingInstances[pathfindingInstances.Count - 1].CheckOutsideBoard(position.x, position.y, position.z);
            }
            else
            {
                return pathfindingInstances[layer].CheckOutsideBoard(position.x, position.y, position.z);
            }
        }

        public void CalculateAll(string filenamePath, int layer = -1, bool raycastFilter = false, params string[] masksToIgnore)
        {
            if (layer == -1)
            {
                pathfindingInstances[pathfindingInstances.Count - 1].CalculateAll(filenamePath, raycastFilter, masksToIgnore);
            }
            else
            {
                pathfindingInstances[layer].CalculateAll(filenamePath, raycastFilter, masksToIgnore);
            }
        }

        public void LoadFile(string filenamePath, int layer = -1)
        {
            _isPrecalculated = true;
            if (layer == -1)
            {
                pathfindingInstances[pathfindingInstances.Count - 1].LoadFile(filenamePath);
            }
            else
            {
                pathfindingInstances[layer].LoadFile(filenamePath);
            }
        }

        public void LoadAsset(TextAsset textAsset, int layer = -1)
        {
            _isPrecalculated = true;
            if (layer == -1)
            {
                pathfindingInstances[pathfindingInstances.Count - 1].LoadAsset(textAsset);
            }
            else
            {
                pathfindingInstances[layer].LoadAsset(textAsset);
            }
        }
    }
}