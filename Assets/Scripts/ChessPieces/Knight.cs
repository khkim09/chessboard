using System.Collections.Generic;
using UnityEngine;

public class Knight : ChessPiece
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

        // knight의 이동 가능 8 case
        List<Vector2Int> availableCase = new List<Vector2Int>();
        availableCase.Add(new Vector2Int(currentX - 2, currentY + 1));
        availableCase.Add(new Vector2Int(currentX - 1, currentY + 2));
        availableCase.Add(new Vector2Int(currentX + 1, currentY + 2));
        availableCase.Add(new Vector2Int(currentX + 2, currentY + 1));
        availableCase.Add(new Vector2Int(currentX + 2, currentY - 1));
        availableCase.Add(new Vector2Int(currentX + 1, currentY - 2));
        availableCase.Add(new Vector2Int(currentX - 1, currentY - 2));
        availableCase.Add(new Vector2Int(currentX - 2, currentY - 1));

        for (int i = 0; i < 8; i++)
            if (isValidMove(availableCase[i]))
            {
                if (board[availableCase[i].x, availableCase[i].y] == null)
                    l.Add(availableCase[i]);
                else
                    if (board[availableCase[i].x, availableCase[i].y].team != board[currentX, currentY].team)
                        l.Add(availableCase[i]);
            }

        return l;
    }
}
