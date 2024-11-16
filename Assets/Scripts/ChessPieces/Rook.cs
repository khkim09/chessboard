using System.Collections.Generic;
using UnityEngine;

public class Rook : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> l = new List<Vector2Int>();

        int minAvailableX = 0, maxAvailableX = 7;
        int minAvailableY = 0, maxAvailableY = 7;

        // vertical move
        for (int i = 0; i < currentX; i++) // 현재 위치 기준 구간 나눠서
            if (board[i, currentY] != null)
                if (i >= minAvailableX)
                {
                    if (board[i, currentY].team == board[currentX, currentY].team) // 같은 team으로 막혀 있으면 해당 tile 제외 (i + 1)
                        minAvailableX = i + 1;
                    else
                        minAvailableX = i;
                }

        for (int i = currentX + 1; i < tileCountX; i++)
            if (board[i, currentY] != null)
                if (i <= maxAvailableX)
                {
                    if (board[i, currentY].team == board[currentX, currentY].team)
                        maxAvailableX = i - 1;
                    else
                        maxAvailableX = i;
                }

        // horizontal move
        for (int j = 0; j < currentY; j++) // 현재 위치 기준 구간 나눠서
            if (board[currentX, j] != null)
                if (j >= minAvailableY)
                {
                    if (board[currentX, j].team == board[currentX, currentY].team)
                        minAvailableY = j + 1;
                    else
                        minAvailableY = j;
                }

        for (int j = currentY + 1; j < tileCountX; j++)
            if (board[currentX, j] != null)
                if (j <= maxAvailableY)
                {
                    if (board[currentX, j].team == board[currentX, currentY].team)
                        maxAvailableY = j - 1;
                    else
                        maxAvailableY = j;
                }

        for (int i = minAvailableX; i <= maxAvailableX; i++)
            l.Add(new Vector2Int(i, currentY));
        for (int j = minAvailableY; j <= maxAvailableY; j++)
            l.Add(new Vector2Int(currentX, j));

        return l;
    }
}
