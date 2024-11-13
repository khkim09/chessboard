using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

public enum ChessPieceType
{
    None = 0,
    Pawn = 1,
    Rook = 2,
    Knight = 3,
    Bishop = 4,
    Queen = 5,
    King = 6
}



public class ChessPiece : MonoBehaviour
{
    public int team; // 색 구분 (white = 0, black = 1)
    public int currentX;
    public int currentY;
    public ChessPieceType type;

    private Vector3 desiredPosition;
    private Vector3 desiredScale = Vector3.one;

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * 10);
        transform.localScale = Vector3.Lerp(transform.localScale, desiredScale, Time.deltaTime * 10);
    }

    public List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> l = new List<Vector2Int>();

        l.Add(new Vector2Int(3, 3));
        l.Add(new Vector2Int(3, 4));
        l.Add(new Vector2Int(1,2));

        return l;
    }

    public virtual void SetPosition(Vector3 position, bool force)
    {
        desiredPosition = position;
        if (force)
            transform.position = desiredPosition;
    }
    public virtual void SetScale(Vector3 scale, bool force)
    {
        desiredScale = scale;
        if (force)
            transform.localScale = desiredScale;
    }
}
