using System.Collections.Generic;
using UnityEngine;

public class Queen : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> l = new List<Vector2Int>();

        // diagonal move
        // left diagonal
        for (int i = currentX - 1, j = currentY + 1; i >= 0 && j < tileCountY; i--, j++) // 좌상향
        {
            if (board[i, j] == null)
                l.Add(new Vector2Int(i, j));
            else
            {
                if (board[i, j].team != board[currentX, currentY].team) // 다른 team (kill)
                {
                    l.Add(new Vector2Int(i, j));
                    break; // kill 가능한 chess 말 까지만 이동 가능
                }
                else // 같은 team - break (그 이후로 같은 방향 이동 불가)
                    break;
            }
        }
        for (int i = currentX + 1, j = currentY - 1; i < tileCountX && j >= 0; i++, j--) // 우하향
        {
            if (board[i, j] == null)
                l.Add(new Vector2Int(i, j));
            else
            {
                if (board[i, j].team != board[currentX, currentY].team)
                {
                    l.Add(new Vector2Int(i, j));
                    break;
                }
                else
                    break;
            }
        }

        // right diagonal
        for (int i = currentX + 1, j = currentY + 1; i < tileCountX && j < tileCountY; i++, j++) // 우상향
        {
            if (board[i, j] == null)
                l.Add(new Vector2Int(i, j));
            else
            {
                if (board[i, j].team != board[currentX, currentY].team)
                {
                    l.Add(new Vector2Int(i, j));
                    break;
                }
                else
                    break;
            }
        }
        for (int i = currentX - 1, j = currentY - 1; i >= 0 && j >= 0; i--, j--) // 좌하향
        {
            if (board[i, j] == null)
                l.Add(new Vector2Int(i, j));
            else
            {
                if (board[i, j].team != board[currentX, currentY].team)
                {
                    l.Add(new Vector2Int(i, j));
                    break;
                }
                else
                    break;
            }
        }

        int minAvailableX = 0, maxAvailableX = 7;
        int minAvailableY = 0, maxAvailableY = 7;

        // horizontal & vertical move
        // horizontal move
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

        // vertical move
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
        {
            if (i == currentX)
                continue;
            l.Add(new Vector2Int(i, currentY));
        }
        for (int j = minAvailableY; j <= maxAvailableY; j++)
        {
            if (j == currentY)
                continue;
            l.Add(new Vector2Int(currentX, j));
        }

        return l;
    }
}
