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

        List<Vector2Int> availableCase = new List<Vector2Int>(); // 이동 가능 case (8 방향 - 단 칸)
        availableCase.Add(new Vector2Int(currentX - 1, currentY + 1));
        availableCase.Add(new Vector2Int(currentX, currentY + 1));
        availableCase.Add(new Vector2Int(currentX + 1, currentY + 1));
        availableCase.Add(new Vector2Int(currentX + 1, currentY));
        availableCase.Add(new Vector2Int(currentX + 1, currentY - 1));
        availableCase.Add(new Vector2Int(currentX, currentY - 1));
        availableCase.Add(new Vector2Int(currentX - 1, currentY - 1));
        availableCase.Add(new Vector2Int(currentX - 1, currentY));

        for (int i = 0; i < availableCase.Count; i++)
        {
            if (isValidMove(availableCase[i]))
            {
                if (board[availableCase[i].x, availableCase[i].y] == null)
                    l.Add(new Vector2Int(availableCase[i].x, availableCase[i].y));
                else
                    if (board[availableCase[i].x, availableCase[i].y].team != board[currentX, currentY].team)
                        l.Add(new Vector2Int(availableCase[i].x, availableCase[i].y));
            }
        }

        
        return l;
    }
}
