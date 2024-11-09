using UnityEngine;

public enum ChessPieceType
{
    None = 0,
    Pawn = 1,
    Rook = 2,
    Knight = 3,
    Bishop = 4,
    Queen = 5,
    King = 6
}



public class ChessPiece : MonoBehaviour
{
    public int team; // 색 구분 (white = 0, black = 1)
    public int currentX;
    public int currentY;
    public ChessPieceType type;

    private Vector3 desiredPostion;
    private Vector3 desiredScale;
}
