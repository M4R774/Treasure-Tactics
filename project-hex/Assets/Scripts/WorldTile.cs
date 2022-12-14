using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// A class for storing data of each hex. Every hex has a instance of this
// class and you can store data here. 
public class WorldTile
{
    public Vector3Int CellCoordinates { get; set; }
    public Vector3 WorldPosition { get; set; }
    public TileBase TileBase { get; set; }
    public Tilemap TilemapMember { get; set; }
    public string Name { get; set; }
    public GameObject GameObjectOnTheTile { get; set; }
    public List<WorldTile> history;


    // Variables needed for pathfinding
    public int DistanceFromStart { get; set; }
    public int DistanceToTarget { get; set; }
    public WorldTile NextDestination { get; set; }
    public int BFSDistance;

    private bool isVisible;
    private bool isExplored;
    private bool isWithinMovementRange;
    private bool isInBarrageZone;

    public bool IsWalkable()
    {
        if (!GameTiles.instance.TileTypeIsWalkable(this))
        {
            return false; // Tiletype is not walkable
        }
        else if (GameObjectOnTheTile == null)
        {
            return true; // The tiletype is walkable and free of gameobjects
        }
        else // there is a GameObject on a walkable tile
        {
            HideIfNotVisible enemyComponent = GameObjectOnTheTile.GetComponent<HideIfNotVisible>();
            if (enemyComponent == null)
            {
                return true; // There is a gameobject, but you can move through it
            }
            else
            {
                return false; // There was an enemy object
            }
        }
    }

    public bool BlocksVision()
    {
        if (GameTiles.instance.TileTypeBlocksVision(this))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool CanEndTurnHere()
    {

        if (IsWalkable() && GameObjectOnTheTile == null)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool IsVisible 
    {
        get { return isVisible; }
        set
        {
            isVisible = value;
            if (isVisible)
            {
                GameObject instantiatedObject = TilemapMember.GetInstantiatedObject(CellCoordinates);
                if (instantiatedObject != null)
                {
                    instantiatedObject.SetActive(true);
                }
                
                isExplored = true;
            }
            CalculateAndSetTileColor();
        }
    }

    public bool IsExplored 
    {
        get { return isExplored; }
        set 
        {
            isExplored = value;
            CalculateAndSetTileColor();
        }
    }

    public bool IsWithinMovementRange
    {
        get { return isWithinMovementRange; }
        set
        {
            isWithinMovementRange = value;
            CalculateAndSetTileColor();
        }
    }

    public bool IsInBarrageZone
    {
        get { return isInBarrageZone; }
        set
        {
            isInBarrageZone = value;
            CalculateAndSetTileColor();
        }
    }

    public int GetTotalPathCost()
    {
        return DistanceFromStart + DistanceToTarget;
    }

    public int GetTileTypeCost()
    {
        int cost = GameTiles.instance.GetTileTypeCost(this);
        return cost;
    }

    public List<WorldTile> Neighbors()
    {
        int evenOrOddColumn = CellCoordinates.y & 1;
        List<Vector3Int> neighborDirections = Pathfinding.GetPrecalculatedNeighbourDirections()[evenOrOddColumn];

        List<WorldTile> neighbors = new();
        foreach (Vector3Int neigborDirection in neighborDirections)
        {
            var tiles = GameTiles.instance.tiles;
            if (tiles.TryGetValue(CellCoordinates + neigborDirection, out WorldTile _tile))
            {
                neighbors.Add(_tile);
            }
        }

        return neighbors;
    }

    private void CalculateAndSetTileColor()
    {
        if (isVisible)
        {
            SetColorForChildrenMeshes(Color.white);
        }
        else
        {
            SetColorForChildrenMeshes(Color.gray);
        }

        if (isInBarrageZone)
        {
            SetColor(Color.red);
        }
        else if (isVisible) //&& !isWithinMovementRange)
        {
            SetColor(Color.white);
        } 
        /*
        else if (isWithinMovementRange && isVisible)
        {
            Color lightGreen = new(.7f, 1, .7f, 1);
            //SetColor(lightGreen);
        }
        else if (isWithinMovementRange && !isVisible && isExplored)
        {
            Color darkGreen = new(.35f, .5f, .35f, 1);
            //SetColor(darkGreen);
        }
        else if (isWithinMovementRange && !isVisible && !isExplored)
        {
            Color darkGreen = new(.35f, .5f, .35f, .5f);
            //SetColor(darkGreen);
        } */
        else if (isExplored)
        {
            SetColor(Color.grey);
        }
        else 
        {
            Color invisible = new(0,0,0,0);
            SetColor(invisible);
        }
    }

    private void SetColorForChildrenMeshes(Color color)
    {
        GameObject instantiated = TilemapMember.GetInstantiatedObject(CellCoordinates);
        if (instantiated != null)
        {
            for (int i = 0; i < instantiated.transform.childCount; i++)
            {
                GameObject child = instantiated.transform.GetChild(i).gameObject;
                if (child.GetComponent<MeshRenderer>() != null)
                {
                    child.GetComponent<MeshRenderer>().material.color = color;
                }

                for (int j = 0; j < child.transform.childCount; j++)
                {
                    GameObject innerChild = child.transform.GetChild(j).gameObject;
                    if (innerChild.GetComponent<MeshRenderer>() != null)
                    {
                        innerChild.GetComponent<MeshRenderer>().material.color = color;
                    }
                }
            }
        }
    }

    private void SetColor(Color color)
    {
        if (TilemapMember != null)
        {
            TilemapMember.SetTileFlags(CellCoordinates, TileFlags.None);
            TilemapMember.SetColor(CellCoordinates, color);
        }
    }
}
