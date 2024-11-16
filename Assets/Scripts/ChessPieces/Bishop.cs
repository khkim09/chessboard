using System.Collections.Generic;
using UnityEngine;

public class Bishop : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> l = new List<Vector2Int>();

        // left diagonal - 합 일정 & 각 좌표 chessboard 내부로
        int sum = currentX + currentY;
        for (int i = 0; i < tileCountX; i++)
            for (int j = 0; j < tileCountY; j++)
                if (i + j == sum)
                {
                    if (board[i, j] == null)
                        l.Add(new Vector2Int(i, j));
                    else
                        if (board[i, j].team != board[currentX, currentY].team)
                            l.Add(new Vector2Int(i, j));
                }

        /*
        // 수정 필요
        // right diagonal - 좌표 차이 유지
        int diff = currentX - currentY;
        for (int i = 0; i < tileCountX; i++)
            for (int j = 0; j < tileCountY; j++)
                if (board[i, i + diff] == null)
                    
        */

        // 아래는 버려
        /*
        for (int i = 0; i < tileCountX; i++)
            for (int j = 0; j < tileCountY; j++)
            {
                if (i < j)
                {
                    if (j - i == diff)
                    {
                        if (board[i, j] == null)
                            l.Add(new Vector2Int(i, j));
                        else
                            if (board[i, j].team != board[currentX, currentY].team)
                                l.Add(new Vector2Int(i, j));
                    }
                }
                else
                {
                    if (i - j == diff)
                    {
                        if (board[i, j] == null)
                            l.Add(new Vector2Int(i, j));
                        else
                            if (board[i, j].team != board[currentX, currentY].team)
                                l.Add(new Vector2Int(i, j));
                    }
                }
            }
        */
        return l;
    }
}
