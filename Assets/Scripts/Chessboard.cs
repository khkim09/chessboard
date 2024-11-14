using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

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

    [Header("Prefabs && Materials")] // array - prefabs & materials
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] teamMaterials;

    // LOGIC
    private ChessPiece[,] chessPieces; // 2차원 array (chess board 전체 위의 chess 말 position)
    private ChessPiece currentlyDragging; // 지금 선택한 말
    private List<ChessPiece> deadWhites = new List<ChessPiece>();
    private List<ChessPiece> deadBlacks = new List<ChessPiece>();
    private List<Vector2Int> availableMoves = new List<Vector2Int>(); // 이동 가능한 위치 표기할 array

    private const int TILE_COUNT_X = 8;
    private const int TILE_COUNT_Y = 8;
    private GameObject[,] tiles; // 2차원 array (chess board)

    private Camera currentCamera; // 카메라
    private Vector2Int currentHover; // 마우스로 가리키고 있는 vector
    private Vector3 bounds;

    private void Awake() // game start 시 setting 사항
    {
        transform.position = new Vector3(-3.5f, 0, -3.5f); // 게임 시작과 동시에 chess board 위치 알맞게 수정
        
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
        if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover", "Highlight"))) // "Tile", "Hover", "Highlight" 객체와 충돌하는지 검사 (충돌 시 info에 해당 tile 저장, 검사 거리 100)
        {
            // 충돌한 객체의 index 받아서 위치정보 저장
            Vector2Int hitPosition = LookUpTileIndex(info.transform.gameObject);

            // none -> tile hovering 경우 (new hovering)
            if (currentHover == -Vector2Int.one) // hovering background -> tile (new hovering)
            {
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover"); // tile에 hover 효과 적용을 위해 "Hover" layer로 변경
            }

            // tile A -> tile B hovering 경우 (hovering change)
            if (currentHover != hitPosition) // hovering tile A -> tile B (tile 변경)
            {
                if (tiles[currentHover.x, currentHover.y].layer == LayerMask.NameToLayer("Highlight"))
                    tiles[currentHover.x, currentHover.y].layer = LayerMask.NameToLayer("Highlight"); // 기존 highlight tile -> "Highlight" layer로 복구
                else
                    tiles[currentHover.x, currentHover.y].layer = LayerMask.NameToLayer("Tile"); // 기존 hover tile -> "Tile" layer로 복구
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover"); // 새로운 hover tile -> "Hover" layer로 변경
            }

            // 선택한 chess 말 이동에 대한 작업 - chess 말 선택 (click) -> 이동 tile 선택 (click)으로 2회 click으로 구성
            if (Input.GetMouseButtonDown(0)) // mouse left 클릭
            {
                // 첫번째 클릭에 관한 작업 (chess 말 선택)
                if (chessPieces[hitPosition.x, hitPosition.y] != null && !islifting) // 클릭한 위치에 chessPiece 존재 && chessPiece lifting X (none chosen chessPiece)
                {
                    if (true) // my turn ?
                    {
                        currentlyDragging = chessPieces[hitPosition.x, hitPosition.y]; // 현재 클릭 한 chess 말 저장

                        availableMoves = currentlyDragging.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y); // 이동 가능 위치 list에 저장
                        HighlightTiles(); // highlight
                        islifting = liftingPiece(); // chessPiece lifting (lifting = true로 변경)
                    }
                }
                else if (islifting) // islifting = chessPiece가 선택되어 있고 공중에 있음 (already chosen ChessPiece)
                {
                    Vector2Int previousPosition = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY); // currentX, Y = 이동 전 위치

                    bool validMove = MoveTo(currentlyDragging, hitPosition.x, hitPosition.y); // 이동 가능 여부 - hitPosition = 이동할 위치
                    
                    if (!validMove)// 이동 불가 (같은 team 말이 already exists)
                        currentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y), false); // 위치 원상 복구
                    
                    RemoveHighlightTiles(); // highlight 제거
                    islifting = landingPiece();
                    currentlyDragging = null; // 선택한 chess 말 해제
                }
            }
        }
        else // "Tile", "Hover" 객체와 충돌 X (board 아닌 곳 hover 하는 중)
        {
            // tile -> background
            if (currentHover != -Vector2Int.one) // tile 작업 - chess board에서 벗어난 곳으로 hover 시
            {
                tiles[currentHover.x, currentHover.y].layer = LayerMask.NameToLayer("Tile"); // "Hover" -> "Tile"로 layer로 복구
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
        chessPieces[3, 0] = SpawnSinglePiece(ChessPieceType.King, whiteTeam);
        chessPieces[4, 0] = SpawnSinglePiece(ChessPieceType.Queen, whiteTeam);
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
        for (int i = 0; i < availableMoves.Count; i++)
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Highlight");
    }
    private void RemoveHighlightTiles() // Highlight 제거
    {
        for (int i = 0; i < availableMoves.Count; i++)
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Tile");
        availableMoves.Clear();
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
        Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY); // currentX, Y = 이동 전 위치

        if (chessPieces[x, y] != null) // 이동할 위치에 chess 말이 있을 경우 (같은 team)
        {
            ChessPiece ocp = chessPieces[x, y]; // already exists chess 말

            if (cp.team == ocp.team) // 같은 team (이동 불가)
                return false;
            
            if (ocp.team == 0) // ocp == white team
            {
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
        chessPieces[previousPosition.x, previousPosition.y] = null; // 이동 전 위치 정보 삭제

        PositionSinglePiece(x, y, false); // chess 말 이동 (smooth operation : force = false)

        return true;
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