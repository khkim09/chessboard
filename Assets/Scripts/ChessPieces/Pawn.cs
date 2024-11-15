using System.Collections.Generic;
using UnityEngine;

public class Pawn : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> l = new List<Vector2Int>();

        int direction = (team == 0) ? 1 : -1; // white : move up (1), black : move down (-1)

        // pawn simple move
        if (team == 0) // white pawn의 첫 움직임 (1, 2칸 전진 중 선택)
        {
            if (board[currentX, currentY + direction] == null)
                l.Add(new Vector2Int(currentX, currentY + direction));

            if (currentY == 1)
                if (board[currentX, currentY + direction * 2] == null)
                    if (board[currentX, currentY + direction] == null)
                        l.Add(new Vector2Int(currentX, currentY + direction * 2));
        }
        else // black pawn 동일
        {
            if (board[currentX, currentY + direction] == null)
                l.Add(new Vector2Int(currentX, currentY + direction));

            if (currentY == 6)
                if (board[currentX, currentY + direction * 2] == null)
                    if (board[currentX, currentY + direction] == null)
                        l.Add(new Vector2Int(currentX, currentY + direction * 2));
        }

        // pawn killing move
        if (board[currentX + 1, currentY + direction] != null) // 우측 앞에 chess 말 존재
            if (board[currentX, currentY].team != board[currentX + 1, currentY + direction].team) // 해당 chess 말이 상대 team일 경우
                l.Add(new Vector2Int(currentX + 1, currentY + direction));
        if (board[currentX - 1, currentY + direction] != null)
            if (board[currentX, currentY].team != board[currentX - 1, currentY + direction].team)
                l.Add(new Vector2Int(currentX - 1, currentY + direction));

        return l;
    }
}