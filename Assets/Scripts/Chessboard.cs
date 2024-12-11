using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;
using UnityEngine.UI;
using Unity.VisualScripting;
using Unity.Collections;

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
    [SerializeField] public bool islifting = false;
    [SerializeField] private GameObject victoryScreen;
    [SerializeField] private GameObject choosingScreen;
    [SerializeField] private Transform rematchIndicator;
    [SerializeField] private Button rematchButton;

    [Header("Prefabs && Materials")] // array - prefabs & materials
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] teamMaterials;

    // LOGIC
    public ChessPiece[,] chessPieces; // 2차원 array (chess board 전체 위의 chess 말 position)
    public ChessPiece currentlyDragging; // 지금 선택한 말
    public List<ChessPiece> deadWhites = new List<ChessPiece>();
    public List<ChessPiece> deadBlacks = new List<ChessPiece>();
    public List<Vector2Int> availableMoves = new List<Vector2Int>(); // 이동 가능한 위치 표기할 array
    public List<Vector2Int> specialMoves = new List<Vector2Int>(); // 이동 가능한 special move array

    public const int TILE_COUNT_X = 8;
    public const int TILE_COUNT_Y = 8;
    public GameObject[,] tiles; // 2차원 array (chess board)

    public Camera currentCamera; // 카메라
    public Vector2Int currentHover; // 매 frame update 전 마우스 위치 (60fps 기준 A -> B, currentHover = A, hitPosition = B)
    private Vector3 bounds;
    public bool isWhiteTurn;

    public SpecialMove specialMove; // rook <-> king swap (castling) 같은 special move
    public List<Vector2Int[]> moveList = new List<Vector2Int[]>(); // 여태 이동한 chess move 모두 저장 : (이동 전 위치 vector2Int, 이동 후 위치 vector2Int) 쌍

    // Multi Logic
    private int playerCount = -1;
    public int currentTeam = -1;
    private bool localGame = true;
    private bool[] playerRematch = new bool[2];

    public int turn = 0;
    public bool isRunePhase = false;
    public Vector2Int lastMove;
    private bool isFirstTurn = true;
    public bool isHaste = false;
    public bool whiteChoose = false;
    public bool blackChoose = false;



    private void Awake() // game start 시 setting 사항
    {
        transform.position = new Vector3(-3.5f, 0, -3.5f); // 게임 시작과 동시에 chess board 위치 알맞게 수정

        isWhiteTurn = true;

        GenerateAllTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y); // 8 x 8 chess board 생성 (GenereateAllTiles 호출)
        SpawnAllPieces(); // 32 pieces의 chess pieces 생성
        PositionAllPieces(); // chess pieces의 올바른 positioning

        RegisterEvents();
    }
    private void Update()
    {
        if (!currentCamera) // 카메라 설정 안 됐을 경우, main camera로 설정
        {
            currentCamera = Camera.main;
            return;
        }
        GameObject ui = GameObject.Find("UI");
        if (!ui.GetComponent<UI>().timeOut)
        {
            if (isRunePhase)
            {
                RuneSystem.RuneActivate();

            }
            // tile에 마우스 올려 놓을 시(hover) 해당 tile 표시되도록 하는 과정
            else
            {
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
                        if (chessPieces[hitPosition.x, hitPosition.y] != null && !islifting && chessPieces[hitPosition.x, hitPosition.y].isActive) // 클릭한 위치에 chessPiece 존재 && chessPiece lifting X (none chosen chessPiece)
                        {
                            if ((chessPieces[hitPosition.x, hitPosition.y].team == 0 && isWhiteTurn && currentTeam == 0)
                            || (chessPieces[hitPosition.x, hitPosition.y].team == 1 && !isWhiteTurn && currentTeam == 1)) // check turn
                            {
                                currentlyDragging = chessPieces[hitPosition.x, hitPosition.y]; // 현재 클릭 한 chess 말 저장

                                // 이동 가능 위치 list에 저장
                                availableMoves = currentlyDragging.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);

                                // special move 유형 반환받아 저장
                                specialMove = currentlyDragging.GetSpecialMoves(ref chessPieces, ref moveList, ref specialMoves);

                                PreventCheck(); // chess 말 이동 시 check인 상황 - 해당 chess 말 이동 제한 || 현재 check인 상황 - king 구할 수 있는 chess 말만 이동 가능
                                HighlightTiles(); // highlight
                                islifting = liftingPiece(); // chessPiece lifting (islifting = true로 변경)
                            }
                        }
                        else if (islifting) // islifting = chessPiece가 선택되어 있고 공중에 있음 (already chosen ChessPiece)
                        {
                            Vector2Int previousPosition = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY); // currentX, Y = 이동 전 위치
                                                                                                                                  //Debug.Log()
                            if ((ContainsValidMove(ref availableMoves, new Vector2Int(hitPosition.x, hitPosition.y)) || isSpecialMove(ref specialMoves, new Vector2Int(hitPosition.x, hitPosition.y))) && chessPieces[previousPosition.x, previousPosition.y].isActive) // "Highlight" && "Special" 이 아닌 위치로 이동 시도
                            {
                                MoveTo(previousPosition.x, previousPosition.y, hitPosition.x, hitPosition.y); // 이동 가능 여부 - hitPosition = 이동할 위치
                                lastMove = hitPosition;
                                ActiveTeamPieces();
                                isRunePhase = true;
                                if (isFirstTurn)
                                {
                                    ui.GetComponent<UI>().startTimer();
                                    isFirstTurn = false;
                                }
                                string runeName = tiles[hitPosition.x, hitPosition.y].GetComponent<Rune>().tileRune;
                                ui.GetComponent<UI>().displayRune(runeName);

                                // Net Implements
                                NetMakeMove nmm = new NetMakeMove();
                                nmm.originalX = previousPosition.x;
                                nmm.originalY = previousPosition.y;
                                nmm.destinationX = hitPosition.x;
                                nmm.destinationY = hitPosition.y;
                                nmm.teamId = currentTeam;

                                Client.Instance.SendToServer(nmm);
                            }
                            else // 이동 불가
                            {
                                currentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y), false); // 위치 원상 복구
                                RemoveHighlightTiles(); // highlight 제거
                                islifting = landingPiece(); // chessPiece landing (islifting = false로 변경)
                                currentlyDragging = null; // 선택 말 해제
                            }
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
        }
        else
        {
            if (currentlyDragging != null)
            {
                currentlyDragging.SetPosition(GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentY), false); // 위치 원상 복구
                RemoveHighlightTiles();
                islifting = landingPiece();
                currentlyDragging = null;
            }
            ui.GetComponent<UI>().timeOut = false;
            ui.GetComponent<UI>().resetTimer(30);
            isRunePhase = false;
            ui.GetComponent<UI>().displayRune("");
            Debug.Log("Timer reset");
            isWhiteTurn = !isWhiteTurn; // turn 순환 (white -> black -> white -> black)
            Debug.Log(isWhiteTurn);
            if (localGame) // local Game일 경우의 turn 순환
                currentTeam = (currentTeam == 0) ? 1 : 0;
        }
    }

    private void ActiveTeamPieces()
    {
        for (int i = 0; i < TILE_COUNT_X; i++)
        {
            for (int j = 0; j < TILE_COUNT_Y; j++)
            {
                if (chessPieces[i, j] != null)
                {
                    if (chessPieces[i, j].team == currentTeam)
                    {
                        if (!chessPieces[i, j].isVined)
                        {
                            chessPieces[i, j].isActive = true;
                        }
                        else
                        {
                            chessPieces[i, j].isVined = false;
                        }
                        chessPieces[i, j].isInvincible = false;
                    }
                }
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

        //타일의 위치에 따라 무작위 문양 부여
        tileObject.AddComponent<Rune>();
        tileObject.GetComponent<Rune>().tileRune = RuneSystem.RuneAssign(x, y);

        tileObject.name = string.Format("X:{0} Y:{1} Rune:{2}", x, y, tileObject.GetComponent<Rune>().tileRune);

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
        cp.isActive = true;
        cp.isInvincible = false;
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
    public void PositionSinglePiece(int x, int y, bool force)
    {
        chessPieces[x, y].currentX = x;
        chessPieces[x, y].currentY = y;
        chessPieces[x, y].SetPosition(GetTileCenter(x, y), force); // 위치 적절히 조정
    }
    public Vector3 GetTileCenter(int x, int y)
    {
        return new Vector3(x * tileSize, yOffset, y * tileSize) - bounds + new Vector3((tileSize / 2), 0, (tileSize / 2));
    }

    // Highlight Tiles (이동 가능 위치 highlight)
    public void HighlightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
        {
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
    public void RemoveHighlightTiles() // "Highlight", "Special" 제거
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
    public void CheckMate(int team)
    {
        DisplayVictory(team);
        GameObject.Find("UI").GetComponent<UI>().stopTimer();
    }
    private void DisplayVictory(int winningTeam)
    {
        victoryScreen.SetActive(true);
        victoryScreen.transform.GetChild(winningTeam).gameObject.SetActive(true);
        GameObject.Find("UI").GetComponent<UI>().stopTimer();
        GameObject.Find("UI").GetComponent<UI>().UIon_off(false);
    }
    private void Draw()
    {
        DisplayDraw();
    }
    private void DisplayDraw()
    {
        victoryScreen.SetActive(true);
        victoryScreen.transform.GetChild(2).gameObject.SetActive(true);
        GameObject.Find("UI").GetComponent<UI>().stopTimer();
        GameObject.Find("UI").GetComponent<UI>().UIon_off(false);
    }
    public void OnRematchButton()
    {
        if (localGame)
        {
            NetRematch wrm = new NetRematch();
            wrm.teamId = 0;
            wrm.wantRematch = 1;
            Client.Instance.SendToServer(wrm);

            NetRematch brm = new NetRematch();
            brm.teamId = 1;
            brm.wantRematch = 1;
            Client.Instance.SendToServer(brm);
        }
        else
        {
            NetRematch nrm = new NetRematch();
            nrm.teamId = currentTeam;
            nrm.wantRematch = 1;
            Client.Instance.SendToServer(nrm);
        }
    }
    public void GameReset()
    {
        // UI - 승리 text mesh 모두 비활성화
        rematchButton.interactable = true;

        rematchIndicator.transform.GetChild(0).gameObject.SetActive(false);
        rematchIndicator.transform.GetChild(1).gameObject.SetActive(false);

        victoryScreen.transform.GetChild(0).gameObject.SetActive(false);
        victoryScreen.transform.GetChild(1).gameObject.SetActive(false);
        victoryScreen.SetActive(false);

        // Field reset - chessPiece 관련 선택 해제
        currentlyDragging = null;
        availableMoves.Clear();
        specialMoves.Clear();
        moveList.Clear();
        playerRematch[0] = playerRematch[1] = false;

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

        GameObject.Find("UI").GetComponent<UI>().startTimer();
        GameObject.Find("UI").GetComponent<UI>().UIon_off(true);
    }
    public void OnMenuButton()
    {
        NetRematch nrm = new NetRematch();
        nrm.teamId = currentTeam;
        nrm.wantRematch = 0;
        Client.Instance.SendToServer(nrm);

        GameReset();
        GameUI.Instance.OnLeaveFromGameMenu();

        Invoke("ShutdownRelay", 1.0f);

        // Reset some values
        playerCount = -1;
        currentTeam = -1;
        islifting = false;
    }

    // Choosing chessPiece type for promotion
    private void ChoosePromotionChessType()
    {
        if (localGame)
            choosingScreen.SetActive(true);

        if (currentTeam == (isWhiteTurn ? 1 : 0))
            choosingScreen.SetActive(true);
    }
    private void SendPromotionMessage(ChessPieceType promotionType)
    {
        if (moveList.Count == 0)
            return;

        Vector2Int[] lastMove = moveList[moveList.Count - 1];
        ChessPiece readyPawn = chessPieces[lastMove[1].x, lastMove[1].y];

        Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject); // 기존 pawn 삭제
        ChessPiece newPiece = SpawnSinglePiece(promotionType, readyPawn.team); // pawn -> ? (promotion 수행)
        chessPieces[lastMove[1].x, lastMove[1].y] = newPiece; // moveList에 pawn이 아닌 newPieceType으로 적용되도록 설정
        PositionSinglePiece(lastMove[1].x, lastMove[1].y, true); // newPieceType 위치

        NetPromotion netPromotion = new NetPromotion
        {
            teamId = readyPawn.team,
            position = lastMove[1],
            newPieceType = promotionType
        };

        Client.Instance.SendToServer(netPromotion);
        choosingScreen.SetActive(false);
    }
    public void OnRookButton()
    {
        SendPromotionMessage(ChessPieceType.Rook);
    }
    public void OnKnightButton()
    {
        SendPromotionMessage(ChessPieceType.Knight);
    }
    public void OnBishopButton()
    {
        SendPromotionMessage(ChessPieceType.Bishop);
    }
    public void OnQueenButton()
    {
        SendPromotionMessage(ChessPieceType.Queen);
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
                    Debug.Log("Promotion");
                    ChoosePromotionChessType();
                }
            }
        }
    }
    private void PreventCheck() // 움직이면 check 되는 상황 - 못 움직이도록 availableMoves에서 제거
    {
        ChessPiece targetKing = null; // currentlyDragging team의 king 저장
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (chessPieces[x, y] != null)
                    if (chessPieces[x, y].type == ChessPieceType.King)
                        if (chessPieces[x, y].team == currentlyDragging.team)
                            targetKing = chessPieces[x, y];

        // simulation 후 움직임 제한
        SimulateMoveForSinglePiece(currentlyDragging, ref availableMoves, targetKing);
    }
    private void SimulateMoveForSinglePiece(ChessPiece cp, ref List<Vector2Int> moves, ChessPiece targetKing) // simulation
    {
        // currentlyDragging의 현재 위치 저장
        int actualX = cp.currentX;
        int actualY = cp.currentY;
        List<Vector2Int> movesToRemove = new List<Vector2Int>(); // 이동 제한 list

        // availableMoves simulation - check 상황 확인
        for (int i = 0; i < moves.Count; i++)
        {
            int simX = moves[i].x; // availableMove 위치
            int simY = moves[i].y;

            Vector2Int kingPostionThisSim = new Vector2Int(targetKing.currentX, targetKing.currentY); // simulation에서의 king의 위치

            if (cp.type == ChessPieceType.King) // currentlyDragging = king일 경우 king position update (availableMoves 위치)
                kingPostionThisSim = new Vector2Int(simX, simY);

            // currentlyDragging을 kill할 가능성 있는 chessPiece 저장 과정
            ChessPiece[,] simulation = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y]; // 새로운 chess 말 위치 저장할 8 x 8 배열 (not reference)
            List<ChessPiece> simAttackingPieces = new List<ChessPiece>(); // currentlyDragging을 kill 할 가능성 있는 chessPiece 저장할 list
            for (int x = 0; x < TILE_COUNT_X; x++)
                for (int y = 0; y < TILE_COUNT_Y; y++)
                    if (chessPieces[x, y] != null)
                    {
                        simulation[x, y] = chessPieces[x, y]; // simulation 전의 chessPiece 위치 복사 (copy 과정)
                        if (simulation[x, y].team != cp.team) // cp(currentlyDragging)의 상대 team 모두 저장
                            simAttackingPieces.Add(simulation[x, y]);
                    }

            // Simulation 수행
            simulation[actualX, actualY] = null; // 현재 currentlyDragging의 위치 해제
            cp.currentX = simX;
            cp.currentY = simY;
            simulation[simX, simY] = cp; // currentlyDragging의 이동 가능한 tile로의 위치 이동 (simulation)

            // currentlyDragging이 kill할 수 있는 chessPiece는 제거
            var deadPiece = simAttackingPieces.Find(c => c.currentX == simX && c.currentY == simY);
            if (deadPiece != null)
                simAttackingPieces.Remove(deadPiece);

            // simAttackingPieces (currentlyDragging을 kill할 가능성 있는 chessPiece)의 availableMove 저장
            List<Vector2Int> simMoves = new List<Vector2Int>();

            // 해당 tile로 이동 시 죽을 경우 이동 제한
            for (int p = 0; p < simAttackingPieces.Count; p++)
            {
                List<Vector2Int> pieceMoves = simAttackingPieces[p].GetAvailableMoves(ref simulation, TILE_COUNT_X, TILE_COUNT_Y); // currentlyDragging을 kill할 가능성 있는 chessPiece의 availableMove 호출
                for (int q = 0; q < pieceMoves.Count; q++)
                    simMoves.Add(pieceMoves[q]); // simMoves에 추가
            }

            // simMoves에 king의 현재 위치가 포함될 경우 (check), 이동 제한 list에 추가
            if (ContainsValidMove(ref simMoves, kingPostionThisSim))
                movesToRemove.Add(moves[i]);

            // simulation 실행 전으로 chessPiece 위치 복구
            cp.currentX = actualX;
            cp.currentY = actualY;
        }

        // 현재 이동 가능 tile에서 제외 (availableMoves에서 제거)
        for (int i = 0; i < movesToRemove.Count; i++)
            moves.Remove(movesToRemove[i]);
    }
    public bool CheckforCheckMate() // checkmate라 움직일 게 없는 상황 - exit
    {
        Vector2Int[] lastMove = moveList[moveList.Count - 1]; // 마지막 이동 chess 말의 team 확인
        int targetTeam = chessPieces[lastMove[1].x, lastMove[1].y].team == 0 ? 1 : 0; // 반대 team의 check 상황 (lastMove가 white였으면, black이 check인 상황)

        List<ChessPiece> attackingPieces = new List<ChessPiece>(); // 공격 team
        List<ChessPiece> defendingPieces = new List<ChessPiece>(); // 수비 team
        ChessPiece targetKing = null;
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (chessPieces[x, y] != null)
                {
                    if (chessPieces[x, y].team == targetTeam)
                    {
                        defendingPieces.Add(chessPieces[x, y]); // check 당한 team의 모든 chessPiece
                        if (chessPieces[x, y].type == ChessPieceType.King)
                            targetKing = chessPieces[x, y]; // check 당한 king
                    }
                    else
                    {
                        attackingPieces.Add(chessPieces[x, y]); // check 만든 team의 모든 chessPiece
                    }
                }

        // attacking Piece들의 이동 가능 위치 받아오기
        List<Vector2Int> currentAvailableMoves = new List<Vector2Int>();
        for (int i = 0; i < attackingPieces.Count; i++) // check 만든 team의 chessPiece들 simulation 수행
        {
            List<Vector2Int> attackingPieceMoves = attackingPieces[i].GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y); // 공격 team chess 말들의 이동 가능 위치 list로 저장
            for (int j = 0; j < attackingPieceMoves.Count; j++)
                currentAvailableMoves.Add(attackingPieceMoves[j]);
        }

        // 지금 check 상황 ?
        if (ContainsValidMove(ref currentAvailableMoves, new Vector2Int(targetKing.currentX, targetKing.currentY))) // 공격 team 이동 가능 위치에 현재 targetKing Position이 포함되는가
        {
            // king이 위험한 상황 -> 다른 chess Piece로 구할 수 있나
            for (int i = 0; i < defendingPieces.Count; i++)
            {
                List<Vector2Int> defendingPieceMoves = defendingPieces[i].GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y); // 수비 team chess 말들의 이동 가능 위치 list로 저장
                SimulateMoveForSinglePiece(defendingPieces[i], ref defendingPieceMoves, targetKing); // check 상황 제거 가능한 chess Piece있는지 모두 simulation

                if (defendingPieceMoves.Count != 0) // 수비 team chess말의 이동 가능 tile이 있는 경우 (다른 chessPiece 옮겨서 수비 || check에서 벗어나는 moving)
                    return false; // check 아님을 반환
            }
            return true; // check에서 벗어날 수 없음
        }

        return false; // 지금 check 상황 아님
    }
    private bool CheckforDraw() // king 끼리만 남으면 draw
    {
        for (int i = 0; i < TILE_COUNT_X; i++)
            for (int j = 0; j < TILE_COUNT_Y; j++)
                if (chessPieces[i, j] != null && chessPieces[i, j].type != ChessPieceType.King)
                    return false;

        return true;
    }

    // Operations
    public Vector2Int LookUpTileIndex(GameObject hitInfo)
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (tiles[x, y] == hitInfo)
                    return new Vector2Int(x, y); // hovering tile 위치 정보 반환

        return -Vector2Int.one; // tile을 hovering 하고 있지 않음 (-1, -1)
    }
    private void MoveTo(int originalX, int originalY, int x, int y) // x, y = hitPosition (이동하려는 위치)
    {
        ChessPiece cp = chessPieces[originalX, originalY];
        Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY); // previous position (= currentX, Y) = 이동 전 위치

        if (chessPieces[x, y] != null) // 이동할 위치에 chess 말이 있을 경우
        {
            ChessPiece ocp = chessPieces[x, y]; // already exists chess 말

            if (cp.team == ocp.team || chessPieces[x, y].isInvincible) // 같은 team (이동 불가)
                return;

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
        if (localGame) // local Game일 경우의 turn 순환
            currentTeam = (currentTeam == 0) ? 1 : 0;
        moveList.Add(new Vector2Int[] { previousPosition, new Vector2Int(x, y) }); // 모든 chess move 저장

        ProcessSpecialMove(); // special move에 해당되면 logic 수행

        RemoveHighlightTiles(); // highlight 제거
        islifting = landingPiece(); // chessPiece landing (islifting = false로 변경)
        if (currentlyDragging)
            currentlyDragging = null; // 선택 말 해제

        if (CheckforCheckMate())
            CheckMate(cp.team);

        if (CheckforDraw())
            Draw();

        return;
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
    public int ReturnToOriginalTile() // "Hover" -> " ? " (기존 tile layer로의 복구) - availableMoves, specialMoves에 대해 수행
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
    public bool liftingPiece()
    {
        currentlyDragging.SetPosition(GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentY) + Vector3.up * dragOffset, false);
        return true;
    }
    public bool landingPiece()
    {
        if (currentlyDragging == null)
            return false;
        currentlyDragging.SetPosition(GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentY), false);
        return false;
    }


    #region
    private void RegisterEvents()
    {
        NetUtility.S_WELCOME += OnWelcomeServer;
        NetUtility.S_MAKE_MOVE += OnMakeMoveServer;
        NetUtility.S_REMATCH += OnRematchServer;
        NetUtility.S_PROMOTION += OnPromotionServer;

        NetUtility.C_WELCOME += OnWelcomeClient;
        NetUtility.C_START_GAME += OnStartGameClient;
        NetUtility.C_MAKE_MOVE += OnMakeMoveClient;
        NetUtility.C_REMATCH += OnRematchClient;
        NetUtility.C_PROMOTION += OnPromotionClient;

        GameUI.Instance.SetLocalGame += OnSetLocalGame;
    }
    private void UnRegisterEvents()
    {
        NetUtility.S_WELCOME -= OnWelcomeServer;
        NetUtility.S_MAKE_MOVE -= OnMakeMoveServer;
        NetUtility.S_REMATCH -= OnRematchServer;
        NetUtility.S_PROMOTION -= OnPromotionServer;

        NetUtility.C_WELCOME -= OnWelcomeClient;
        NetUtility.C_START_GAME -= OnStartGameClient;
        NetUtility.C_MAKE_MOVE -= OnMakeMoveClient;
        NetUtility.C_REMATCH -= OnRematchClient;
        NetUtility.C_PROMOTION -= OnPromotionClient;

        GameUI.Instance.SetLocalGame -= OnSetLocalGame;
    }

    // Server
    private void OnWelcomeServer(NetMessage msg, NetworkConnection cnn)
    {
        NetWelcome nw = msg as NetWelcome;

        nw.AssignedTeam = ++playerCount;

        Server.Instance.SendToClient(cnn, nw);

        // Start Game
        if (playerCount == 1)
            Server.Instance.Broadcast(new NetStartGame());
    }
    private void OnMakeMoveServer(NetMessage msg, NetworkConnection cnn)
    {
        NetMakeMove nmm = msg as NetMakeMove;

        Server.Instance.Broadcast(nmm);
    }
    private void OnRematchServer(NetMessage msg, NetworkConnection cnn)
    {
        NetRematch nr = msg as NetRematch;

        Server.Instance.Broadcast(nr);
    }
    private void OnPromotionServer(NetMessage msg, NetworkConnection cnn)
    {
        NetPromotion np = msg as NetPromotion;

        Server.Instance.Broadcast(np);
    }

    // Client
    private void OnWelcomeClient(NetMessage msg)
    {
        NetWelcome nw = msg as NetWelcome;

        currentTeam = nw.AssignedTeam;

        if (localGame && currentTeam == 0)
            Server.Instance.Broadcast(new NetStartGame());
    }
    private void OnStartGameClient(NetMessage msg)
    {
        GameUI.Instance.ChangeCamera((currentTeam == 0) ? CameraAngle.whiteTeam : CameraAngle.blackTeam);
    }
    private void OnMakeMoveClient(NetMessage msg)
    {
        NetMakeMove nmm = msg as NetMakeMove;

        Debug.Log($"Move : {nmm.teamId} : ({nmm.originalX}, {nmm.originalY}) -> ({nmm.destinationX}, {nmm.destinationY})");

        if (nmm.teamId != currentTeam)
        {
            ChessPiece target = chessPieces[nmm.originalX, nmm.originalY];

            availableMoves = target.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
            specialMove = target.GetSpecialMoves(ref chessPieces, ref moveList, ref specialMoves);

            MoveTo(nmm.originalX, nmm.originalY, nmm.destinationX, nmm.destinationY);
        }
    }
    private void OnRematchClient(NetMessage msg)
    {
        NetRematch nrm = msg as NetRematch;

        playerRematch[nrm.teamId] = nrm.wantRematch == 1;

        // Activate the piece of UI
        if (nrm.teamId != currentTeam)
        {
            rematchIndicator.transform.GetChild((nrm.wantRematch == 1) ? 0 : 1).gameObject.SetActive(true);
            if (nrm.wantRematch != 1)
                rematchButton.interactable = false;
        }

        // if both wants to rematch
        if (playerRematch[0] && playerRematch[1])
            GameReset();
    }
    private void OnPromotionClient(NetMessage msg)
    {
        NetPromotion np = msg as NetPromotion;

        if (np.teamId != currentTeam)
        {
            Destroy(chessPieces[np.position.x, np.position.y].gameObject); // 기존 pawn 삭제
            ChessPiece newPiece = SpawnSinglePiece(np.newPieceType, np.teamId); // pawn -> ? (promotion 수행)
            chessPieces[np.position.x, np.position.y] = newPiece; // moveList에 pawn이 아닌 newPieceType으로 적용되도록 설정
            PositionSinglePiece(np.position.x, np.position.y, true); // newPieceType 위치
        }
    }

    private void ShutdownRelay()
    {
        Client.Instance.Shutdown();
        Server.Instance.Shutdown();
    }

    //Local
    private void OnSetLocalGame(bool b)
    {
        playerCount = -1;
        currentTeam = -1;
        localGame = b;
    }
    #endregion
}