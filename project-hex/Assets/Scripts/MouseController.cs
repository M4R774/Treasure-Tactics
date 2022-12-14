using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MouseController : MonoBehaviour
{
    public GridLayout gridLayout;
    public RaycastHit hit;
    public LineRenderer lineRenderer;
    public GameObject glowingHex;
    public GameObject projectilePrefab;
    public ISelectable selectedObject;
    private WorldTile oldTileUnderMouse;
    public ActionBarManager actionBarManager;
    private int shootingRange = 10;

    public static MouseController instance;

    public ISelectable GetSelectedObject()
    {
        return selectedObject;
    }

    public void SetSelectedObject(ISelectable newSelected)
    {
        if (selectedObject != null)
        {
            selectedObject.Unselect();
        }
        if (newSelected != null && newSelected.IsPlayable())
        {
            newSelected.Select();
            selectedObject = newSelected;
        }
    }

    private void Start()
    {
        CheckThatIamOnlyInstance();

        lineRenderer = transform.gameObject.GetComponent<LineRenderer>();
        selectedObject = null;

        WorldTile initialPlayerTile = GameTiles.instance.GetTileByWorldPosition(TurnManager.instance.playerControlledUnits[0].GetComponent<ISelectable>().GetTileCoordinates());
        ISelectable initialSelectable = TurnManager.instance.playerControlledUnits[0].GetComponent<ISelectable>();
        HandleSelectable(initialSelectable);
        HandleTile(initialPlayerTile, initialSelectable);
        ResetLineRenderer();

        if(actionBarManager == null) actionBarManager = GameObject.Find("ActionBar").GetComponent<ActionBarManager>();
    }

    private void CheckThatIamOnlyInstance()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (Time.timeScale > 0)
        {
            GetWorldlTileUnderMouse(out WorldTile tileUnderMouse, out ISelectable selectableUnderMouse);
            if (selectedObject != null && selectedObject.IsPlayable()) {
                PlanAndDrawPath(tileUnderMouse);
            }
            oldTileUnderMouse = tileUnderMouse;

            if (Input.GetMouseButtonDown(0))
            {
                HandleSelectable(selectableUnderMouse);
                HandleTile(tileUnderMouse, selectableUnderMouse);
                ResetLineRenderer();
            }
            else if (Input.GetMouseButtonDown(1))
            {
                HandleSelectable(selectableUnderMouse);
                HandlePossibleShootingTile(tileUnderMouse, selectableUnderMouse);
                ResetLineRenderer();
            }
        }
    }

    private void HandleSelectable(ISelectable clickedSelectable)
    {
        if (clickedSelectable != null)
        {
            selectedObject?.Unselect();
            clickedSelectable.Select();
            selectedObject = clickedSelectable;
        }
    }

    private void HandleTile(WorldTile clickedTile, ISelectable clickeckSelectable)
    {
        if (MovementIsPossible(clickedTile, clickeckSelectable))
        {
            Vector3Int selectedObjectCoordinates = selectedObject.GetTileCoordinates();
            GameTiles.instance.tiles.TryGetValue(selectedObjectCoordinates, out WorldTile tile);
            List<WorldTile> path = Pathfinding.FindPath(tile, clickedTile);
            selectedObject.MoveTowardsTarget(path);
        }
    }

    private void HandlePossibleShootingTile(WorldTile clickedTile, ISelectable clickeckSelectable)
    {
        if (ShootingIsPossible(clickedTile, clickeckSelectable))
        {
            FireBarrage(clickedTile.WorldPosition);
        }
    }

    private void FireBarrage(Vector3 positionToFireUpon)
    {
        selectedObject.SetMovementPointsLeft(selectedObject.MovementPointsLeft() - 1);
        GameObject projectile = Instantiate(projectilePrefab, transform, false);
        projectile.GetComponent<MGBulletLerp>().SlerpToTargetAndExplode(
            selectedObject.GetTileUnderMyself().WorldPosition,
            positionToFireUpon
        );
    }

    private void GetWorldlTileUnderMouse(out WorldTile _tile, out ISelectable _selectable)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        _selectable = null;
        _tile = null;
        if (Physics.Raycast(ray, out hit))
        {
            _selectable = hit.transform.GetComponent<ISelectable>();
            Vector3 mousePosition = hit.point;
            Vector3Int tileCoordinatesUnderMouse = gridLayout.WorldToCell(mousePosition);
            var tiles = GameTiles.instance.tiles; // This is our Dictionary of tiles
            tiles.TryGetValue(tileCoordinatesUnderMouse, out _tile);
        }
    }

    private bool MovementIsPossible(WorldTile clickedTile, ISelectable clickedSelectable)
    {
        if (clickedTile != null &&
            clickedSelectable == null &&
            selectedObject != null &&
            clickedTile.CanEndTurnHere() &&
            clickedTile.IsVisible &&
            selectedObject.IsPlayable())
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private bool ShootingIsPossible(WorldTile clickedTile, ISelectable clickedSelectable)
    {
        if (clickedTile != null &&
            clickedSelectable == null &&
            clickedTile.IsVisible &&
            selectedObject.MovementPointsLeft() > 0)
        {
            WorldTile startTile = selectedObject.GetTileUnderMyself();
            List<WorldTile> tilesWithinRange = Pathfinding.GetAllVisibleTiles(startTile, shootingRange);
            if (tilesWithinRange.Contains(clickedTile))
            {
                return true;
            }
        }
        return false;
    }


    private void PlanAndDrawPath(WorldTile tileUnderMouse)
    {
        // If no valid target
        if (!IfDrawingNewLineIsNeeded(tileUnderMouse, out List<WorldTile> path))
        {
            return;
        }
        else
        {
            glowingHex.SetActive(true);
        }

        int movementPointsLeft = selectedObject.MovementPointsLeft();
        int numberOfTilesToMove = selectedObject.MovementPointsLeft();
        if (path.Count < numberOfTilesToMove)
        {
            numberOfTilesToMove = path.Count;
        }

        int offset = path.Count - numberOfTilesToMove;
        int plannedMoveCount = path.Count - offset;

        Vector3[] pathPositions = new Vector3[path.Count + 1];
        pathPositions.SetValue(path[offset].WorldPosition, 0);
        glowingHex.transform.position = path[offset].WorldPosition;
        for (int i = 1; i < numberOfTilesToMove; i++)
        {
            Vector3 newPosition = path[offset + i].WorldPosition;
            newPosition.y = 0.2f;
            pathPositions.SetValue(newPosition, i);
        }
        pathPositions.SetValue(selectedObject.GetTileUnderMyself().WorldPosition, path.Count - offset);

        lineRenderer.positionCount = plannedMoveCount + 1;
        lineRenderer.SetPositions(pathPositions);

        Material mymat = GetComponent<Renderer>().material;
        if (plannedMoveCount <= shootingRange)
        {
            mymat.SetColor("_EmissionColor", Color.red);
        }
        else
        {
            mymat.SetColor("_EmissionColor", Color.yellow);
        }

        actionBarManager.SetPlan(plannedMoveCount);
    }

    private bool IfDrawingNewLineIsNeeded(WorldTile tileUnderMouse, out List<WorldTile> path)
    {
        path = null;
        // If no valid target
        if (selectedObject == null ||
            selectedObject.MovementSpeed <= 0 ||
            tileUnderMouse == null ||
            !tileUnderMouse.IsWalkable() ||
            !tileUnderMouse.IsVisible)
        {
            ResetLineRenderer();
            return false;
        }

        // If nothin has changed
        if (oldTileUnderMouse == tileUnderMouse)
        {
            return false;
        }

        // If no valid path
        path = Pathfinding.FindPath(selectedObject.GetTileUnderMyself(), tileUnderMouse);
        if (path == null || path.Count == 0)
        {
            ResetLineRenderer();
            return false;
        }
        return true;
    }

    private void ResetLineRenderer()
    {
        glowingHex.SetActive(false);
        lineRenderer.positionCount = 0;
    }
}
