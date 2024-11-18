using System.Collections.Generic;
using UnityEngine;

public class King : ChessPiece
{
    private bool isValidMove(Vector2Int move)
    {
        if (0 <= move.x && move.x <= 7 && 0 <= move.y && move.y <= 7)
            return true;

        return false;
    }
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> l = new List<Vector2Int>();

        List<Vector2Int> availableCase = new List<Vector2Int>();
        availableCase.Add(new Vector2Int(currentX - 1, currentY + 1));
        availableCase.Add(new Vector2Int(currentX, currentY + 1));
        availableCase.Add(new Vector2Int(currentX + 1, currentY + 1));
        availableCase.Add(new Vector2Int(currentX + 1, currentY));
        availableCase.Add(new Vector2Int(currentX + 1, currentY - 1));
        availableCase.Add(new Vector2Int(currentX, currentY - 1));
        availableCase.Add(new Vector2Int(currentX - 1, currentY - 1));
        availableCase.Add(new Vector2Int(currentX - 1, currentY));

        if (isValidMove(new Vector2Int(currentX - 1, currentY + 1)))

        switch()
        {
            case 1: 
        }

        return l;
    }
}
