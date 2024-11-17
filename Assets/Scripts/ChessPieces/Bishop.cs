using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;

public class Bishop : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> l = new List<Vector2Int>();

        // left diagonal
        for (int i = currentX - 1, j = currentY + 1; currentX >= 0 && currentY < tileCountY; i--, j++) // 좌상향
        {
            if (board[i, j] == null)
                l.Add(new Vector2Int(i, j));
            else
            {
                if (board[i, j].team != board[currentX, currentY].team) // 다른 team (kill)
                    l.Add(new Vector2Int(i, j));
                else // 같은 team - break (그 이후로 같은 방향 이동 불가)
                    break;
            }
        }
        for (int i = currentX + 1, j = currentY - 1; currentX < tileCountX && currentY >= 0; i++, j--) // 우하향
        {
            if (board[i, j] == null)
                l.Add(new Vector2Int(i, j));
            else
            {
                if (board[i, j].team != board[currentX, currentY].team)
                    l.Add(new Vector2Int(i, j));
                else
                    break;
            }
        }

        // right diagonal
        for (int i = currentX + 1, j = currentY + 1; currentX < tileCountX && currentY< tileCountY; i++, j++) // 우상향
        {
            if (board[i, j] == null)
                l.Add(new Vector2Int(i, j));
            else
            {
                if (board[i, j].team != board[currentX, currentY].team)
                    l.Add(new Vector2Int(i, j));
                else
                    break;
            }
        }
        for (int i = currentX - 1, j = currentY - 1; currentX >= 0 && currentY >= 0; i--, j--) // 좌하향
        {
            if (board[i, j] == null)
                l.Add(new Vector2Int(i, j));
            else
            {
                if (board[i, j].team != board[currentX, currentY].team)
                    l.Add(new Vector2Int(i, j));
                else
                    break;
            }
        }

        return l;
    }
}
