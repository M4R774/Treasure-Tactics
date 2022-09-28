using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISelectable
{
    public void SkipTurn();
    public Transform GetTransform();
    public GameObject GetGameObject();
    int MovementSpeed { get; }
    int MovementPointsLeft();
    public void Select();
    public void Unselect();
    public bool IsSelected();
    public Vector3Int GetTileCoordinates();
    public void MoveTowardsTarget(List<WorldTile> path);
    public WorldTile GetTileUnderMyself();
}
