using System.Collections.Generic;
using UnityEngine;

public class Knight : ChessPiece
{
    // knight의 이동 가능 case 위치가 보드에서 벗어나는 구역인지 검사
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
            if (isValidMove(availableCase[i])) // 8개 이동 가능 case 검사
            {
                if (board[availableCase[i].x, availableCase[i].y] == null) // 이동하려는 위치에 chess 말 X (이동)
                    l.Add(availableCase[i]);
                else
                    if (board[availableCase[i].x, availableCase[i].y].team != board[currentX, currentY].team) // chess 말 O - 다른 team (kill)
                        l.Add(availableCase[i]);
            }

        return l;
    }
}