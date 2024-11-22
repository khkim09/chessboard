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

    public override SpecialMove GetSpecialMoves(ref ChessPiece[,] board, ref List<Vector2Int[]> moveList, ref List<Vector2Int> specialMoves)
    {
        SpecialMove sp = SpecialMove.None;

        // m : moveList 중 하나의 Vector2Int[] 배열, m[0] = (이동 전 위치, 이동 후 위치) 쌍 중 이동 전 위치
        // moveList.Find(m => 조건) - 조건 만족 하는 첫 번째 요소 반환
        var kingMove = moveList.Find(m => m[0].x == 4 && m[0].y == ((team == 0) ? 0 : 7)); // king 움직였는지 파악 - 이동 전 위치 x좌표 == 4, 이동 전 위치 y좌표 (white == 0, black == 7)
        var leftRook = moveList.Find(m => m[0].x == 0 && m[0].y == ((team == 0) ? 0 : 7)); // left Rook - 기존 위치 (0, 0)
        var rightRook = moveList.Find(m => m[0].x == 7 && m[0].y == ((team == 0) ? 0 : 7)); // right Rook - 기존 위치 (7, 0)

        // 움직였다면 moveList에 저장될 것이므로 반환 값 있을 것 (null == king 움직인 적 X)
        if (kingMove == null && currentX == 4)
        {
            // white team
            if (team == 0)
            {
                // left rook
                if (leftRook == null) // left rook 도 움직인 적 X
                    if (board[1, 0] == null && board[2, 0] == null && board[3, 0] == null) // rook <-> king 사이 아무것도 없음
                    {
                        specialMoves.Add(new Vector2Int(2, 0));
                        sp = SpecialMove.Castling;
                    }

                // right rook
                if (rightRook == null) // right rook 도 움직인 적 X
                    if (board[5, 0] == null && board[6, 0] == null) // rook <-> king 사이 아무것도 없음
                    {
                        specialMoves.Add(new Vector2Int(6, 0));
                        sp = SpecialMove.Castling;
                    }
            }
            // black team
            else
            {
                // left rook
                if (leftRook == null) // left rook 도 움직인 적 X
                    if (board[1, 7] == null && board[2, 7] == null && board[3, 7] == null) // rook <-> king 사이 아무것도 없음
                    {
                        specialMoves.Add(new Vector2Int(2, 7));
                        sp = SpecialMove.Castling;
                    }

                // right rook
                if (rightRook == null) // right rook 도 움직인 적 X
                    if (board[5, 7] == null && board[6, 7] == null) // rook <-> king 사이 아무것도 없음
                    {
                        specialMoves.Add(new Vector2Int(6, 7));
                        sp = SpecialMove.Castling;
                    }
            }
        }

        return sp;
    }
}
