using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography.X509Certificates;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public enum SpecialMove
{
    None = 0,
    Castling,
    Promotion
}

public class Chessboard : MonoBehaviour
{
    [Header("Art stuff")] // Serialize field의 소제목
    [SerializeField] private Material tileMaterial;
    [SerializeField] private float tileSize = 1.0f;
    [SerializeField] private float yOffset = 0.01f;
    [SerializeField] private Vector3 boardCenter = Vector3.zero;
    [SerializeField] private float deathSize = 0.6f;
    [SerializeField] private float deathSpacing = 0.5f;
    [SerializeField] private float floatSpacing = 0.1f;
    [SerializeField] private float dragOffset = 1.0f;
    [SerializeField] private bool islifting = false;
    [SerializeField] private GameObject victoryScreen;
    [SerializeField] private GameObject choosingScreen;

    [Header("Prefabs && Materials")] // array - prefabs & materials
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] teamMaterials;

    // LOGIC
    private ChessPiece[,] chessPieces; // 2차원 array (chess board 전체 위의 chess 말 position)
    private ChessPiece currentlyDragging; // 지금 선택한 말
    public List<ChessPiece> deadWhites = new List<ChessPiece>();
    public List<ChessPiece> deadBlacks = new List<ChessPiece>();
    private List<Vector2Int> availableMoves = new List<Vector2Int>(); // 이동 가능한 위치 표기할 array
    private List<Vector2Int> specialMoves = new List<Vector2Int>(); // 이동 가능한 special move array

    private const int TILE_COUNT_X = 8;
    private const int TILE_COUNT_Y = 8;
    private GameObject[,] tiles; // 2차원 array (chess board)

    private Camera currentCamera; // 카메라
    private Vector2Int currentHover; // 매 frame update 전 마우스 위치 (60fps 기준 A -> B, currentHover = A, hitPosition = B)
    private Vector3 bounds;
    private bool isWhiteTurn;

    private SpecialMove specialMove; // rook <-> king swap (castling) 같은 special move
    private List<Vector2Int[]> moveList = new List<Vector2Int[]>(); // 여태 이동한 chess move 모두 저장 : (이동 전 위치 vector2Int, 이동 후 위치 vector2Int) 쌍



    private void Awake() // game start 시 setting 사항
    {
        transform.position = new Vector3(-3.5f, 0, -3.5f); // 게임 시작과 동시에 chess board 위치 알맞게 수정

        isWhiteTurn = true;
        
        GenerateAllTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y); // 8 x 8 chess board 생성 (GenereateAllTiles 호출)
        SpawnAllPieces(); // 32 pieces의 chess pieces 생성
        PositionAllPieces(); // chess pieces의 올바른 positioning
    }
    private void Update()
    {
        if (!currentCamera) // 카메라 설정 안 됐을 경우, main camera로 설정
        {
            currentCamera = Camera.main;
            return;
        }

        // tile에 마우스 올려 놓을 시(hover) 해당 tile 표시되도록 하는 과정
        RaycastHit info; // raycast가 충돌한 정보 저장
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition); // 마우스 위치에서 화면 상 좌표를 3D로 변환하는 ray 생성
        if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover", "Highlight", "Insight", "Special"))) // tile layer 객체와 충돌하는지 검사 (충돌 시 info에 해당 tile 저장, 검사 거리 100)
        {
            // 충돌한 객체의 index 받아서 위치정보 저장
            Vector2Int hitPosition = LookUpTileIndex(info.transform.gameObject); // hitPosition = 새로운 frame에서 update된 현 마우스 위치

            // none -> tile hovering 경우 (new hovering)
            if (currentHover == -Vector2Int.one) // hovering background -> tile (new hovering)
            {
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover"); // tile에 hover 효과 적용을 위해 layer 변경 ("Tile" -> "Hover")
            }

            // tile A -> tile B hovering 경우 (hovering change)
            if (currentHover != hitPosition) // hovering tile A -> tile B (tile 변경)
            {
                // 기존 "Highlight" or "Tile" layer로 각각 복구
                tiles[currentHover.x, currentHover.y].layer = ReturnToOriginalTile();
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover"); // 새로운 hover tile -> "Hover" layer로 변경
            }

            // 선택한 chess 말 이동에 대한 작업 - chess 말 선택 (click) -> 이동 tile 선택 (click)으로 2회 click으로 구성
            if (Input.GetMouseButtonDown(0)) // mouse left 클릭
            {
                // 첫번째 클릭에 관한 작업 (chess 말 선택)
                if (chessPieces[hitPosition.x, hitPosition.y] != null && !islifting) // 클릭한 위치에 chessPiece 존재 && chessPiece lifting X (none chosen chessPiece)
                {
                    if ((chessPieces[hitPosition.x, hitPosition.y].team == 0 && isWhiteTurn) || (chessPieces[hitPosition.x, hitPosition.y].team == 1 && !isWhiteTurn)) // check turn
                    {
                        currentlyDragging = chessPieces[hitPosition.x, hitPosition.y]; // 현재 클릭 한 chess 말 저장

                        // 이동 가능 위치 list에 저장
                        availableMoves = currentlyDragging.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                        
                        // special move 유형 반환받아 저장
                        specialMove = currentlyDragging.GetSpecialMoves(ref chessPieces, ref moveList, ref specialMoves);

                        // PreventCheck(); // chess 말 이동 시 check인 상황 - 해당 chess 말 이동 제한 || 현재 check인 상황 - king만 이동 가능
                        HighlightTiles(); // highlight
                        islifting = liftingPiece(); // chessPiece lifting (islifting = true로 변경)
                    }
                }
                else if (islifting) // islifting = chessPiece가 선택되어 있고 공중에 있음 (already chosen ChessPiece)
                {
                    Vector2Int previousPosition = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY); // currentX, Y = 이동 전 위치

                    bool validMove = MoveTo(currentlyDragging, hitPosition.x, hitPosition.y); // 이동 가능 여부 - hitPosition = 이동할 위치
                    
                    if (!validMove)// 이동 불가 (같은 team 말이 already exists)
                        currentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y), false); // 위치 원상 복구
                    
                    RemoveHighlightTiles(); // highlight 제거
                    islifting = landingPiece(); // chessPiece landing (islifting = false로 변경)
                    currentlyDragging = null; // 선택한 chess 말 해제
                }
            }
        }
        else // "Tile", "Hover", "Highlight" 객체와 충돌 X (board 아닌 곳 hover 하는 중)
        {
            // tile -> background
            if (currentHover != -Vector2Int.one) // tile 작업 - chess board에서 벗어난 곳으로 hover 시
            {
                tiles[currentHover.x, currentHover.y].layer = ReturnToOriginalTile();
                currentHover = -Vector2Int.one; // tile 밖이므로 위치 정보 (-1, -1) 저장
            }

            if (currentlyDragging && Input.GetMouseButtonDown(0) && islifting) // chess 말 작업 - chess board에서 벗어난 곳으로 drag 시
            {
                currentlyDragging.SetPosition(GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentY), false); // 위치 원상 복구
                RemoveHighlightTiles();
                islifting = landingPiece();
                currentlyDragging = null;
            }
        }
    }

    // Generate the board
    private void GenerateAllTiles(float tileSize, int tileCountX, int tileCountY) // hover 시 tile 표시
    {
        yOffset += transform.position.y; // chess board asset 위에 tile 깔기
        bounds = new Vector3((tileCountX / 2) * tileSize, 0, (tileCountX / 2) * tileSize) + boardCenter;

        tiles = new GameObject[tileCountX, tileCountY]; // 2차원 array 초기화 (chess board)
        for (int i = 0; i < tileCountX; i++)
            for (int j = 0; j < tileCountY; j++)
                tiles[i, j] = GenerateSingleTile(tileSize, i, j); // 각 tile 생성
    }
    private GameObject GenerateSingleTile(float tileSize, int x, int y)
    {
        // 형태 없는 tile Object 생성
        GameObject tileObject = new GameObject(string.Format("X:{0}, Y:{1}", x, y)); // tile index
        tileObject.transform.parent = transform; // transform = Chessboard

        // tile Object에 형태 부여 (Mesh, MeshRenderer)
        Mesh mesh = new Mesh();
        tileObject.AddComponent<MeshFilter>().mesh = mesh;
        tileObject.AddComponent<MeshRenderer>().material = tileMaterial;

        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(x * tileSize, yOffset, y * tileSize) - bounds; // 가로, 높이, 세로
        vertices[1] = new Vector3(x * tileSize, yOffset, (y + 1) * tileSize) - bounds;
        vertices[2] = new Vector3((x + 1) * tileSize, yOffset, y * tileSize) - bounds;
        vertices[3] = new Vector3((x + 1) * tileSize, yOffset, (y + 1) * tileSize) - bounds;

        int[] tris = new int[] { 0, 1, 2, 1, 3, 2 }; // 삼각형 생성 (0,1,2), (1,3,2)

        mesh.vertices = vertices;
        mesh.triangles = tris;

        mesh.RecalculateNormals(); // normal 자동 계산 -> 빛의 계산 이뤄져서 색 나오도록 (없으면 빛을 받지 않아서 어둡게 나옴)

        tileObject.layer = LayerMask.NameToLayer("Tile"); // 각 tile의 layer를 "Tile"로 지정
        tileObject.AddComponent<BoxCollider>(); // Collider for Raycast (각 tile의 경계선 생성)

        return tileObject; // 각 tile 반환
    }

    // Spawning of the pieces (말 소환)
    private void SpawnAllPieces()
    {
        chessPieces = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y]; // chess 말 넣을 2차원 array
        
        int whiteTeam = 0, blackTeam = 1;

        // white team
        chessPieces[0, 0] = SpawnSinglePiece(ChessPieceType.Rook, whiteTeam);
        chessPieces[1, 0] = SpawnSinglePiece(ChessPieceType.Knight, whiteTeam);
        chessPieces[2, 0] = SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[3, 0] = SpawnSinglePiece(ChessPieceType.Queen, whiteTeam);
        chessPieces[4, 0] = SpawnSinglePiece(ChessPieceType.King, whiteTeam);
        chessPieces[5, 0] = SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[6, 0] = SpawnSinglePiece(ChessPieceType.Knight, whiteTeam);
        chessPieces[7, 0] = SpawnSinglePiece(ChessPieceType.Rook, whiteTeam);
        for (int i = 0; i < TILE_COUNT_X; i++)
            chessPieces[i, 1] = SpawnSinglePiece(ChessPieceType.Pawn, whiteTeam);
        
        // black team
        chessPieces[0, 7] = SpawnSinglePiece(ChessPieceType.Rook, blackTeam);
        chessPieces[1, 7] = SpawnSinglePiece(ChessPieceType.Knight, blackTeam);
        chessPieces[2, 7] = SpawnSinglePiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[3, 7] = SpawnSinglePiece(ChessPieceType.Queen, blackTeam);
        chessPieces[4, 7] = SpawnSinglePiece(ChessPieceType.King, blackTeam);
        chessPieces[5, 7] = SpawnSinglePiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[6, 7] = SpawnSinglePiece(ChessPieceType.Knight, blackTeam);
        chessPieces[7, 7] = SpawnSinglePiece(ChessPieceType.Rook, blackTeam);
        for (int i = 0; i < TILE_COUNT_X; i++)
            chessPieces[i, 6] = SpawnSinglePiece(ChessPieceType.Pawn, blackTeam);
    }
    private ChessPiece SpawnSinglePiece(ChessPieceType type, int team)
    {
        ChessPiece cp = Instantiate(prefabs[(int)type - 1], transform).GetComponent<ChessPiece>(); // prefabs array의 {enum인 type - 1}을 instance화 해서 transform의 자식 객체로 생성
        
        // ChessPieceType = "Rook" (type = 1) -> (prefabs[(int)type - 1]  = Rook) -> instance화 (객체 생성)
        cp.type = type; // 해당 객체 = "Rook" 임을 명시
        cp.team = team; // black or white team
        cp.GetComponent<MeshRenderer>().material = teamMaterials[team]; // material 색상 입히기

        if (cp.type == ChessPieceType.Knight && cp.team == 1) // black knight 정면 바라보도록 rotation 수행
            cp.GetComponent<Transform>().rotation = Quaternion.Euler(0, 180, 0);

        return cp;
    }

    // Positioning
    private void PositionAllPieces()
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (chessPieces[x, y] != null)
                    PositionSinglePiece(x, y, true); // smooth operation 구현 X : game start에서는 각 말의 즉시 positioning을 위해 'force = true'
    }
    private void PositionSinglePiece(int x, int y, bool force)
    {
        chessPieces[x, y].currentX = x;
        chessPieces[x, y].currentY = y;
        chessPieces[x, y].SetPosition(GetTileCenter(x, y), force); // 위치 적절히 조정
    }
    private Vector3 GetTileCenter(int x, int y)
    {
        return new Vector3(x * tileSize, yOffset, y * tileSize) - bounds + new Vector3((tileSize / 2), 0, (tileSize / 2));
    }

    // Highlight Tiles (이동 가능 위치 highlight)
    private void HighlightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++){
            if (chessPieces[availableMoves[i].x, availableMoves[i].y] != null) // 해당 위치에 chess piece 존재
                if (chessPieces[availableMoves[i].x, availableMoves[i].y].team != currentlyDragging.team) // 상대 team일 경우
                {
                    tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Insight"); // 빨간 "Insight" layer로 변경
                    continue;
                }
                
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Highlight"); // 그 외 "Highlight" layer로 변경
        }
        for (int i = 0; i < specialMoves.Count; i++)
            tiles[specialMoves[i].x, specialMoves[i].y].layer = LayerMask.NameToLayer("Special"); // special move 가능한 위치 - "Special" layer로 변경
    }
    private void RemoveHighlightTiles() // "Highlight", "Special" 제거
    {
        // "Highlight" 제거
        for (int i = 0; i < availableMoves.Count; i++)
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Tile");
        availableMoves.Clear();

        // "Special" 제거
        for (int i = 0; i < specialMoves.Count; i++)
            tiles[specialMoves[i].x, specialMoves[i].y].layer = LayerMask.NameToLayer("Tile");
        specialMoves.Clear();
    }

    // CheckMate (Win)
    private void CheckMate(int team)
    {
        DisplayVictory(team);
    }
    private void DisplayVictory(int winningTeam)
    {
        victoryScreen.SetActive(true);
        victoryScreen.transform.GetChild(winningTeam).gameObject.SetActive(true);
    }
    public void OnResetButton()
    {
        // UI - 승리 text mesh 모두 비활성화
        victoryScreen.transform.GetChild(0).gameObject.SetActive(false);
        victoryScreen.transform.GetChild(1).gameObject.SetActive(false);
        victoryScreen.SetActive(false);

        // Field reset - chessPiece 관련 선택 해제
        currentlyDragging = null;
        availableMoves.Clear();
        moveList.Clear();

        // Clean up - 모두 없애고
        for (int i = 0; i < TILE_COUNT_X; i++)
        {
            for (int j = 0; j < TILE_COUNT_Y; j++)
            {
                if (chessPieces[i, j] != null)
                    Destroy(chessPieces[i, j].gameObject);
                
                chessPieces[i, j] = null;
            }
        }

        // dead piece도 모두 삭제
        for (int i = 0; i < deadWhites.Count; i++)
            Destroy(deadWhites[i].gameObject);
        for (int i = 0; i < deadBlacks.Count; i++)
            Destroy(deadBlacks[i].gameObject);
        
        deadWhites.Clear();
        deadBlacks.Clear();

        // chess piece 전체 다시 세팅
        SpawnAllPieces();
        PositionAllPieces();
        isWhiteTurn = true; // always white first
    }
    public void OnExitButton()
    {
        Application.Quit(); // build 된 실행 파일 내 적용
        EditorApplication.isPlaying = false; // unity editor도 play 종료
    }

    // Choosing chessPiece type for promotion
    private void ChoosePromotionChessType()
    {
        choosingScreen.SetActive(true);
    }
    public void OnRookButton()
    {
        choosingScreen.SetActive(false);

        Vector2Int[] lastMove = moveList[moveList.Count - 1];
        ChessPiece readyPawn = chessPieces[lastMove[1].x, lastMove[1].y];

        Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject); // 기존 pawn 삭제
        ChessPiece newRook = SpawnSinglePiece(ChessPieceType.Rook, readyPawn.team); // pawn -> rook
        chessPieces[lastMove[1].x, lastMove[1].y] = newRook; // moveList에 pawn이 아닌 rook으로 적용되도록 설정
        PositionSinglePiece(lastMove[1].x, lastMove[1].y, true); // rook 위치
    }
    public void OnKnightButton()
    {
        choosingScreen.SetActive(false);

        Vector2Int[] lastMove = moveList[moveList.Count - 1];
        ChessPiece readyPawn = chessPieces[lastMove[1].x, lastMove[1].y];

        Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
        ChessPiece newKnight = SpawnSinglePiece(ChessPieceType.Knight, readyPawn.team); // pawn -> knight
        chessPieces[lastMove[1].x, lastMove[1].y] = newKnight;
        PositionSinglePiece(lastMove[1].x, lastMove[1].y, true);
    }
    public void OnBishopButton()
    {
        choosingScreen.SetActive(false);

        Vector2Int[] lastMove = moveList[moveList.Count - 1];
        ChessPiece readyPawn = chessPieces[lastMove[1].x, lastMove[1].y];

        Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
        ChessPiece newBishop = SpawnSinglePiece(ChessPieceType.Bishop, readyPawn.team); // pawn -> bishop
        chessPieces[lastMove[1].x, lastMove[1].y] = newBishop;
        PositionSinglePiece(lastMove[1].x, lastMove[1].y, true);
    }
    public void OnQueenButton()
    {
        choosingScreen.SetActive(false);

        Vector2Int[] lastMove = moveList[moveList.Count - 1];
        ChessPiece readyPawn = chessPieces[lastMove[1].x, lastMove[1].y];

        Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
        ChessPiece newQueen = SpawnSinglePiece(ChessPieceType.Queen, readyPawn.team); // pawn -> queen
        chessPieces[lastMove[1].x, lastMove[1].y] = newQueen;
        PositionSinglePiece(lastMove[1].x, lastMove[1].y, true);
    }

    // Special Moves
    private void ProcessSpecialMove() // castling, promotion
    {
        // castling - king <-> rook switch 변환
        if (specialMove == SpecialMove.Castling)
        {
            Vector2Int[] lastMove = moveList[moveList.Count - 1]; // king의 마지막 move (이동 전, 이동 후)쌍 확인

            // left rook (left castling)
            if (lastMove[1].x == 2) // castling 수행 후 king x좌표 == 2
            {
                // white castling
                if (lastMove[1].y == 0)
                {
                    ChessPiece rook = chessPieces[0, 0];
                    chessPieces[3, 0] = rook;
                    PositionSinglePiece(3, 0, false); // left rook 이동
                    chessPieces[0, 0] = null;
                }
                // black castling
                if (lastMove[1].y == 7)
                {
                    ChessPiece rook = chessPieces[0, 7];
                    chessPieces[3, 7] = rook;
                    PositionSinglePiece(3, 7, false);
                    chessPieces[0, 7] = null;
                }
            }

            // right rook (right castling)
            if (lastMove[1].x == 6)
            {
                // white castling
                if (lastMove[1].y == 0)
                {
                    ChessPiece rook = chessPieces[7, 0];
                    chessPieces[5, 0] = rook;
                    PositionSinglePiece(5, 0, false); // left rook 이동
                    chessPieces[7, 0] = null;
                }
                // black castling
                if (lastMove[1].y == 7)
                {
                    ChessPiece rook = chessPieces[7, 7];
                    chessPieces[5, 7] = rook;
                    PositionSinglePiece(5, 7, false);
                    chessPieces[7, 7] = null;
                }
            }

        }
        
        // promotion - (pawn -> ?) 변환
        if (specialMove == SpecialMove.Promotion)
        {
            Vector2Int[] lastMove = moveList[moveList.Count - 1]; // promotion 수행한 pawn의 마지막 move
            ChessPiece readyPawn = chessPieces[lastMove[1].x, lastMove[1].y]; // promotion 진행할 pawn - 이동 후 좌표 (chess board의 최상단 or 최하단)

            if (readyPawn.type == ChessPieceType.Pawn)
            {
                if ((readyPawn.team == 0 && lastMove[1].y == 7) || (readyPawn.team == 1 && lastMove[1].y == 0)) // pawn이 끝까지 도착
                {
                    ChoosePromotionChessType();
                }
            }
        }
    }
    private void PreventCheck() // 움직이면 check 상황 - 못 움직이도록 highlight tile 제거
    {
        ChessPiece targetKing = null;
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (chessPieces[x, y].type == ChessPieceType.King)
                    if (chessPieces[x, y].team == currentlyDragging.team)
                        targetKing = chessPieces[x, y];

        // 해당 chess 말 움직일 시 check가 되는 상황 - 움직임 제한
        SimulateMoveForSinglePiece(currentlyDragging, ref availableMoves, targetKing);
    }
    private void SimulateMoveForSinglePiece(ChessPiece cp, ref List<Vector2Int> moves, ChessPiece targetKing)
    {
        
    }

    // Operations
    private Vector2Int LookUpTileIndex(GameObject hitInfo)
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (tiles[x, y] == hitInfo)
                    return new Vector2Int(x, y); // hovering tile 위치 정보 반환

        return -Vector2Int.one; // tile을 hovering 하고 있지 않음 (-1, -1)
    }
    private bool MoveTo(ChessPiece cp, int x, int y) // x, y = hitPosition (이동하려는 위치)
    {
        if (!ContainsValidMove(ref availableMoves, new Vector2Int(x, y)) && !isSpecialMove(ref specialMoves, new Vector2Int(x, y))) // "Highlight" && "Special" 이 아닌 위치로 이동 시도
            return false;
        
        Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY); // previous position (= currentX, Y) = 이동 전 위치

        if (chessPieces[x, y] != null) // 이동할 위치에 chess 말이 있을 경우
        {
            ChessPiece ocp = chessPieces[x, y]; // already exists chess 말

            if (cp.team == ocp.team) // 같은 team (이동 불가)
                return false;
            
            if (ocp.team == 0) // ocp == white team
            {
                if (ocp.type == ChessPieceType.King) // king 잡으면 게임 끝
                    CheckMate(1);

                deadWhites.Add(ocp);
                ocp.SetScale(Vector3.one * deathSize, false); // 죽은 말 크기 조정
                ocp.SetPosition(
                    new Vector3(8 * tileSize + 0.05f, yOffset + floatSpacing, -0.5f * tileSize)
                    - bounds
                    + new Vector3(tileSize / 2, 0, tileSize / 2)
                    + (Vector3.forward * deathSpacing) * deadWhites.Count, false); // 죽은 말 사이드에 열거
            }
            else // ocp == black team
            {
                if (ocp.type == ChessPieceType.King)
                    CheckMate(0);

                deadBlacks.Add(ocp);
                ocp.SetScale(Vector3.one * deathSize, false);
                ocp.SetPosition(
                    new Vector3(-8 * tileSize - 1.05f, yOffset + floatSpacing, -0.5f * tileSize)
                    + bounds
                    + new Vector3(tileSize / 2, 0, tileSize / 2)
                    + (Vector3.back * deathSpacing) * deadBlacks.Count, false);
            }
        }

        chessPieces[x, y] = cp; // 이동하려는 위치에 chess 말 저장
        chessPieces[previousPosition.x, previousPosition.y] = null; // 이동 전 chess board 위 chess 말 삭제

        PositionSinglePiece(x, y, false); // chess 말 이동 (smooth operation : force = false)

        isWhiteTurn = !isWhiteTurn; // turn 순환 (white -> black -> white -> black)
        moveList.Add(new Vector2Int[] { previousPosition, new Vector2Int(x, y) }); // 모든 chess move 저장

        ProcessSpecialMove(); // special move에 해당되면 logic 수행

        return true;
    }
    private bool ContainsValidMove(ref List<Vector2Int> moves, Vector2Int pos) // 이동 가능한 위치 ("Highlight") 여부 확인
    {
        for (int i = 0; i < moves.Count; i++)
            if (moves[i].x == pos.x && moves[i].y == pos.y)
                return true;
        
        return false;
    }
    private bool isSpecialMove(ref List<Vector2Int> moves, Vector2Int pos)
    {
        for (int i = 0; i < moves.Count; i++)
            if (moves[i].x == pos.x && moves[i].y == pos.y)
                return true;
        
        return false;
    }
    private int ReturnToOriginalTile() // "Hover" -> " ? " (기존 tile layer로의 복구) - availableMoves, specialMoves에 대해 수행
    {
        for (int i = 0; i < availableMoves.Count; i++) // 이동 가능한 타일 list (availableMoves = moves) 순회
            if (availableMoves[i].x == currentHover.x && availableMoves[i].y == currentHover.y) // 마우스 옮기기 직전 tile (= currentHover)이 moves에 포함되면 (Highlight || Insight || Special)
            {
                if (chessPieces[availableMoves[i].x, availableMoves[i].y] != null) // 이동 가능 타일 중 chess 말이 존재한다? = 상대 team 말 (같은 team 말은 모두 availableMoves에서 제외되기 때문)
                    return LayerMask.NameToLayer("Insight"); // kill 가능한 tile 로 복구 ("Insight")
                
                // else if (isSpecialMove()) // special move 가능한 tile임을 표시("Special" tile)로 복구

                return LayerMask.NameToLayer("Highlight"); // "Highlight" tile 
            }
        
        for (int i = 0; i < specialMoves.Count; i++) // "Special" layer로 복구
            if (specialMoves[i].x == currentHover.x && specialMoves[i].y == currentHover.y)
                return LayerMask.NameToLayer("Special");
        
        return LayerMask.NameToLayer("Tile");
    }
    private bool liftingPiece()
    {
        currentlyDragging.SetPosition(GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentY) + Vector3.up * dragOffset, false);
        return true;
    }
    private bool landingPiece()
    {
        currentlyDragging.SetPosition(GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentY), false);
        return false;
    }
}